using AgentMesh.Application.Models;
using AgentMesh.Application.Models;
using AgentMesh.Models;
using AgentMesh.Models.ContextAnalyzer;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class ContextAnalyzerAgent : IContextAnalyzerAgent
    {
        public const string NO_RELEVANT_CONTEXT_FOUND = "NO RELEVANT CONTEXT FOUND";

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<ContextAnalyzerAgent> _logger;

        public ContextAnalyzerAgent(
            [FromKeyedServices(ContextAnalyzerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ContextAnalyzerAgentConfiguration configuration,
            ILogger<ContextAnalyzerAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<ContextAnalyzerAgentOutput> ExecuteAsync(
            ContextAnalyzerAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing ContextAnalyzerAgent.");
            _logger.LogDebug("ContextAnalyzerAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(input) }
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

                var (relevantContext, actionableRequirements) = ParseStructuredResponse(responseText);

                return new ContextAnalyzerAgentOutput
                {
                    EnrichedIntent = relevantContext,
                    ActionableRequirements = actionableRequirements,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, ContextAnalyzerAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "ContextAnalyzerAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("ContextAnalyzerAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }


        private (string RelevantContext, IEnumerable<string> ActionableRequirements) ParseStructuredResponse(string responseText)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(responseText);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("enrichedIntent", out var enrichedIntentElement) ||
                    !root.TryGetProperty("actionableRequirements", out var actionableReqElement))
                {
                    throw new BadStructuredResponseException(responseText, "The model's response is not in the expected JSON format. Expected properties 'enrichedIntent' and 'actionableRequirements' were not found.");
                }

                if (enrichedIntentElement.ValueKind != JsonValueKind.String)
                {
                    throw new BadStructuredResponseException(responseText, "The 'enrichedIntent' property is expected to be a string.");
                }

                var enrichedIntent = enrichedIntentElement.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(enrichedIntent))
                {
                    throw new BadStructuredResponseException(responseText, "The 'enrichedIntent' property is empty.");
                }

                var actionableRequirements = new List<string>();
                if (actionableReqElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in actionableReqElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                actionableRequirements.Add(value);
                            }
                        }
                    }
                }

                return (enrichedIntent, actionableRequirements);
            }
            catch (JsonException ex)
            {
                throw new BadStructuredResponseException(responseText, $"Failed to parse JSON response: {ex.Message}");
            }
        }
    }
}
