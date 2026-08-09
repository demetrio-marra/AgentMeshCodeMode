using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Embedding;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Executors
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly EmbeddingServiceConfiguration _configuration;

        public EmbeddingService(HttpClient httpClient, EmbeddingServiceConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _httpClient.BaseAddress = new Uri(_configuration.ModelEndpoint);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_configuration.ApiKey}");
        }

        public async Task<EmbeddingResult> GetEmbeddingAsync(string input, CancellationToken cancellationToken = default)
        {
            var payload = new EmbeddingPayload
            {
                Input = input,
                Model = _configuration.ModelName
            };

            var response = await _httpClient.PostAsJsonAsync("", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<EmbeddingApiResponse>(cancellationToken: cancellationToken);

            if (data?.Data.Length == 0
                || data?.Data[0].Embedding.Length == 0)
            {
                return new EmbeddingResult
                {
                    Embedding = Array.Empty<float>(),
                    TotalTokens = data?.Usage?.TotalTokens ?? 0
                };
            }

            return new EmbeddingResult
            {
                Embedding = data!.Data[0].Embedding,
                TotalTokens = data.Usage?.TotalTokens ?? 0
            };
        }

        public async Task<IEnumerable<EmbeddingResult>> GetEmbeddingAsync(IEnumerable<string> inputs, CancellationToken cancellationToken = default)
        {
            var payload = new EmbeddingsPayload
            {
                Input = inputs,
                Model = _configuration.ModelName
            };

            var response = await _httpClient.PostAsJsonAsync("", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<EmbeddingApiResponse>(cancellationToken: cancellationToken);

            var ret = data!.Data.Select(d => new EmbeddingResult
            {
                Embedding = d.Embedding,
                TotalTokens = data.Usage?.TotalTokens ?? 0
            }).AsEnumerable();
            return ret;
        }

        private class EmbeddingPayload
        {
            public string Input { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
        }

        private class EmbeddingsPayload
        {
            public IEnumerable<string> Input { get; set; } = Array.Empty<string>();
            public string Model { get; set; } = string.Empty;
        }

        private class EmbeddingApiResponse
        {
            [JsonPropertyName("data")]
            public EmbeddingData[] Data { get; set; } = Array.Empty<EmbeddingData>();

            [JsonPropertyName("usage")]
            public UsageData? Usage { get; set; }
        }

        private class EmbeddingData
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }

        private class UsageData
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("prompt_tokens_details")]
            public object? PromptTokensDetails { get; set; }
        }
    }
}
