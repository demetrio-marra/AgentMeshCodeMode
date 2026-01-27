namespace AgentMesh.Application.Models
{
    public class LLMConfiguration
    {
        public string Model { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal CostPerMillionInputTokens { get; set; }
        public decimal CostPerMillionOutputTokens { get; set; }
    }

    public class LLMsConfiguration : Dictionary<string, LLMConfiguration>
    {
        public const string SectionName = "LLMs";
    }
}
