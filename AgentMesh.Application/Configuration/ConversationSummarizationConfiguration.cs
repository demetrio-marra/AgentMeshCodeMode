namespace AgentMesh.Application.Configuration
{
    public class ConversationSummarizationConfiguration
    {
        public const string SectionName = "ConversationSummarization";

        public int SummaryTokenThreshold { get; set; } = 3000;
        public int NumMessageToPreseve { get; set; } = 5;
        public string SummarizeLanguage { get; set; } = "English";
    }
}
