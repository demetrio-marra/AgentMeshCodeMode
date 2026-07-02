using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.SemanticSearch;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class QDrantSemanticSearchService : ISemanticSearchService
    {
        private readonly QdrantClient _qdrantClient;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<QDrantSemanticSearchService> _logger;
        private readonly int _maxExtractedResults;
        private readonly string _businessProcessesCollectionName;

        public QDrantSemanticSearchService(QDrantSemanticSearchServiceConfiguration configuration,
            IEmbeddingService embeddingService,
            ILogger<QDrantSemanticSearchService> logger)
        {
            _embeddingService = embeddingService;
            _logger = logger;
            _qdrantClient = new QdrantClient(
               host: configuration.Host,
               https: configuration.Https,
               port: configuration.Port
           );
            _maxExtractedResults = configuration.MaxResults;
            _businessProcessesCollectionName = configuration.BusinessProcessesCollectionName;

            _ = EnsureAgentRoleIndexAsync();
        }

        private async Task EnsureAgentRoleIndexAsync()
        {
            try
            {
                _logger.LogInformation("Creating payload index on 'agentRole' field for collection '{CollectionName}'...", _businessProcessesCollectionName);
                
                await _qdrantClient.CreatePayloadIndexAsync(
                    collectionName: _businessProcessesCollectionName,
                    fieldName: "agentRole",
                    schemaType: PayloadSchemaType.Keyword
                );

                _logger.LogInformation("Successfully created payload index on 'agentRole' field.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create payload index on 'agentRole' field. It may already exist or the collection may not be ready.");
            }
        }

        public async Task<IEnumerable<SemanticSearchResult>> SearchByActionableRequirements(IEnumerable<string> actionableRequirements,
            string? agentRole = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<string>();

            _logger.LogDebug("Fetching embeddings for agent {1} and actionable requirements: {0}...", string.Join(", ", actionableRequirements), agentRole);

            Filter? filter = null;
            if (agentRole != null)
            {
                filter = new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "agentRole",
                                Match = new Match
                                {
                                    Text = agentRole
                                }
                            }
                        }
                    }
                };
            }

            var embeddings = await _embeddingService.GetEmbeddingAsync(actionableRequirements);
            var searchPoints = embeddings.Select(vec => new SearchPoints
            {
                WithPayload = true,
                WithVectors = false,
                Limit = (ulong)_maxExtractedResults,
                Filter = filter
            }).ToList();

            var li = 0;
            foreach (var emb in embeddings)
            {
                searchPoints[li].Vector.AddRange(emb);
                li++;
            }

            var batchSearchResult = await _qdrantClient.SearchBatchAsync(
                collectionName: _businessProcessesCollectionName,
                searches: searchPoints,
                cancellationToken: cancellationToken
            );

            var rr = new List<SemanticSearchResult>();
            foreach (var searchResult in batchSearchResult)
            {
                foreach (var result in searchResult.Result)
                {
                    if (result.Payload.TryGetValue("text", out var extractedValue))
                    {
                        rr.Add(new SemanticSearchResult
                        {
                            FoundInformation = extractedValue.StringValue,
                            Relevance = result.Score
                        });
                    }
                }
            }

            var semanticSearchResults = rr.OrderByDescending(r => r.Relevance)
                .ToList();

            _logger.LogDebug("Found {0} relevant skills: {1}", semanticSearchResults.Count, string.Join(", ", semanticSearchResults.Select(r => r.FoundInformation)));

            return semanticSearchResults;
        }
    }
}
