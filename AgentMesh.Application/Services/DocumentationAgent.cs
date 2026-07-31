using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.Documentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Services
{
    public class DocumentationAgent(
        [FromKeyedServices(DocumentationAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<DocumentationAgent> logger) : AgentBase<string>(logger, DocumentationAgentConfiguration.AgentName, openAIClient, resilience)
    {
        private readonly ILogger<DocumentationAgent> _logger = logger;
        public const string AgentName = "Documentation Agent";

        public async Task<DocumentationAgentOutput> ExecuteAsync(
            DocumentationAgentInput input,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(input.KnowledgeBaseDocumentsContent))
            {
                _logger.LogInformation("No relevant API documentation found for the given actionable requirements.");
            }

            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}."
            };

            if (!string.IsNullOrWhiteSpace(input.LanguageOfTheUser))
            {
                systemMessages.Add($"User's Language: {input.LanguageOfTheUser}");
            }
            if (!string.IsNullOrWhiteSpace(input.KnowledgeBaseDocumentsContent))
            {
                systemMessages.Add($"Knowledge Base Documents Content:\n{input.KnowledgeBaseDocumentsContent}");
            }

            var userPayload = new
            {
                intent = input.UserRequest.Intent,
                userPreferences = input.UserRequest.UserPreferences,
                userProvidedData = input.UserRequest.UserProvidedData,
                userRequestedActions = input.UserRequest.UserRequestedActions,
                agentMemories = input.AgentMemories,
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

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

        protected override IEnumerable<AgentOutputParameterRecord> ParseOutputParameters(string rawResponseText)
        {
            return [
                CreateOutputParameter(CodeModeWorkflowParametersFactory.DocumentationContentParameterName, ParseStructuredResponse(rawResponseText)),
            ];
        }
    }
}
