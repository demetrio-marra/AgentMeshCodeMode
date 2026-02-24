using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.JSSandbox.Models
{
    internal class CodeExecutionRequestDTO
    {
        [JsonPropertyName("codeToRun")]
        public string CodeToRun { get; set; } = string.Empty;

        [JsonPropertyName("userAgentId")]
        public string UserAgentId { get; set; } = string.Empty;
    }
}
