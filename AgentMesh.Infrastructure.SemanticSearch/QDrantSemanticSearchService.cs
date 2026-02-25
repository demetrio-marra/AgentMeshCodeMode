using AgentMesh.Models;
using AgentMesh.Services;
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
        }

        public async Task<IEnumerable<SemanticSearchResult>> SearchByActionableRequirements(string agentRole,
            IEnumerable<string> actionableRequirements,
            CancellationToken cancellationToken = default)
        {
            var results = new List<string>();

            _logger.LogDebug("Fetching embeddings for agent {1} and actionable requirements: {0}...", string.Join(", ", actionableRequirements), agentRole);

            var embeddings = await _embeddingService.GetEmbeddingAsync(actionableRequirements);
            var searchPoints = embeddings.Select(vec => new SearchPoints
            {
                WithPayload = true,
                WithVectors = false,
                Limit = (ulong)_maxExtractedResults,
                //Filter = new Filter
                //{
                //    Must =
                //    {
                //        new Condition
                //        {
                //            Field = new FieldCondition
                //            {
                //                Key = "agentRole",
                //                Match = new Match
                //                {
                //                    Text = agentRole
                //                }
                //            }
                //        }
                //    }
                //}
            }).ToList();

            var li = 0;
            foreach (var emb in embeddings)
            {
                searchPoints[li].Vector.AddRange(emb);
                li++;
            }

            var batchSearchResult = await _qdrantClient.SearchBatchAsync(
                collectionName: "BusinessProcesses",
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
