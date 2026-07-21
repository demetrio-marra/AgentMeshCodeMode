using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AgentMesh.Application.Contracts;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.QueriesCache;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AgentMesh.Infrastructure.QDrant
{
    public class QDrantQueriesCacheService : IQueriesCacheService
    {
        private readonly QdrantClient _qdrantClient;
        private readonly ILogger<QDrantQueriesCacheService> _logger;
        private readonly string _queriesCacheCollectionName;
        private readonly float[] _defaultVector;
        private readonly IEmbeddingService _embeddingService;
        private int _maxResults;

        public QDrantQueriesCacheService(
            QDrantQueriesCacheServiceConfiguration configuration,
            IEmbeddingService embeddingService,
            ILogger<QDrantQueriesCacheService> logger)
        {
            _logger = logger;
            _embeddingService = embeddingService;
            _qdrantClient = new QdrantClient(
                host: configuration.Host,
                https: configuration.Https,
                port: configuration.Port);
            _queriesCacheCollectionName = configuration.QueriesCacheCollectionName;
            _defaultVector = new float[configuration.VectorSize];
            _maxResults = configuration.MaxResults;

            _ = EnsureQueryCacheIndexesAsync();
        }

        public async Task<KnowledgeBaseQueriesCacheResult> GetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueryInputItem> queries)
        {
            var requestedQueries = queries
                .Where(q => !string.IsNullOrWhiteSpace(q.Query))
                .Select(q => (QueryKind: MapQueryTypeToKind(q.SearchType), q.Query))
                .DistinctBy(q => $"{q.QueryKind}|{q.Query}", StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedQueries.Count == 0)
            {
                return new KnowledgeBaseQueriesCacheResult { Items = [] };
            }

            var (results, totalTokens) = await SearchByQueryKindsAsync(requestedQueries.Select(q => (q.QueryKind, q.Query)).ToList());
            var resultsByKind = results
                .GroupBy(r => r.QueryKind, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.DistinctBy(CreateStablePointId).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            var items = requestedQueries
                .SelectMany(request =>
                    resultsByKind.TryGetValue(request.QueryKind, out var matchingResults)
                        ? matchingResults.Select(result => MapToKnowledgeBaseCacheItem(result, request.Query, MapKindToQueryType(request.QueryKind)))
                        : Enumerable.Empty<KnowledgeBaseQueriesCacheItem>())
                .ToList();

            return new KnowledgeBaseQueriesCacheResult
            {
                TotalTokens = totalTokens,
                Items = items
            };
        }

        public async Task<AgentMemoryQueriesCacheResult> GetMemoryCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItemInput> queries)
        {
            var requestedQueries = queries
                .Where(q => !string.IsNullOrWhiteSpace(q.Query))
                .Select(q => q.Query)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedQueries.Count == 0)
            {
                return new AgentMemoryQueriesCacheResult { Items = [] };
            }

            var queryRequests = requestedQueries
                .Select(q => (QueryKind: QDrantQueriesCacheItem.AgentMemoryQueryKind, Query: q))
                .ToList();

            var (results, totalTokens) = await SearchByQueryKindsAsync(queryRequests);
            var distinctResults = results
                .DistinctBy(CreateStablePointId)
                .ToList();

            var items = requestedQueries
                .SelectMany(requestedQuery => distinctResults.Select(result => MapToAgentMemoryCacheItem(result, requestedQuery)))
                .ToList();

            return new AgentMemoryQueriesCacheResult
            {
                TotalTokens = totalTokens,
                Items = items
            };
        }

        public async Task<QueryCacheUpdateResult> SetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueriesCacheItem> cachedItems)
        {
            var entities = cachedItems
                .Where(item => !string.IsNullOrWhiteSpace(item.FoundQuery))
                .Select(MapFromKnowledgeBaseCacheItem)
                .ToList();

            var totalTokens = await UpsertAsync(entities);

            return new QueryCacheUpdateResult
            {
                TotalTokens = totalTokens
            };
        }

        public async Task<QueryCacheUpdateResult> SetMemoryCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItem> cachedItems)
        {
            var entities = cachedItems
                .Where(item => !string.IsNullOrWhiteSpace(item.FoundQuery))
                .Select(MapFromAgentMemoryCacheItem)
                .ToList();

            var totalTokens = await UpsertAsync(entities);

            return new QueryCacheUpdateResult
            {
                TotalTokens = totalTokens
            };
        }

        private async Task EnsureQueryCacheIndexesAsync()
        {
            await TryCreatePayloadIndexAsync("queryKind");
            await TryCreatePayloadIndexAsync("query");
        }

        private async Task TryCreatePayloadIndexAsync(string fieldName)
        {
            try
            {
                _logger.LogInformation("Creating payload index on '{FieldName}' for collection '{CollectionName}'...", fieldName, _queriesCacheCollectionName);

                await _qdrantClient.CreatePayloadIndexAsync(
                    collectionName: _queriesCacheCollectionName,
                    fieldName: fieldName,
                    schemaType: PayloadSchemaType.Keyword);

                _logger.LogInformation("Successfully created payload index on '{FieldName}' field.", fieldName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create payload index on '{FieldName}' field. It may already exist or the collection may not be ready.", fieldName);
            }
        }

        private async Task<(IReadOnlyCollection<QDrantQueriesCacheItem> Results, int TotalTokens)> SearchByQueryKindsAsync(IReadOnlyCollection<(string QueryKind, string Query)> queryRequests)
        {
            if (queryRequests.Count == 0)
            {
                return ([], 0);
            }

            var normalizedRequests = queryRequests
                .Where(q => !string.IsNullOrWhiteSpace(q.Query) && !string.IsNullOrWhiteSpace(q.QueryKind))
                .DistinctBy(q => $"{q.QueryKind}|{q.Query}", StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedRequests.Count == 0)
            {
                return ([], 0);
            }

            var embeddings = (await _embeddingService.GetEmbeddingAsync(normalizedRequests.Select(q => q.Query))).ToList();
            var totalTokens = embeddings.FirstOrDefault()?.TotalTokens ?? 0;
            var searches = new List<SearchPoints>(normalizedRequests.Count);

            for (var i = 0; i < normalizedRequests.Count; i++)
            {
                var request = normalizedRequests[i];

                var filter = new Filter();
                filter.Must.Add(CreateKeywordCondition("queryKind", request.QueryKind));

                var search = new SearchPoints
                {
                    WithPayload = true,
                    WithVectors = false,
                    Limit = (ulong)_maxResults,
                    Filter = filter
                };

                search.Vector.AddRange(embeddings[i].Embedding);
                searches.Add(search);
            }

            var batchSearchResult = await _qdrantClient.SearchBatchAsync(
                collectionName: _queriesCacheCollectionName,
                searches: searches);

            var results = batchSearchResult
                .SelectMany(batch => batch.Result)
                .Select(MapFromScoredPoint)
                .ToList();

            return (results, totalTokens);
        }

        private async Task<int> UpsertAsync(IReadOnlyCollection<QDrantQueriesCacheItem> entities)
        {
            if (entities.Count == 0)
            {
                return 0;
            }

            var distinctEntities = entities
                .DistinctBy(CreateStablePointId)
                .ToList();

            var queries = distinctEntities.Select(e => e.Query).ToList();
            var embeddings = (await _embeddingService.GetEmbeddingAsync(queries)).ToList();

            var points = new List<PointStruct>(distinctEntities.Count);
            for (var i = 0; i < distinctEntities.Count; i++)
            {
                var vector = i < embeddings.Count ? embeddings[i].Embedding : _defaultVector;
                points.Add(CreatePoint(distinctEntities[i], vector));
            }

            await _qdrantClient.UpsertAsync(_queriesCacheCollectionName, points);

            return embeddings.Sum(e => e.TotalTokens);
        }

        private PointStruct CreatePoint(QDrantQueriesCacheItem entity, float[] vector)
        {
            var point = new PointStruct
            {
                Id = CreateStablePointId(entity),
                Vectors = vector
            };

            point.Payload.Add("queryKind", entity.QueryKind);
            point.Payload.Add("query", entity.Query);
            point.Payload.Add("result", entity.Result);
            point.Payload.Add("queryType", entity.QueryType?.ToString() ?? string.Empty);
            point.Payload.Add("documentId", entity.DocumentId);
            point.Payload.Add("documentTitle", entity.DocumentTitle);
            point.Payload.Add("documentFile", entity.DocumentFile);
            point.Payload.Add("lastUpdate", entity.LastUpdate.ToString("O", CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(entity.DocumentSummary))
            {
                point.Payload.Add("documentSummary", entity.DocumentSummary);
            }

            return point;
        }

        private static Condition CreateKeywordCondition(string key, string value)
        {
            return new Condition
            {
                Field = new FieldCondition
                {
                    Key = key,
                    Match = new Match
                    {
                        Keyword = value
                    }
                }
            };
        }

        private static QDrantQueriesCacheItem MapFromAgentMemoryCacheItem(AgentMemoryQueriesCacheItem item)
        {
            return new QDrantQueriesCacheItem
            {
                Query = item.FoundQuery,
                QueryKind = QDrantQueriesCacheItem.AgentMemoryQueryKind,
                Result = item.Result,
                LastUpdate = DateTime.UtcNow
            };
        }

        private static QDrantQueriesCacheItem MapFromKnowledgeBaseCacheItem(KnowledgeBaseQueriesCacheItem item)
        {
            return new QDrantQueriesCacheItem
            {
                Query = item.FoundQuery,
                QueryKind = MapQueryTypeToKind(item.FoundQueryType),
                QueryType = item.FoundQueryType,
                DocumentId = item.DocumentId,
                DocumentTitle = item.DocumentTitle,
                DocumentSummary = item.DocumentSummary,
                DocumentFile = item.DocumentFile,
                LastUpdate = DateTime.UtcNow
            };
        }

        private static AgentMemoryQueriesCacheItem MapToAgentMemoryCacheItem(QDrantQueriesCacheItem item, string searchedQuery)
        {
            return new AgentMemoryQueriesCacheItem
            {
                FoundQuery = item.Query,
                SearchedQuery = searchedQuery,
                Result = item.Result,
                Relevance = item.Relevance
            };
        }

        private static KnowledgeBaseQueriesCacheItem MapToKnowledgeBaseCacheItem(
            QDrantQueriesCacheItem item,
            string searchedQuery,
            KnowledgeBaseQuerySearchType searchedQueryType)
        {
            return new KnowledgeBaseQueriesCacheItem
            {
                FoundQuery = item.Query,
                FoundQueryType = item.QueryType ?? MapKindToQueryType(item.QueryKind),
                SearchedQuery = searchedQuery,
                SearchedQueryType = searchedQueryType,
                DocumentId = item.DocumentId,
                DocumentTitle = item.DocumentTitle,
                DocumentSummary = item.DocumentSummary,
                DocumentFile = item.DocumentFile,
                Relevance = item.Relevance
            };
        }

        private static QDrantQueriesCacheItem MapFromScoredPoint(ScoredPoint point)
        {
            return new QDrantQueriesCacheItem
            {
                QueryKind = GetStringPayloadValue(point.Payload, "queryKind"),
                Query = GetStringPayloadValue(point.Payload, "query"),
                Result = GetStringPayloadValue(point.Payload, "result"),
                QueryType = GetQueryType(point.Payload, "queryType"),
                DocumentId = GetStringPayloadValue(point.Payload, "documentId"),
                DocumentTitle = GetStringPayloadValue(point.Payload, "documentTitle"),
                DocumentSummary = GetNullableStringPayloadValue(point.Payload, "documentSummary"),
                DocumentFile = GetStringPayloadValue(point.Payload, "documentFile"),
                LastUpdate = GetDateTimePayloadValue(point.Payload, "lastUpdate"),
                Relevance = point.Score
            };
        }

        private static string GetStringPayloadValue(IDictionary<string, Value> payload, string key)
        {
            return payload.TryGetValue(key, out var value)
                ? value.StringValue
                : string.Empty;
        }

        private static string? GetNullableStringPayloadValue(IDictionary<string, Value> payload, string key)
        {
            return payload.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.StringValue)
                ? value.StringValue
                : null;
        }

        private static DateTime GetDateTimePayloadValue(IDictionary<string, Value> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value.StringValue))
            {
                return DateTime.UtcNow;
            }

            return DateTime.TryParse(value.StringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : DateTime.UtcNow;
        }

        private static KnowledgeBaseQuerySearchType? GetQueryType(IDictionary<string, Value> payload, string key)
        {
            if (!payload.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value.StringValue))
            {
                return null;
            }

            return Enum.TryParse<KnowledgeBaseQuerySearchType>(value.StringValue, ignoreCase: true, out var queryType)
                ? queryType
                : null;
        }

        private static string MapQueryTypeToKind(KnowledgeBaseQuerySearchType queryType)
        {
            return queryType switch
            {
                KnowledgeBaseQuerySearchType.Semantic => QDrantQueriesCacheItem.SemanticQueryKind,
                KnowledgeBaseQuerySearchType.Keyword => QDrantQueriesCacheItem.KeywordsQueryKind,
                KnowledgeBaseQuerySearchType.HypotheticalDocument => QDrantQueriesCacheItem.HydeQueryKind,
                _ => throw new ArgumentOutOfRangeException(nameof(queryType), queryType, null)
            };
        }

        private static KnowledgeBaseQuerySearchType MapKindToQueryType(string queryKind)
        {
            return queryKind switch
            {
                QDrantQueriesCacheItem.SemanticQueryKind => KnowledgeBaseQuerySearchType.Semantic,
                QDrantQueriesCacheItem.KeywordsQueryKind => KnowledgeBaseQuerySearchType.Keyword,
                QDrantQueriesCacheItem.HydeQueryKind => KnowledgeBaseQuerySearchType.HypotheticalDocument,
                _ => throw new ArgumentOutOfRangeException(nameof(queryKind), queryKind, null)
            };
        }

        private static Guid CreateStablePointId(QDrantQueriesCacheItem item)
        {
            var rawId = string.Join("|",
                item.QueryKind,
                item.Query,
                item.Result,
                item.QueryType?.ToString() ?? string.Empty,
                item.DocumentId,
                item.DocumentTitle,
                item.DocumentSummary ?? string.Empty,
                item.DocumentFile);

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawId));
            return new Guid(hash.AsSpan(0, 16));
        }
    }
}
