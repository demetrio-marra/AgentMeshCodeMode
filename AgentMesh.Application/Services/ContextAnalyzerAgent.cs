using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.ContextAnalyzer;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class ContextAnalyzerAgent : AgentBase<(string EnrichedIntent, IEnumerable<string> ActionableRequirements)>, IContextAnalyzerAgent
    {
        public const string NO_RELEVANT_CONTEXT_FOUND = "NO RELEVANT CONTEXT FOUND";

        public ContextAnalyzerAgent(
            [FromKeyedServices(ContextAnalyzerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ContextAnalyzerAgentConfiguration configuration,
            ILogger<ContextAnalyzerAgent> logger) : base(logger, ContextAnalyzerAgentConfiguration.AgentName, openAIClient)
        {
        }

        public async Task<ContextAnalyzerAgentOutput> ExecuteAsync(
            ContextAnalyzerAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(input) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new ContextAnalyzerAgentOutput
            {
                EnrichedIntent = result.Result.EnrichedIntent,
                ActionableRequirements = result.Result.ActionableRequirements,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override (string EnrichedIntent, IEnumerable<string> ActionableRequirements) ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(rawResponseText);
                var root = jsonDoc.RootElement;

                if (!root.TryGetProperty("enrichedIntent", out var enrichedIntentElement) ||
                    !root.TryGetProperty("actionableRequirements", out var actionableReqElement))
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response is not in the expected JSON format. Expected properties 'enrichedIntent' and 'actionableRequirements' were not found.");
                }

                if (enrichedIntentElement.ValueKind != JsonValueKind.String)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The 'enrichedIntent' property is expected to be a string.");
                }

                var enrichedIntent = enrichedIntentElement.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(enrichedIntent))
                {
                    throw new BadStructuredResponseException(rawResponseText, "The 'enrichedIntent' property is empty.");
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
                throw new BadStructuredResponseException(rawResponseText, $"Failed to parse JSON response: {ex.Message}");
            }
        }
    }
}
