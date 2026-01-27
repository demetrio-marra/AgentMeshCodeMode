namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState
    {
        public CodeModeWorkflowState(string userQuestion)
        {
            UserQuestion = userQuestion;
            TokenUsageEntries = new List<AgentTokenUsageEntry>();
            CodeIssues = new List<string>();
        }

        public string UserQuestion { get; }
        public string? UserQuestionRelevantContext { get; set; }
        public string? TranslatorResponse { get; set; }
        public string? AggregatedUserQuestion { get; set; }
        public string? DetectedOriginalLanguage { get; set; }
        public string? RouterRecipient { get; set; }
        public string? BusinessRequirements { get; set; }
        public bool ShouldEngageCoder { get; set; }
        public string? OutputForUserFromBusinessAnalyst { get; set; }
        public string? GeneratedCode { get; set; }
        public string? LastCodeWithLineNumbers { get; set; }
        public List<string> CodeIssues { get; set; }
        public bool IsCodeValid { get; set; }
        public int CodeFixerIterationCount { get; set; }
        public string? SandboxResult { get; set; }
        public string? SandboxError { get; set; }
        public string? PresenterOutput { get; set; }
        public string? FinalAnswer { get; set; }
        public List<AgentTokenUsageEntry> TokenUsageEntries { get; set; }

        public void AddTokenUsage(string agentName, int tokenCount, int inputTokenCount, int outputTokenCount)
        {
            TokenUsageEntries.Add(new AgentTokenUsageEntry
            {
                AgentName = agentName,
                InputTokens = inputTokenCount,
                OutputTokens = outputTokenCount
            });
        }
    }
}
