using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.BusinessAdvisor;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class BusinessAdvisorAgent(
        [FromKeyedServices(BusinessAdvisorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<BusinessAdvisorAgent> logger) : AgentBase<string>(logger, BusinessAdvisorAgentConfiguration.AgentName, openAIClient, resilience), IBusinessAdvisorAgent
    {
        private readonly ILogger<BusinessAdvisorAgent> _logger = logger;

        public async Task<BusinessAdvisorAgentOutput> ExecuteAsync(
            BusinessAdvisorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>();

            if (!string.IsNullOrWhiteSpace(input.Intent))
            {
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Intent: {input.Intent}" });
            }

            if (input.SupportingIntentInformation.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"Supporting Intent Information:\n{string.Join("\n", input.SupportingIntentInformation.Select(i => $"- {i}"))}"
                });
            }

            if (input.Entities.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"Entities:\n{string.Join("\n", input.Entities.SelectMany(kvp => kvp.Value.Select(v => $"- [{kvp.Key}] {v}")))}"
                });
            }

            if (input.UserPreferences.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"User Preferences:\n{string.Join("\n", input.UserPreferences.Select(p => $"- {p}"))}"
                });
            }

            if (input.AgentMemories.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"Memories from AgentMemoryService:\n{string.Join("\n", input.AgentMemories.Select(m => $"- {m}"))}"
                });
            }

            if (!string.IsNullOrWhiteSpace(input.KnowledgeBaseDocumentsContent))
            {
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"KnowledgeBaseDocumentsContent: {input.KnowledgeBaseDocumentsContent}" });
            }
            else
            {
                _logger.LogInformation("No relevant API documentation found for the given actionable requirements.");
            }

            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest });

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new BusinessAdvisorAgentOutput
            {
                Content = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}
