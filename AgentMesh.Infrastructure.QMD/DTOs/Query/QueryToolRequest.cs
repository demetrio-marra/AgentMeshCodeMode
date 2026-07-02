using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.QMD.DTOs.Query
{
    /// <summary>
    /// Arguments for the MCP <c>query</c> tool — searches the knowledge base with one or more typed sub-queries.
    /// </summary>
    public class QueryToolRequest
    {
        /// <summary>
        /// Typed sub-queries to execute (lex/vec/hyde). First gets 2× weight. Required, 1–10 items.
        /// </summary>
        [JsonPropertyName("searches")]
        public List<QuerySubQuery> Searches { get; set; } = [];

        /// <summary>
        /// Max results (server default: 10).
        /// </summary>
        [JsonPropertyName("limit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Limit { get; set; }

        /// <summary>
        /// Min relevance 0-1 (server default: 0).
        /// </summary>
        [JsonPropertyName("minScore")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? MinScore { get; set; }

        /// <summary>
        /// Maximum candidates to rerank (server default: 40).
        /// </summary>
        [JsonPropertyName("candidateLimit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? CandidateLimit { get; set; }

        /// <summary>
        /// Filter to collections (OR match).
        /// </summary>
        [JsonPropertyName("collections")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Collections { get; set; }

        /// <summary>
        /// Background context to disambiguate the query. Does not search on its own.
        /// </summary>
        [JsonPropertyName("intent")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Intent { get; set; }

        /// <summary>
        /// Rerank results using LLM (server default: true).
        /// </summary>
        [JsonPropertyName("rerank")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Rerank { get; set; }
    }

    /// <summary>
    /// A single typed sub-query inside a <see cref="QueryToolRequest"/>.
    /// </summary>
    public class QuerySubQuery
    {
        /// <summary>
        /// Sub-query type: <c>lex</c>, <c>vec</c> or <c>hyde</c>.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The query text. Format depends on <see cref="Type"/>.
        /// </summary>
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;
    }
}
