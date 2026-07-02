using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AgentMesh.Application.Contracts;
using AgentMesh.Infrastructure.QDrant.Models;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.QueriesCache;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AgentMesh.Infrastructure.QDrant
{
    public class QDrantQueriesCacheService : IQueriesCacheService
    {
        private const uint ScrollPageSize = 256;

        private readonly QdrantClient _qdrantClient;
        private readonly ILogger<QDrantQueriesCacheService> _logger;
        private readonly string _queriesCacheCollectionName;
        private readonly float[] _defaultVector;

        public QDrantQueriesCacheService(
            QDrantSemanticSearchServiceConfiguration configuration,
            ILogger<QDrantQueriesCacheService> logger)
        {
            _logger = logger;
            _qdrantClient = new QdrantClient(
                host: configuration.Host,
                https: configuration.Https,
                port: configuration.Port);
            _queriesCacheCollectionName = configuration.QueriesCacheCollectionName;
            _defaultVector = new float[configuration.VectorSize];

            _ = EnsureQueryCacheIndexesAsync();
        }

        public async Task<IEnumerable<KnowledgeBaseQueriesCacheItem>> GetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueriesCacheItemInput> queries)
        {
            var requestedQueries = queries
                .Where(q => !string.IsNullOrWhiteSpace(q.Query))
                .GroupBy(q => MapQueryTypeToKind(q.QueryType))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(q => q.Query).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            if (requestedQueries.Count == 0)
            {
                return [];
            }

            var results = new List<QDrantQueriesCacheItem>();
            foreach (var queryGroup in requestedQueries)
            {
                results.AddRange(await ScrollByQueryKindAsync(queryGroup.Key, queryGroup.Value));
            }

            return results
                .Select(MapToKnowledgeBaseCacheItem)
                .ToList();
        }

        public async Task<IEnumerable<AgentMemoryQueriesCacheItem>> GetMemoryCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItemInput> queries)
        {
            var requestedQueries = queries
                .Where(q => !string.IsNullOrWhiteSpace(q.Query))
                .Select(q => q.Query)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedQueries.Count == 0)
            {
                return [];
            }

            var results = await ScrollByQueryKindAsync(QDrantQueriesCacheItem.AgentMemoryQueryKind, requestedQueries);

            return results
                .Select(MapToAgentMemoryCacheItem)
                .ToList();
        }

        public async Task SetKnowledgeBaseCachedItemsAsync(IEnumerable<KnowledgeBaseQueriesCacheItem> cachedItems)
        {
            var entities = cachedItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Query))
                .Select(MapFromKnowledgeBaseCacheItem)
                .ToList();

            await UpsertAsync(entities);
        }

        public async Task SetMemoryCachedItemsAsync(IEnumerable<AgentMemoryQueriesCacheItem> cachedItems)
        {
            var entities = cachedItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Query))
                .Select(MapFromAgentMemoryCacheItem)
                .ToList();

            await UpsertAsync(entities);
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

        private async Task<IReadOnlyCollection<QDrantQueriesCacheItem>> ScrollByQueryKindAsync(string queryKind, IReadOnlyCollection<string> queries)
        {
            if (queries.Count == 0)
            {
                return [];
            }

            var filter = new Filter();
            filter.Must.Add(CreateKeywordCondition("queryKind", queryKind));

            foreach (var query in queries)
            {
                filter.Should.Add(CreateKeywordCondition("query", query));
            }

            return await ScrollAllAsync(filter);
        }

        private async Task<IReadOnlyCollection<QDrantQueriesCacheItem>> ScrollAllAsync(Filter filter)
        {
            var results = new List<QDrantQueriesCacheItem>();
            PointId? offset = null;

            do
            {
                var scrollResult = await _qdrantClient.ScrollAsync(
                    _queriesCacheCollectionName,
                    filter,
                    ScrollPageSize,
                    offset,
                    true,
                    false);

                var points = scrollResult.Result;
                results.AddRange(points.Select(MapFromRetrievedPoint));
                offset = scrollResult.NextPageOffset;
            }
            while (offset != null);

            return results;
        }

        private async Task UpsertAsync(IReadOnlyCollection<QDrantQueriesCacheItem> entities)
        {
            if (entities.Count == 0)
            {
                return;
            }

            var points = entities
                .DistinctBy(CreateStablePointId)
                .Select(CreatePoint)
                .ToList();

            await _qdrantClient.UpsertAsync(_queriesCacheCollectionName, points);
        }

        private PointStruct CreatePoint(QDrantQueriesCacheItem entity)
        {
            var point = new PointStruct
            {
                Id = CreateStablePointId(entity),
                Vectors = _defaultVector
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
                Query = item.Query,
                QueryKind = QDrantQueriesCacheItem.AgentMemoryQueryKind,
                Result = item.Result,
                LastUpdate = DateTime.UtcNow
            };
        }

        private static QDrantQueriesCacheItem MapFromKnowledgeBaseCacheItem(KnowledgeBaseQueriesCacheItem item)
        {
            return new QDrantQueriesCacheItem
            {
                Query = item.Query,
                QueryKind = MapQueryTypeToKind(item.QueryType),
                QueryType = item.QueryType,
                DocumentId = item.DocumentId,
                DocumentTitle = item.DocumentTitle,
                DocumentSummary = item.DocumentSummary,
                DocumentFile = item.DocumentFile,
                LastUpdate = DateTime.UtcNow
            };
        }

        private static AgentMemoryQueriesCacheItem MapToAgentMemoryCacheItem(QDrantQueriesCacheItem item)
        {
            return new AgentMemoryQueriesCacheItem
            {
                Query = item.Query,
                Result = item.Result
            };
        }

        private static KnowledgeBaseQueriesCacheItem MapToKnowledgeBaseCacheItem(QDrantQueriesCacheItem item)
        {
            return new KnowledgeBaseQueriesCacheItem
            {
                Query = item.Query,
                QueryType = item.QueryType ?? MapKindToQueryType(item.QueryKind),
                DocumentId = item.DocumentId,
                DocumentTitle = item.DocumentTitle,
                DocumentSummary = item.DocumentSummary,
                DocumentFile = item.DocumentFile
            };
        }

        private static QDrantQueriesCacheItem MapFromRetrievedPoint(RetrievedPoint point)
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
                LastUpdate = GetDateTimePayloadValue(point.Payload, "lastUpdate")
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
