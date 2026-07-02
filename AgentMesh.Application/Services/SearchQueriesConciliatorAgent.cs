using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Configuration;
using AgentMesh.Models.SearchQueriesConciliator;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using static AgentMesh.Models.SearchQueriesConciliator.SearchQueriesConciliatorAgentOutput;

namespace AgentMesh.Application.Services
{
    public class SearchQueriesConciliatorAgent : AgentBase<SearchQueriesConciliatorAgent.ConciliationResult>, ISearchQueriesConciliatorAgent
    {
        private readonly ILogger<SearchQueriesConciliatorAgent> _logger;

        public SearchQueriesConciliatorAgent(
            [FromKeyedServices(SearchQueriesConciliatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            SearchQueriesConciliatorAgentConfiguration configuration,
            Resilience resilience,
            ILogger<SearchQueriesConciliatorAgent> logger) : base(logger, SearchQueriesConciliatorAgentConfiguration.AgentName, openAIClient, resilience)
        {
            _logger = logger;
        }

        public async Task<SearchQueriesConciliatorAgentOutput> ExecuteAsync(
            SearchQueriesConciliatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var extractedKbQueries = string.Join("\n", input.ExtractedKnowledgeBaseSearchQueries.Select(q => $"- Type: {q.Type}, Query: {q.Query}"));
            var cachedKbQueries = string.Join("\n", input.CachedKnowledgeBaseSearchQueries.Select(q => $"- Type: {q.Type}, Query: {q.Query}"));
            var extractedMemoryQueries = string.Join("\n", input.ExtractedMemorySearchQueries.Select(m => $"- {m.Query}"));
            var cachedMemoryQueries = string.Join("\n", input.CachedMemorySearchQueries.Select(m => $"- {m.Query}"));

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = $"Extracted knowledge base search queries:\n{extractedKbQueries}\n\nCached knowledge base search queries:\n{cachedKbQueries}\n\nExtracted memory search queries:\n{extractedMemoryQueries}\n\nCached memory search queries:\n{cachedMemoryQueries}" }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new SearchQueriesConciliatorAgentOutput
            {
                ConciliatedKnowledgeBaseSearchQueries = result.Result.ConciliatedKnowledgeBaseSearchQueries,
                ConciliatedMemorySearchQueries = result.Result.ConciliatedMemorySearchQueries,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override ConciliationResult ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (!responseDTO.ConciliatedKnowledgeBaseSearchQueries.Any() && !responseDTO.ConciliatedMemorySearchQueries.Any())
                {
                    _logger.LogWarning("The model's response contains empty conciliated data. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty conciliated knowledge base search queries and memory search queries.");
                }

                return new ConciliationResult
                {
                    ConciliatedKnowledgeBaseSearchQueries = responseDTO.ConciliatedKnowledgeBaseSearchQueries,
                    ConciliatedMemorySearchQueries = responseDTO.ConciliatedMemorySearchQueries
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        public class ConciliationResult
        {
            public IEnumerable<KnowledgeBaseSearchQuery> ConciliatedKnowledgeBaseSearchQueries { get; set; } = [];
            public IEnumerable<MemorySearchQuery> ConciliatedMemorySearchQueries { get; set; } = [];
        }

        public class ParsedResponse
        {
            [JsonPropertyName("conciliatedKnowledgeBaseSearchQueries")]
            public IEnumerable<KnowledgeBaseSearchQuery> ConciliatedKnowledgeBaseSearchQueries { get; set; } = [];

            [JsonPropertyName("conciliatedMemorySearchQueries")]
            public IEnumerable<MemorySearchQuery> ConciliatedMemorySearchQueries { get; set; } = [];
        }
    }
}
