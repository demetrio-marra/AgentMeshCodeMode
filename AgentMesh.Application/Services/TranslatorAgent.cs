using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class TranslatorAgent : ITranslatorAgent
    {
        private const string NO_CONTEXT_MARKER = "NO_CONTEXT";

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

            var userMessage = MessageSerializationUtils.SerializeRequestAndContext(input.RequestContext, input.UserRequest);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Translate this text to {input.TargetLanguage}" },
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

                var detectedLanguage = ExtractTagContent(responseText, "DETECTED_LANGUAGE");
                var translatedRequest = ExtractTagContent(responseText, "TRANSLATED_REQUEST");
                var translatedContext = ExtractTagContent(responseText, "TRANSLATED_CONTEXT");

                if (string.IsNullOrWhiteSpace(detectedLanguage))
                {
                    _logger.LogError("Translation response did not contain DETECTED_LANGUAGE tag: {ResponseText}", responseText);
                    throw new BadStructuredResponseException(responseText, "The model's response did not contain the expected DETECTED_LANGUAGE tag.");
                }

                if (string.IsNullOrWhiteSpace(translatedRequest))
                {
                    _logger.LogError("Translation response did not contain TRANSLATED_REQUEST tag: {ResponseText}", responseText);
                    throw new BadStructuredResponseException(responseText, "The model's response did not contain the expected TRANSLATED_REQUEST tag.");
                }

                if (string.IsNullOrWhiteSpace(translatedContext))
                {
                    _logger.LogError("Translation response did not contain TRANSLATED_CONTEXT tag: {ResponseText}", responseText);
                    throw new BadStructuredResponseException(responseText, "The model's response did not contain the expected TRANSLATED_CONTEXT tag.");
                }

                return new TranslatorAgentOutput
                {
                    TranslatedSentence = translatedRequest,
                    TranslatedContext = translatedContext.Equals(NO_CONTEXT_MARKER, StringComparison.OrdinalIgnoreCase) ? null : translatedContext,
                    DetectedOriginalLanguage = string.IsNullOrWhiteSpace(detectedLanguage) ? "Unknown" : detectedLanguage,
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

        private static string ExtractTagContent(string text, string tagName)
        {
            var openTag = $"<{tagName}>";
            var closeTag = $"</{tagName}>";

            var startIndex = text.IndexOf(openTag, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
            {
                return string.Empty;
            }

            startIndex += openTag.Length;
            var endIndex = text.IndexOf(closeTag, startIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex == -1)
            {
                return string.Empty;
            }

            return text.Substring(startIndex, endIndex - startIndex).Trim();
        }
    }
}
