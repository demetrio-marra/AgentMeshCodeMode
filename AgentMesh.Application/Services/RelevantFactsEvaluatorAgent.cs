using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.RelevantFactsEvaluator;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class RelevantFactsEvaluatorAgent(
        [FromKeyedServices(RelevantFactsEvaluatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RelevantFactsEvaluatorAgent> logger) : AgentBase<bool>(logger, RelevantFactsEvaluatorAgentConfiguration.AgentName, openAIClient, resilience), IRelevantFactsEvaluatorAgent
    {
        private readonly ILogger<RelevantFactsEvaluatorAgent> _logger = logger;

        public async Task<RelevantFactsEvaluatorAgentOutput> ExecuteAsync(
            RelevantFactsEvaluatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.User, Content = $"User request:\n{input.EnrichedUserRequest}\n\nAssistant answer:\n{input.FinalAnswer}" }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new RelevantFactsEvaluatorAgentOutput
            {
                IsWorthSaving = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override bool ParseStructuredResponse(string rawResponseText)
        {
            var normalized = rawResponseText.Trim().ToLowerInvariant();

            if (normalized == "true" || normalized == "yes")
                return true;

            if (normalized == "false" || normalized == "no")
                return false;

            throw new BadStructuredResponseException(rawResponseText, $"Expected 'true' or 'false', got: '{rawResponseText}'");
        }
    }
}
