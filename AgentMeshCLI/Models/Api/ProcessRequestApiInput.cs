using System.ComponentModel.DataAnnotations;

namespace AgentMesh.Models.Api
{
    public sealed class ProcessRequestApiInput
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
