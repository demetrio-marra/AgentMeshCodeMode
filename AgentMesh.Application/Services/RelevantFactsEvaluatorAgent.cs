using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.RelevantFactsEvaluator;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class RelevantFactsEvaluatorAgent(
        [FromKeyedServices(RelevantFactsEvaluatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RelevantFactsEvaluatorAgent> logger) : AgentBase<List<string>>(logger, RelevantFactsEvaluatorAgentConfiguration.AgentName, openAIClient, resilience), IRelevantFactsEvaluatorAgent
    {
        private readonly ILogger<RelevantFactsEvaluatorAgent> _logger = logger;

        public async Task<RelevantFactsEvaluatorAgentOutput> ExecuteAsync(
            RelevantFactsEvaluatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var serializedConversation = MessageSerializationUtils.SerializeConversationHistory(input.ConversationHistory);

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.User, Content = serializedConversation }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new RelevantFactsEvaluatorAgentOutput
            {
                RelevantFacts = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override List<string> ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(rawResponseText);
                if (parsed == null)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into a list of facts.");
                }

                return [.. parsed
                    .Where(fact => !string.IsNullOrWhiteSpace(fact))
                    .Select(fact => fact.Trim())];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize relevant facts evaluator response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response as a JSON array of strings.", ex);
            }
        }
    }
}
