using System.Net.Http.Json;
using System.Text.Json;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Utils;
using AgentMesh.Infrastructure.LightRag.Configuration;
using AgentMesh.Infrastructure.LightRag.DTOs.QueryData;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Infrastructure.LightRag.Services
{
    public class LightRagKnowledgeService : IKnowledgeService
    {
        private const string QueryDataEndpoint = "/query/data";

        private readonly HttpClient _httpClient;
        private readonly int _maxResults;
        private readonly string? _apiKey;
        private readonly Resilience _resilience;
        private readonly ILogger<LightRagKnowledgeService> _logger;

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LightRagKnowledgeService(
            HttpClient httpClient,
            LightRagServiceConfiguration configuration,
            Resilience resilience,
            ILogger<LightRagKnowledgeService> logger)
        {
            _httpClient = httpClient;
            _maxResults = configuration.MaxTopK;
            _apiKey = configuration.ApiKey;
            _resilience = resilience;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(configuration.BaseUrl);
        }

        public async Task<KnowledgeQueryResult> QueryKnowledgeAsync(KnowledgeQuery query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var request = new QueryDataRequestDto
            {
                Query = query.QueryText,
                Mode = query.QueryRetrievalKind == KnowledgeQueryRetrievalKind.SemanticOnly ? "naive" : "mix",
                TopK = query.MaxResults > _maxResults ? _maxResults : query.MaxResults,
                HighLevelKeywords = [.. query.PrimaryRelevanceKeywords],
                LowLevelKeywords = [.. query.SecondaryRelevanceKeywords],
                IncludeReferences = true,
                IncludeChunkContent = true
            };

            using var response = await _resilience.SendWithRetryAsync(
                async () =>
                {
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Post, QueryDataEndpoint)
                    {
                        Content = JsonContent.Create(request, options: JsonSerializerOptions)
                    };

                    if (!string.IsNullOrWhiteSpace(_apiKey))
                    {
                        requestMessage.Headers.TryAddWithoutValidation("X-API-Key", _apiKey);
                    }

                    return await _httpClient.SendAsync(requestMessage, cancellationToken);
                },
                nameof(QueryKnowledgeAsync),
                _logger);
            response.EnsureSuccessStatusCode();

            var responseDto = await response.Content.ReadFromJsonAsync<QueryDataResponseDto>(JsonSerializerOptions, cancellationToken);
            if (responseDto?.Data == null)
            {
                return new KnowledgeQueryResult();
            }

            var contentById = responseDto.Data.Chunks
                .Where(c => !string.IsNullOrWhiteSpace(c.ChunkId))
                .ToDictionary(
                    c => c.ChunkId!,
                    c => new KnowledgeContentItem
                    {
                        Id = c.ChunkId!,
                        Content = c.Content ?? string.Empty,
                        Source = c.FilePath ?? string.Empty
                    });

            var contents = responseDto.Data.Chunks
                .Select(c => new KnowledgeContentItem
                {
                    Id = c.ChunkId ?? string.Empty,
                    Content = c.Content ?? string.Empty,
                    Source = c.FilePath ?? string.Empty
                })
                .ToList();

            var entities = query.IncludeEntities
                ? responseDto.Data.Entities.Select(e => new KnowledgeEntityItem
                {
                    Entity = e.EntityName,
                    Description = e.Description ?? string.Empty,
                    Type = e.EntityType ?? string.Empty,
                    ContentItem = GetContentItem(e.SourceId, e.FilePath, contentById)
                }).ToList()
                : [];

            var relations = query.IncludeRelations
                ? responseDto.Data.Relationships.Select(r => new KnowledgeRelationItem
                {
                    Description = r.Description ?? string.Empty,
                    Keywords = r.Keywords ?? string.Empty,
                    EntityRelationFrom = r.SourceEntity,
                    EntityRelationTo = r.TargetEntity,
                    ContentItem = GetContentItem(r.SourceId, r.FilePath, contentById)
                }).ToList()
                : [];

            return new KnowledgeQueryResult
            {
                Contents = contents,
                Entities = query.IncludeEntities ? entities : [],
                Relations = query.IncludeRelations ? relations : []
            };
        }

        private static KnowledgeContentItem GetContentItem(string? sourceId, string? filePath, IReadOnlyDictionary<string, KnowledgeContentItem> contentById)
        {
            if (!string.IsNullOrWhiteSpace(sourceId) && contentById.TryGetValue(sourceId, out var contentItem))
            {
                return contentItem;
            }

            return new KnowledgeContentItem
            {
                Id = sourceId ?? string.Empty,
                Source = filePath ?? string.Empty,
                Content = string.Empty
            };
        }
    }
}
