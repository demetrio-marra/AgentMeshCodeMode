using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.JSSandbox.Models
{
    internal class CodeSandboxExecutionDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("isError")]
        public bool IsError { get; set; }

        [JsonPropertyName("executionResult")]
        public string ExecutionResult { get; set; } = string.Empty;
    }
}
