using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.FunctionalAnalyst;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Application.Models.ChatMessages;

namespace AgentMesh.Application.Services
{
    public class FunctionalAnalystAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<FunctionalAnalystAgent> logger) : AgentBase<FunctionalAnalystAgent.ParsedResponse>(logger, AgentMesh.Application.Configuration.FunctionalAnalystAgentConfiguration.AgentName, openAIClientFactory, resilience)
    {
        private readonly ILogger<FunctionalAnalystAgent> _logger = logger;

        public async Task<FunctionalAnalystAgentOutput> ExecuteAsync(
            FunctionalAnalystAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}."
            };

            if (input.DoNotComment)
            {
                systemMessages.Add("IMPORTANT REQUIREMENT: In `businessRequirements`, you MUST EXPLICITLY instruct the Coder Agent to produce a program that ONLY RETURNS DATA and DOES NOT add comments, insights, explanations, or narrative text in its output.");
            }

            if (!string.IsNullOrWhiteSpace(input.KnowledgeBaseDocumentsContent))
            {
                systemMessages.Add($"IMPORTANT REQUIREMENT: The following knowledge base documents content is provided to you for reference. You MUST use this information to inform your response and ensure that the generated business requirements are accurate and relevant to the provided knowledge base content.\n{input.KnowledgeBaseDocumentsContent}");
            }

            var userPayload = new
            {
                input.Intent,
                input.ConversationTopic,
                input.UserRequestedActions,
                input.UserProvidedData,
                input.UserPreferences,
                input.AgentMemories
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

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
