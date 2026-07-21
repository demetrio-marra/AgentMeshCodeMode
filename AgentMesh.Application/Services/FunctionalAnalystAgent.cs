using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.FunctionalAnalyst;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class FunctionalAnalystAgent(
        [FromKeyedServices(AgentMesh.Application.Configuration.FunctionalAnalystAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<FunctionalAnalystAgent> logger) : AgentBase<FunctionalAnalystAgent.ParsedResponse>(logger, AgentMesh.Application.Configuration.FunctionalAnalystAgentConfiguration.AgentName, openAIClient, resilience), IFunctionalAnalystAgent
    {
        private readonly ILogger<FunctionalAnalystAgent> _logger = logger;

        public async Task<FunctionalAnalystAgentOutput> ExecuteAsync(
            FunctionalAnalystAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>();

            if (!string.IsNullOrWhiteSpace(input.Intent))
            {
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Intent: {input.Intent}" });
            }

            if (!string.IsNullOrWhiteSpace(input.ConversationTopic))
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"Conversation Topic: {input.ConversationTopic}"
                });
            }

            if (input.UserRequestedActions.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"User Requested Actions:\n{string.Join("\n", input.UserRequestedActions.Select(i => $"- {i}"))}"
                });
            }

            if (input.UserProvidedData.Any())
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"User Provided Data:\n{string.Join("\n", input.UserProvidedData.Select(v => $"- {v}"))}"
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

            if (input.DoNotComment)
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = "IMPORTANT REQUIREMENT: In `businessRequirements`, you MUST EXPLICITLY instruct the Coder Agent to produce a program that ONLY RETURNS DATA and DOES NOT add comments, insights, explanations, or narrative text in its output."
                });
            }

            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.Intent });

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new FunctionalAnalystAgentOutput
            {
                BusinessRequirements = result.Result.BusinessRequirements,
                RequestRejected = result.Result.RequestRejected,
                ReasonOfRejection = result.Result.ReasonOfRejection,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override ParsedResponse ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (!responseDTO.RequestRejected && string.IsNullOrWhiteSpace(responseDTO.BusinessRequirements))
                {
                    _logger.LogWarning("The model's response contains empty business requirements for a non-rejected request. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty business requirements for a non-rejected request.");
                }

                if (responseDTO.RequestRejected && string.IsNullOrWhiteSpace(responseDTO.ReasonOfRejection))
                {
                    _logger.LogWarning("The model's response rejected the request without providing reasonOfRejection. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response rejected the request without providing reasonOfRejection.");
                }

                if (!responseDTO.RequestRejected)
                {
                    responseDTO.ReasonOfRejection = null;
                }

                return responseDTO;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        public class ParsedResponse
        {
            [JsonPropertyName("businessRequirements")]
            public string BusinessRequirements { get; set; } = string.Empty;

            [JsonRequired]
            [JsonPropertyName("requestRejected")]
            public bool RequestRejected { get; set; }

            [JsonPropertyName("reasonOfRejection")]
            public string? ReasonOfRejection { get; set; }
        }
    }
}
