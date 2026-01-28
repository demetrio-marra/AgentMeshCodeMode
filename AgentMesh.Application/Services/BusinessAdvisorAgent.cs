using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class BusinessAdvisorAgent : IBusinessAdvisorAgent
    {
        private static readonly Regex ResponseRegex = new("```(?<responseType>(information|businessRequirements))\\s*(?<content>[\\s\\S]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<BusinessAdvisorAgent> _logger;
        private readonly string _apiDocumentation;

        public BusinessAdvisorAgent(
            [FromKeyedServices(BusinessAdvisorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessAdvisorAgentConfiguration configuration,
            ILogger<BusinessAdvisorAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _apiDocumentation = configuration.ApiDocumentation;
        }

        public async Task<BusinessAdvisorAgentOutput> ExecuteAsync(
            BusinessAdvisorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing BusinessAdvisorAgent.");
            _logger.LogDebug("BusinessAdvisorAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var inputMessages = new List<AgentMessage>();
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"API Documentation: {_apiDocumentation}" });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.UserQuestionText });

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                var match = ResponseRegex.Match(responseText);
                if (!match.Success)
                {
                    _logger.LogWarning("The model's response did not match the expected format. Response: {ResponseText}", responseText);
                    throw new BadStructuredResponseException(responseText, "The model's response did not match the expected format.");
                }

                var responseType = match.Groups["responseType"].Value.Trim().ToLowerInvariant();
                if (responseType == "information")
                {
                    return new BusinessAdvisorAgentOutput
                    {
                        EngageCoderAgent = false,
                        AnswerToUserText = match.Groups["content"].Value.Trim(),
                        TokenCount = response.TotalTokenCount,
                        InputTokenCount = response.InputTokenCount,
                        OutputTokenCount = response.OutputTokenCount
                    };
                }

                return new BusinessAdvisorAgentOutput
                {
                    EngageCoderAgent = true,
                    BusinessRequirements = match.Groups["content"].Value.Trim(),
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, BusinessAdvisorAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "BusinessAdvisorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("BusinessAdvisorAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
