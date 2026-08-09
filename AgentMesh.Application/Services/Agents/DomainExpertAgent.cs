using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.DomainExpert;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Application.Models.ChatMessages;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class DomainExpertAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<DomainExpertAgent> logger) : AgentBase<DomainExpertAgent.ParsedResponse>(logger,
            "DomainExpert", 
            openAIClientFactory, 
            resilience)
    {
        private readonly ILogger<DomainExpertAgent> _logger = logger;

        public async Task<DomainExpertAgentOutput> ExecuteAsync(
            DomainExpertAgentInput input,
            CancellationToken cancellationToken = default)
        {
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
                input.Intent,
                input.ConversationTopic,
                input.UserRequestedActions,
                input.UserProvidedData,
                input.UserPreferences,
                input.AgentMemories,
                input.DataToComment
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new DomainExpertAgentOutput
            {
                DomainExpertComment = result.Result.DomainExpertComment,
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

                if (string.IsNullOrWhiteSpace(responseDTO.DomainExpertComment))
                {
                    _logger.LogWarning("The model's response contains empty domain expert comment. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty domain expert comment.");
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
            [JsonPropertyName("domainExpertComment")]
            public string DomainExpertComment { get; set; } = string.Empty;
        }
    }
}
