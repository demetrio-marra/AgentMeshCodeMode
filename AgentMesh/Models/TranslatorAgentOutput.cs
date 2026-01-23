namespace AgentMesh.Models
{
    public class TranslatorAgentOutput : IAgentOutput
    {
        public string TranslatedSentence { get; set; } = string.Empty;
        public string DetectedOriginalLanguage { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
