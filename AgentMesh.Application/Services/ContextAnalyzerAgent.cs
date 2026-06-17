using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.ContextAnalyzer;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using static AgentMesh.Models.ContextAnalyzer.ContextAnalyzerAgentOutput;

namespace AgentMesh.Application.Services
{
    public class ContextAnalyzerAgent : AgentBase<ContextAnalyzerAgentOutput>, IContextAnalyzerAgent
    {
        public const string AgentName = "Context Analyzer";

        public ContextAnalyzerAgent(
            [FromKeyedServices(ContextAnalyzerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ContextAnalyzerAgentConfiguration configuration,
            ILogger<ContextAnalyzerAgent> logger) : base(logger, ContextAnalyzerAgentConfiguration.AgentName, openAIClient)
        {
        }

        public async Task<ContextAnalyzerAgentOutput> ExecuteAsync(
            ContextAnalyzerAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(input) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new ContextAnalyzerAgentOutput
            {
                CondensedUserIntent = result.Result.CondensedUserIntent,
                UserIntentCategory = result.Result.UserIntentCategory,
                FilteredKnowledgeBaseDocuments = result.Result.FilteredKnowledgeBaseDocuments,

                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override ContextAnalyzerAgentOutput ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ContextAnalyzerAgentOutputDTO>(rawResponseText);

                if (responseDTO == null)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.CondensedUserIntent))
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty condensed user intent.");
                }

                return new ContextAnalyzerAgentOutput
                {
                    CondensedUserIntent = responseDTO.CondensedUserIntent,
                    UserIntentCategory = Enum.Parse<UserIntentCategoryValues>(responseDTO.UserIntentCategory, true),
                    FilteredKnowledgeBaseDocuments = responseDTO.FilteredKnowledgeBaseDocuments.Select(u => new FilteredKnowledgeBaseItem
                    {
                        Title = u.Title,
                        DocumentId = u.DocumentId
                    }).ToList()
                };
            }
            catch (JsonException ex)
            {
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private class ContextAnalyzerAgentOutputDTO
        {
            [JsonPropertyName("condensedUserIntent")]
            public string CondensedUserIntent { get; set; } = string.Empty;

            [JsonPropertyName("userIntentCategory")]
            public string UserIntentCategory { get; set; } = string.Empty;

            [JsonPropertyName("filteredKnowledgeBaseDocuments")]
            public List<FilteredKnowledgeBaseDTOItem> FilteredKnowledgeBaseDocuments { get; set; } = new List<FilteredKnowledgeBaseDTOItem>();
        }

        private class FilteredKnowledgeBaseDTOItem
        {
            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("documentId")]
            public string DocumentId { get; set; } = string.Empty;
        }
    }
}
