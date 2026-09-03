using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Rerank;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.Cohere
{
    public class CohereV1RerankerService : IRerankerService
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly CohereV1RerankerServiceConfiguration _configuration;

        public CohereV1RerankerService(HttpClient httpClient, CohereV1RerankerServiceConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);
        }

        public async Task<RerankResult> RerankAsync(RerankInputQuery inputQuery, CancellationToken cancellationToken = default)
        {
            var documents = inputQuery.CandidateDocuments
                .Where(document => !string.IsNullOrWhiteSpace(document))
                .ToList();

            if (documents.Count == 0)
            {
                return new RerankResult
                {
                    RerankedDocuments = [],
                    CompletionTokens = 0,
                    PromptTokens = 0
                };
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/rerank")
            {
                Content = JsonContent.Create(new RerankRequestDto
                {
                    Query = inputQuery.Query,
                    Documents = documents,
                    Model = _configuration.Model,
                    TopN = inputQuery.TopN,
                    ReturnDocuments = true
                }, options: JsonSerializerOptions)
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);

            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseDto = await response.Content.ReadFromJsonAsync<RerankResponseDto>(JsonSerializerOptions, cancellationToken);

            var rerankedDocuments = responseDto?.Data?
                .Select(item => new RerankResultItem(
                    item.Index,
                    item.Document ?? string.Empty,
                    item.RelevanceScore))
                .ToList() ?? [];

            return new RerankResult
            {
                RerankedDocuments = rerankedDocuments,
                CompletionTokens = responseDto?.Usage?.CompletionTokens ?? 0,
                PromptTokens = responseDto?.Usage?.PromptTokens ?? 0
            };
        }

        private sealed class RerankRequestDto
        {
            public required string Query { get; init; }
            public required List<string> Documents { get; init; }
            public required string Model { get; init; }
            public int? TopN { get; init; }
            public bool ReturnDocuments { get; init; }
        }

        private sealed class RerankResponseDto
        {
            public List<RerankResponseDataItemDto>? Data { get; init; }
            public RerankResponseUsageDto? Usage { get; init; }
        }

        private sealed class RerankResponseDataItemDto
        {
            public string? Document { get; init; }
            public int Index { get; init; }
            public double RelevanceScore { get; init; }
        }

        private sealed class RerankResponseUsageDto
        {
            public int CompletionTokens { get; init; }
            public int PromptTokens { get; init; }
            public int TotalTokens { get; init; }
        }
    }
}
