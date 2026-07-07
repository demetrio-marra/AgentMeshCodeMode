using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.IntentCanonicalization;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class IntentCanonicalizationAgent(
        [FromKeyedServices(IntentCanonicalizationAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<IntentCanonicalizationAgent> logger) : AgentBase<string>(logger, IntentCanonicalizationAgentConfiguration.AgentName, openAIClient, resilience), IIntentCanonicalizationAgent
    {
        public async Task<IntentCanonicalizationAgentOutput> ExecuteAsync(IntentCanonicalizationAgentInput input, CancellationToken cancellationToken = default)
        {
            var entitiesByDomainText = input.EntitiesByDomain.Any()
                ? string.Join("\n", input.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(entity => $"- [{kvp.Key}] {entity}")))
                : "(No entities by domain)";

            var supportingIntentInformationText = input.SupportingIntentInformation.Any()
                ? string.Join("\n", input.SupportingIntentInformation.Select(i => $"- {i}"))
                : "(No supporting intent information)";

            var fastKnowledgeBaseResultsText = input.FastDomainsKnowledgeBaseQueryResults.Any()
                ? string.Join("\n", input.FastDomainsKnowledgeBaseQueryResults.Select(r => $"- File: {r.File}; Title: {r.Title}; Relevance: {(r.Relevance.HasValue ? r.Relevance.Value.ToString("0.####") : "n/a")}; Summary: {r.Summary ?? "n/a"}"))
                : "(No fast domains knowledge base results)";

            var userMessage = $"""
Captured user intent:
{input.Intent}

Captured user intent category:
{input.UserIntentCategory}

Entities by domain:
{entitiesByDomainText}

Supporting intent information:
{supportingIntentInformationText}

Fast domains knowledge base results:
{fastKnowledgeBaseResultsText}
""";

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = userMessage }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new IntentCanonicalizationAgentOutput
            {
                DomainedIntent = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override string ParseStructuredResponse(string rawResponseText)
        {
            if (string.IsNullOrWhiteSpace(rawResponseText))
            {
                throw new EmptyAgentResponseException();
            }

            return rawResponseText.Trim();
        }
    }
}
