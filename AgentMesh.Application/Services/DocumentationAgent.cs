using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.Documentation;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class DocumentationAgent(
        [FromKeyedServices(DocumentationAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<DocumentationAgent> logger) : AgentBase<string>(logger, DocumentationAgentConfiguration.AgentName, openAIClient, resilience), IDocumentationAgent
    {
        private readonly ILogger<DocumentationAgent> _logger = logger;
        public const string AgentName = "Documentation Agent";

        public async Task<DocumentationAgentOutput> ExecuteAsync(
            DocumentationAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>();

            if (input.UserRequest.UserPreferences.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"Supporting User Preferences:\n{string.Join("\n", input.UserRequest.UserPreferences.Select(i => $"- {i}"))}"
                });
            }

            if (input.UserRequest.UserProvidedData.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"User provided data:\n{string.Join("\n", input.UserRequest.UserProvidedData.Select(i => $"- {i}"))}"
                });
            }

            if (input.UserRequest.UserRequestedActions.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"User Requested Actions:\n{string.Join("\n", input.UserRequest.UserRequestedActions.Select(a => $"- {a}"))}"
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
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.UserRequest.Intent });

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new DocumentationAgentOutput
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
