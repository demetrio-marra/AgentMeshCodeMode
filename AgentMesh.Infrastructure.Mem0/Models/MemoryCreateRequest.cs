using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.Mem0.Models
{
    public class MemoryCreateRequest
    {
        /// <summary>
        /// Collection of user/assistant messages to be processed by Mem0.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = [];

        /// <summary>
        /// The user identifier associated with these messages.
        /// </summary>
        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        /// <summary>
        /// The agent identifier associated with these messages.
        /// </summary>
        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        /// <summary>
        /// The run identifier for tracking the source of these messages.
        /// </summary>
        [JsonPropertyName("run_id")]
        public string? RunId { get; set; }

        /// <summary>
        /// Optional metadata associated with these messages.
        /// </summary>
        [JsonPropertyName("metadata")]
        public object? Metadata { get; set; }
    }
}
