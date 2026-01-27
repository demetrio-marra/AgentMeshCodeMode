namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState
    {
        public CodeModeWorkflowState(string userQuestion)
        {
            UserQuestion = userQuestion;
            TokenUsage = new Dictionary<string, int>();
            InputTokenUsage = new Dictionary<string, int>();
            OutputTokenUsage = new Dictionary<string, int>();
            CodeIssues = new List<string>();
        }

        public string UserQuestion { get; }
        public string? UserQuestionRelevantContext { get; set; }
        public string UserQuestionWithContext => string.IsNullOrWhiteSpace(UserQuestionRelevantContext) ? TranslatorResponse : $"{TranslatorResponse}\n\nRelevant context:\n{UserQuestionRelevantContext}";
        public string? TranslatorResponse { get; set; }
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
        public Dictionary<string, int> TokenUsage { get; set; }
        public Dictionary<string, int> InputTokenUsage { get; set; }
        public Dictionary<string, int> OutputTokenUsage { get; set; }

        public void AddTokenUsage(string agentName, int tokenCount, int inputTokenCount, int outputTokenCount)
        {
            if (TokenUsage.ContainsKey(agentName))
            {
                TokenUsage[agentName] += tokenCount;
                InputTokenUsage[agentName] += inputTokenCount;
                OutputTokenUsage[agentName] += outputTokenCount;
            }
            else
            {
                TokenUsage[agentName] = tokenCount;
                InputTokenUsage[agentName] = inputTokenCount;
                OutputTokenUsage[agentName] = outputTokenCount;
            }
        }
    }
}
