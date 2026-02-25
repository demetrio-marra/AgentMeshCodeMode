using AgentMesh.Models;
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

            var userMessage = MessageSerializationUtils.SerializeConversationHistory(input.ContextMessages, input.UserLastRequest);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
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

                var (relevantContext, actionableRequirements) = ParseStructuredResponse(responseText);

                return new ContextAnalyzerAgentOutput
                {
                    RelevantContext = relevantContext,
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

                if (!root.TryGetProperty("relevantContext", out var relevantContextElement) ||
                    !root.TryGetProperty("actionableRequirements", out var actionableReqElement))
                {
                    throw new BadStructuredResponseException(responseText, "The model's response is not in the expected JSON format. Expected properties 'relevantContext' and 'actionableRequirements' were not found.");
                }

                var relevantContextItems = new List<string>();
                if (relevantContextElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in relevantContextElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var value = item.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                relevantContextItems.Add(value);
                            }
                        }
                    }
                }

                var relevantContext = relevantContextItems.Count > 0
                    ? string.Join("\n", relevantContextItems.Select(item => $"• {item}"))
                    : string.Empty;

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

                return (relevantContext, actionableRequirements);
            }
            catch (JsonException ex)
            {
                throw new BadStructuredResponseException(responseText, $"Failed to parse JSON response: {ex.Message}");
            }
        }
    }
}
