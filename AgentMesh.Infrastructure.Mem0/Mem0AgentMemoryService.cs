using System.Net.Http.Json;
using System.Text.Json;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Utils;
using AgentMesh.Infrastructure.Mem0.Models;
using AgentMesh.Models.ChatMessages;

namespace AgentMesh.Infrastructure.Mem0
{
    public class Mem0AgentMemoryService : IAgentMemoryService
    {
        private readonly HttpClient _httpClient;
        private readonly AgentMemoryServiceConfiguration _configuration;
        private readonly Resilience _resilience;
        private readonly JsonSerializerOptions _jsonOptions;

        public Mem0AgentMemoryService(
            HttpClient httpClient,
            AgentMemoryServiceConfiguration configuration,
            Resilience resilience)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _resilience = resilience;
            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task AddConversationHistory(
            string userId,
            IEnumerable<ContextMessage> conversationHistory,
            CancellationToken cancellationToken = default)
        {
            var messages = conversationHistory
                .Where(m => !string.IsNullOrWhiteSpace(m.Text))
                .Select(m => new Message
                {
                    Role = m.Role == ContextMessageRole.User ? "user" : "assistant",
                    Content = m.Text.Trim()
                })
                .ToList();

            if (messages.Count == 0)
            {
                return;
            }

            var request = new MemoryCreateRequest
            {
                Messages = messages,
                UserId = userId
            };

            var response = await _resilience.SendWithRetryAsync(
                async () => await _httpClient.PostAsJsonAsync(
                    "/memories",
                    request,
                    _jsonOptions,
                    cancellationToken),
                "AddConversationHistory",
                null);

            response.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<AgentMemoryQueryResultItem>> Query(
            string userId,
            string query,
            CancellationToken cancellationToken = default)
        {
            var searchRequest = new SearchRequest
            {
                Query = query,
                UserId = userId
            };

            var response = await _resilience.SendWithRetryAsync(
                async () => await _httpClient.PostAsJsonAsync(
                    "/search",
                    searchRequest,
                    _jsonOptions,
                    cancellationToken),
                "Query",
                null);

            response.EnsureSuccessStatusCode();

            var searchResponse = await response.Content.ReadFromJsonAsync<SearchResponse>(
                _jsonOptions,
                cancellationToken);

            if (searchResponse?.Results == null)
            {
                return [];
            }

            return searchResponse.Results.Select(r => new AgentMemoryQueryResultItem
            {
                Memory = r.Memory,
                Confidence = r.Score
            });
        }
    }
}
