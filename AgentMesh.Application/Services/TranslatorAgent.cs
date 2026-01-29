using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class TranslatorAgent : ITranslatorAgent
    {
        private readonly Regex JsonCodeRegex = new Regex(@"^```\s*json\s*(?<json>[\s\S]+)```$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<TranslatorAgent> _logger;

        public TranslatorAgent(
            [FromKeyedServices(TranslatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ILogger<TranslatorAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<TranslatorAgentOutput> ExecuteAsync(
            TranslatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing TranslatorAgent.");
            _logger.LogDebug("TranslatorAgent Input: {Input}", JsonSerializer.Serialize(input));

            var userMessage = UserMessageBuilder.BuildUserMessageString(input.RequestContext, input.UserRequest);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Translate this sentence to {input.TargetLanguage}" },
                new AgentMessage { Role = AgentMessageRole.User, Content = userMessage }
            };

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                var jsonMatch = JsonCodeRegex.Match(responseText);
                string jsonContent;
                
                if (jsonMatch.Success)
                {
                    jsonContent = jsonMatch.Groups["json"].Value.Trim();
                }
                else
                {
                    jsonContent = responseText;
                }

                TranslationResponse? translationResponse;
                try
                {
                    translationResponse = JsonSerializer.Deserialize<TranslationResponse>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse translation response: {ResponseText}", jsonContent);
                    throw new BadStructuredResponseException(responseText, "The model's response did not contain valid JSON.");
                }

                if (translationResponse == null || string.IsNullOrWhiteSpace(translationResponse.TranslatedSentence))
                {
                    throw new BadStructuredResponseException(responseText, "The model's response did not contain a valid translation.");
                }

                return new TranslatorAgentOutput
                {
                    TranslatedSentence = translationResponse.TranslatedSentence,
                    DetectedOriginalLanguage = translationResponse.DetectedOriginalLanguage ?? "Unknown",
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, TranslatorAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "TranslatorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("TranslatorAgent Output: {Output}", JsonSerializer.Serialize(result));
            return result;
        }

        private class TranslationResponse
        {
            public string TranslatedSentence { get; set; } = string.Empty;
            public string? DetectedOriginalLanguage { get; set; }
        }
    }
}
