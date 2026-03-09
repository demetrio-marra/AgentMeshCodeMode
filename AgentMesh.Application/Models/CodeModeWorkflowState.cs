using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState
    {
        public CodeModeWorkflowState(string userQuestion, IEnumerable<ContextMessage> contextMessages)
        {
            OriginalUserRequest = userQuestion;
            TokenUsageEntries = new List<AgentTokenUsageEntry>();
            CodeIssues = new List<string>();
            InitialContextMessages = contextMessages.ToList();
        }

        public string OriginalUserRequest { get; }
        public string? ExtractedIntentQuery { get; set; }
        public string EnrichedUserRequest { get; set; } = string.Empty;
        public string? RouterRecipient { get; set; }
        public string? BusinessRequirements { get; set; }
        public IEnumerable<string> MentionedApis { get; set; } = Enumerable.Empty<string>();
        public bool ShouldEngageCoder { get; set; }
        public string? OutputForUserFromBusinessAnalyst { get; set; }
        public string? BusinessAdvisorContent { get; set; }
        public string? GeneratedCode { get; set; }
        public string? LastCodeWithLineNumbers { get => SourceCodeUtils.GetSourceCodeWithLineNumbers(GeneratedCode); }
        public List<string> CodeIssues { get; set; }
        public bool IsCodeValid { get; set; }
        public int CodeFixerIterationCount { get; set; }
        public int CodeExecutionFailuresDetectorIterationCount { get; set; }
        public string? SandboxResult { get; set; }
        public SandboxResultType CodeExecutionResultType { get; set; }
        public string? PresenterOutput { get; set; }
        public string? FinalAnswer { get; set; }
        public List<AgentTokenUsageEntry> TokenUsageEntries { get; set; }
        public IEnumerable<AgentMemoryItem> ExtractedAgentMemories { get; set; } = Enumerable.Empty<AgentMemoryItem>();
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();

        public IEnumerable<ContextMessage> InitialContextMessages { get; set; } = Enumerable.Empty<ContextMessage>();

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
