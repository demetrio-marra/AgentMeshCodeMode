using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.Workflows;
using static AgentMesh.Models.ContextAnalyzer.ContextAnalyzerAgentOutput;
using static AgentMesh.Models.IntentExtractor.IntentExtractorAgentOutput;

namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState
    {
        public CodeModeWorkflowState(string userQuestion, IEnumerable<ContextMessage> contextMessages)
        {
            OriginalUserRequest = userQuestion;
            TokenUsageEntries = new List<WorkflowStepUsageEntry>();
            CodeIssues = new List<string>();
            InitialContextMessages = contextMessages.ToList();
        }

        public string OriginalUserRequest { get; }
        public string? LanguageOfTheUser { get; set; }
        public string? UserIntent { get; set; }
        public IEnumerable<string> MissingPastMemories { get; set; } = [];
        public IEnumerable<IntentExtractorKnowledgeBase> MissingKnowledgeBaseSearchEntries { get; set; } = [];
        public string EnrichedUserRequest { get; set; } = string.Empty;
        public UserIntentCategoryValues UserIntentCategoryValue { get; set; }
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
        public List<WorkflowStepUsageEntry> TokenUsageEntries { get; set; }
        public IEnumerable<AgentMemoryItem> ExtractedAgentMemories { get; set; } = Enumerable.Empty<AgentMemoryItem>();
        public IEnumerable<string> RelevantKnowledgeBaseFileNames { get; set; } = [];
        public IEnumerable<ContextMessage> InitialContextMessages { get; set; } = Enumerable.Empty<ContextMessage>();

        public IEnumerable<KnowledgeBaseQueryResult> KnowledgeBaseQueryResult { get; set; } = [];

        public IEnumerable<KnowledgeBaseDocumentContent> KnowledgeBaseDocumentsContent { get; set; } = [];

        public void AddTokenUsage(string agentName, int tokenCount, int inputTokenCount, int outputTokenCount, TimeSpan? elapsed = null, string? stepName = null)
        {
            TokenUsageEntries.Add(new WorkflowStepUsageEntry
            {
                StepName = stepName ?? agentName,
                Elapsed = elapsed ?? TimeSpan.Zero,
                IsAgentic = true,
                TokensUsage = new AgentTokenUsageEntry
                {
                    AgentName = agentName,
                    InputTokens = inputTokenCount,
                    OutputTokens = outputTokenCount
                }
            });
        }

        public void AddStepUsage(string stepName, TimeSpan elapsed, bool isAgentic, AgentTokenUsageEntry? tokensUsage = null)
        {
            TokenUsageEntries.Add(new WorkflowStepUsageEntry
            {
                StepName = stepName,
                Elapsed = elapsed,
                IsAgentic = isAgentic,
                TokensUsage = tokensUsage
            });
        }
    }
}
