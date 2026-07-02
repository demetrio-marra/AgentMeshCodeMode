using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using static AgentMesh.Models.ContextAnalyzer.ContextAnalyzerAgentOutput;
using static AgentMesh.Models.IntentExtractor.IntentExtractorAgentOutput;

namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState(string userQuestion, IEnumerable<ContextMessage> contextMessages)
    {
        public string OriginalUserRequest { get; } = userQuestion;
        public string? LanguageOfTheUser { get; set; }
        public string? UserIntent { get; set; }
        public IEnumerable<string> MissingPastMemories { get; set; } = [];
        public IEnumerable<IntentExtractorKnowledgeBase> MissingKnowledgeBaseSearchEntries { get; set; } = [];
        public string EnrichedUserRequest { get; set; } = string.Empty;
        public UserIntentCategoryValues UserIntentCategoryValue { get; set; }
        public string? BusinessRequirements { get; set; }
        public bool ShouldEngageCoder { get; set; }
        public string? BusinessAdvisorContent { get; set; }
        public string? DocumentationContent { get; set; }
        public string? GeneratedCode { get; set; }
        public string? LastCodeWithLineNumbers { get => SourceCodeUtils.GetSourceCodeWithLineNumbers(GeneratedCode); }
        public List<string> CodeIssues { get; set; } = [];
        public bool IsCodeValid { get; set; }
        public int CodeFixerIterationCount { get; set; }
        public int CodeExecutionFailuresDetectorIterationCount { get; set; }
        public string? SandboxResult { get; set; }
        public string? SandboxExecutionId { get; set; }
        public SandboxResultType CodeExecutionResultType { get; set; }
        public string? PresenterOutput { get; set; }
        public string? FinalAnswer { get; set; }

        public List<WorkflowStepUsageEntry> TokenUsageEntries { get; set; } = [];
        public IEnumerable<AgentMemoryQueryResultItem> ExtractedAgentMemories { get; set; } = [];
        public IEnumerable<string> RelevantKnowledgeBaseFileNames { get; set; } = [];
        public IEnumerable<ContextMessage> InitialContextMessages { get; set; } = [.. contextMessages];

        public KnowledgeBaseQueryResult KnowledgeBaseQueryResults { get; set; } = new KnowledgeBaseQueryResult();

        public IEnumerable<KnowledgeBaseDocumentContent> KnowledgeBaseDocumentsContent { get; set; } = [];


        public void AddTokenUsage(string agentName, int inputTokenCount, int outputTokenCount, TimeSpan? elapsed = null, string? stepName = null)
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
