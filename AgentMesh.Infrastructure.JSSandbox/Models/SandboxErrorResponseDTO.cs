using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.JSSandbox.Models
{
    internal class SandboxErrorResponseDTO
    {
        [JsonPropertyName("errorType")]
        public string? ErrorType { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
