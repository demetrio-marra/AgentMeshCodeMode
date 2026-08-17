using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.KnowledgeBaseQueryExpander;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Utils;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Models.Parameters;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class KnowledgeBaseQueryExpanderAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<KnowledgeBaseQueryExpanderAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<IEnumerable<KnowledgeBaseQueryExpanderOutputItem>>(logger, 
            "KnowledgeBaseQueryExpander",
            openAIClientFactory,
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<KnowledgeBaseQueryExpanderAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new () { ParameterName = RequestDateTimeParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = LanguageOfTheDocumentationParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = QMDQueryTypesDocumentationParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] }
                ];
        }

        protected override IEnumerable<KnowledgeBaseQueryExpanderOutputItem> ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, SerializationUtils.DefaultDeserializeOptions);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                var ret = responseDTO.SearchQueries.Select(query => new KnowledgeBaseQueryExpanderOutputItem
                {
                    Query = query.Query,
                    SearchType = query.Type
                }).ToList();

                return ret;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        public class ParsedResponse
        {
            [JsonPropertyName("searchQueries")]
            public IEnumerable<QueryItem> SearchQueries { get; set; } = [];
        }

        public class QueryItem
        {
            [JsonPropertyName("type")]
            public KnowledgeBaseQuerySearchType Type { get; set; }

            [JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;
        }
    }
}
