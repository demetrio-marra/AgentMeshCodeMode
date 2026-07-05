using AgentMesh.Application.Configuration;
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
        [FromKeyedServices(DomainExpertAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<DomainExpertAgent> logger) : AgentBase<DomainExpertAgent.ParsedResponse>(logger, DomainExpertAgentConfiguration.AgentName, openAIClient, resilience), IDomainExpertAgent
    {
        private readonly ILogger<DomainExpertAgent> _logger = logger;
        private static readonly string[] AllowedQueryTypes = ["lex", "vec", "hyde"];

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

            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest });

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new DomainExpertAgentOutput
            {
                BusinessRequirements = result.Result.BusinessRequirements,
                KnowledgeBaseAPIQueries = result.Result.KnowledgeBaseAPIQueries,
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

                if (string.IsNullOrWhiteSpace(responseDTO.BusinessRequirements))
                {
                    _logger.LogWarning("The model's response contains empty business requirements. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty business requirements.");
                }

                responseDTO.KnowledgeBaseAPIQueries ??= [];

                if (!responseDTO.KnowledgeBaseAPIQueries.Any())
                {
                    _logger.LogWarning("The model's response contains empty knowledge base API queries. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty knowledge base API queries.");
                }

                if (responseDTO.KnowledgeBaseAPIQueries.Any(q => string.IsNullOrWhiteSpace(q.Query) || !AllowedQueryTypes.Contains(q.Type, StringComparer.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("The model's response contains invalid query entries. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains invalid query entries. Allowed types: lex, vec, hyde; query must be non-empty.");
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

            [JsonPropertyName("knowledgeBaseAPIQueries")]
            public IEnumerable<DomainExpertAgentOutput.KnowledgeBaseAPIQuery> KnowledgeBaseAPIQueries { get; set; } = [];
        }
    }
}
