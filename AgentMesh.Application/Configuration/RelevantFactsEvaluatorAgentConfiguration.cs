namespace AgentMesh.Application.Configuration
{
    public class RelevantFactsEvaluatorAgentConfiguration
    {
        public const string SectionName = "Agents:RelevantFactsEvaluator";
        public const string AgentName = "RelevantFactsEvaluator";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
