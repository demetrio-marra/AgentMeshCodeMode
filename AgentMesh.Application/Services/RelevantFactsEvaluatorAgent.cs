using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models;
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
            var userMessages = input.ConversationHistory
                .Where(message => message.Role == ContextMessageRole.User)
                .Where(message => !string.IsNullOrWhiteSpace(message.Text))
                .ToList();

            if (userMessages.Count == 0)
            {
                return new RelevantFactsEvaluatorAgentOutput
                {
                    RelevantUserMessages = []
                };
            }

            var serializedConversation = MessageSerializationUtils.SerializeConversationHistory(userMessages);

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.User, Content = serializedConversation }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new RelevantFactsEvaluatorAgentOutput
            {
                RelevantUserMessages = result.Result,
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
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into a list of user messages.");
                }

                return [.. parsed
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Select(message => message.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize relevant facts evaluator response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response as a JSON array of user messages.", ex);
            }
        }
    }
}
