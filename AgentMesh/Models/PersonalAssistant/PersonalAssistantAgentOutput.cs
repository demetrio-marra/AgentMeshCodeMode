namespace AgentMesh.Models.PersonalAssistant
{
    public class PersonalAssistantAgentOutput : IAgentOutput
    {
        public bool IsDataAnActualError { get; set; }
        public string? OpeningSentence { get; set; }
        public string? ClosingSentence { get; set; }
        public string? ConvenienceErrorSentence { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
