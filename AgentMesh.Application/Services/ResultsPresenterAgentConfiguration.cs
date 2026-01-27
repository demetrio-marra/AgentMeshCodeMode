namespace AgentMesh.Application.Services
{
    public class ResultsPresenterAgentConfiguration
    {
        public const string SectionName = "Agents:ResultsPresenter";
        public const string AgentName = "ResultsPresenter";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
