using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Reranker;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class RerankerAgent(
        [FromKeyedServices(RerankerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RerankerAgent> logger) : AgentBase<List<string>>(logger, RerankerAgentConfiguration.AgentName, openAIClient, resilience), IRerankerAgent
    {
        private readonly ILogger<RerankerAgent> _logger = logger;

        public async Task<RerankerAgentOutput> ExecuteAsync(
            RerankerAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var candidates = input.QueryResults.ToList();
            if (candidates.Count == 0)
            {
                return new RerankerAgentOutput
                {
                    QueryResults = []
                };
            }

            var rankedCandidates = candidates
                .Select((item, index) => new RankedCandidate
                {
                    RankId = $"R{index + 1}",
                    Item = item
                })
                .ToList();

            var candidatesPayload = rankedCandidates.Select(candidate => new CandidatePayload
            {
                RankId = candidate.RankId,
                Id = candidate.Item.Id,
                Title = candidate.Item.Title,
                Summary = candidate.Item.Summary,
                File = candidate.Item.File,
                Relevance = candidate.Item.Relevance
            });

            var userPayload = new UserPayload
            {
                StructuredUserRequest = input.StructuredUserRequest,
                Candidates = candidatesPayload
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            var rankedMap = rankedCandidates.ToDictionary(candidate => candidate.RankId, candidate => candidate.Item, StringComparer.OrdinalIgnoreCase);

            var selectedItems = result.Result
                .Where(rankId => rankedMap.ContainsKey(rankId))
                .Select(rankId => rankedMap[rankId])
                .Distinct()
                .ToList();

            return new RerankerAgentOutput
            {
                QueryResults = selectedItems,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override List<string> ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions);
                if (responseDTO?.SelectedRankIds != null)
                {
                    return [.. responseDTO.SelectedRankIds
                        .Where(rankId => !string.IsNullOrWhiteSpace(rankId))
                        .Select(rankId => rankId.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)];
                }

                var fallbackList = JsonSerializer.Deserialize<List<string>>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions);
                if (fallbackList != null)
                {
                    return [.. fallbackList
                        .Where(rankId => !string.IsNullOrWhiteSpace(rankId))
                        .Select(rankId => rankId.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)];
                }

                throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into reranker output.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the reranker response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private class UserPayload
        {
            [JsonPropertyName("structuredUserRequest")]
            public required AgentMesh.Models.RequestAnalysis.StructuredUserRequest StructuredUserRequest { get; set; }

            [JsonPropertyName("candidates")]
            public IEnumerable<CandidatePayload> Candidates { get; set; } = [];
        }

        private class CandidatePayload
        {
            [JsonPropertyName("rankId")]
            public string RankId { get; set; } = string.Empty;

            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("summary")]
            public string? Summary { get; set; }

            [JsonPropertyName("file")]
            public string File { get; set; } = string.Empty;

            [JsonPropertyName("relevance")]
            public double? Relevance { get; set; }
        }

        private class ParsedResponse
        {
            [JsonPropertyName("selectedRankIds")]
            public IEnumerable<string> SelectedRankIds { get; set; } = [];
        }

        private class RankedCandidate
        {
            public string RankId { get; set; } = string.Empty;
            public KnowledgeBaseQueryResultItem Item { get; set; } = new();
        }
    }
}
