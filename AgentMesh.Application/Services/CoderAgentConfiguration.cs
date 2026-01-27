namespace AgentMesh.Application.Services
{
    public class CoderAgentConfiguration
    {
        public const string SectionName = "Agents:Coder";
        public const string AgentName = "Coder";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
        public string ApiReference { get; set; } = string.Empty;
        public string? ApiReferenceFile { get; set; }
    }
}
