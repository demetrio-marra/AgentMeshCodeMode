using System.Net.Http.Json;
using System.Text.Json;
using AgentMesh.Application.Contracts;
using AgentMesh.Infrastructure.AgentMemory.Configuration;
using AgentMesh.Infrastructure.AgentMemory.Models;
using AgentMesh.Models;

namespace AgentMesh.Infrastructure.AgentMemory.Services
{
    public class Mem0AgentMemoryService : IAgentMemoryService
    {
        private readonly HttpClient _httpClient;
        private readonly AgentMemoryServiceConfiguration _configuration;
        private readonly JsonSerializerOptions _jsonOptions;

        public Mem0AgentMemoryService(
            HttpClient httpClient,
            AgentMemoryServiceConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task AddChatInteraction(
            string userId,
            string userMessage,
            string agentResponse,
            CancellationToken cancellationToken = default)
        {
            var request = new MemoryCreateRequest
            {
                Messages = new List<Message>
                {
                    new Message { Role = "user", Content = userMessage },
                    new Message { Role = "assistant", Content = agentResponse }
                },
                UserId = userId
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/memories",
                request,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<AgentMemoryItem>> Query(
            string userId,
            string query,
            CancellationToken cancellationToken = default)
        {
            var searchRequest = new SearchRequest
            {
                Query = query,
                UserId = userId
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/search",
                searchRequest,
                _jsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<SearchResponse>(
                _jsonOptions,
                cancellationToken);

            if (searchResponse?.Results == null)
            {
                return Enumerable.Empty<AgentMemoryItem>();
            }

            return searchResponse.Results.Select(r => new AgentMemoryItem
            {
                Memory = r.Memory,
                Confidence = r.Score
            });
        }
    }
}
