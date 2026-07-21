using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.DomainExpert;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class DomainExpertAgent(
        [FromKeyedServices(AgentMesh.Application.Configuration.DomainExpertAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<DomainExpertAgent> logger) : AgentBase<DomainExpertAgent.ParsedResponse>(logger, AgentMesh.Application.Configuration.DomainExpertAgentConfiguration.AgentName, openAIClient, resilience), IDomainExpertAgent
    {
        private readonly ILogger<DomainExpertAgent> _logger = logger;

        public async Task<DomainExpertAgentOutput> ExecuteAsync(
            DomainExpertAgentInput input,
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

            if (!string.IsNullOrWhiteSpace(input.DataToComment))
            {
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"DataToComment: {input.DataToComment}" });
            }

            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            
            if (!string.IsNullOrWhiteSpace(input.LanguageOfTheUser))
            {
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"User's Language: {input.LanguageOfTheUser}" });
            }
            
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.DataToComment });

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


