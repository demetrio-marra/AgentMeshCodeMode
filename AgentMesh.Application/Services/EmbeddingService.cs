using AgentMesh.Application.Configuration;
using AgentMesh.Services;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
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

        public async Task<float[]> GetEmbeddingAsync(string input, CancellationToken cancellationToken = default)
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
                return Array.Empty<float>();
            }

            return data!.Data[0].Embedding;
        }

        public async Task<IEnumerable<float[]>> GetEmbeddingAsync(IEnumerable<string> inputs, CancellationToken cancellationToken = default)
        {
            var payload = new EmbeddingsPayload
            {
                Input = inputs,
                Model = _configuration.ModelName
            };

            var response = await _httpClient.PostAsJsonAsync("", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<EmbeddingApiResponse>(cancellationToken: cancellationToken);

            var ret = data.Data.Select(d => d.Embedding).AsEnumerable();
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
        }

        private class EmbeddingData
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}
