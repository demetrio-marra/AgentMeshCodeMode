using System;

namespace AgentMesh.Application.Configuration
{
    public class APIQueriesGeneratorAgentConfiguration
    {
        public const string SectionName = "Agents:APIQueriesGenerator";
        public const string AgentName = "APIQueriesGenerator";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
