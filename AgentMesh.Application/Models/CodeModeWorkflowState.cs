using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState(string userQuestion, IEnumerable<ContextMessage> contextMessages)
    {
        public string UserLastRequest { get; } = userQuestion;

        public IEnumerable<ContextMessage> InitialContextMessages { get; set; } = [.. contextMessages];


        public string OriginalUserRequest { get => ClassifiedUserRequest.OriginalUserRequest; set => ClassifiedUserRequest.OriginalUserRequest = value; }

        public AgentMesh.Models.RequestAnalysis.StructuredUserRequest? NewStructuredUserRequest { get; set; }
        public AgentMesh.Models.RequestAnalysis.StructuredUserRequest? NewCanonicalizedStructuredUserRequest { get; set; }


        /// <summary>
        /// Results of Intent detection and User Request classification, including the identified intent, user request type, and any relevant metadata.
        /// </summary>
        public AgentMesh.Models.IntentExtractor.StructuredUserRequest ClassifiedUserRequest { get; set; } = new();

        /// <summary>
        /// Canonicalized user intent enriched with domain-specific terminology.
        /// </summary>
        public string CanonicalizedIntent { get => ClassifiedUserRequest.CanonicalizedIntent; set => ClassifiedUserRequest.CanonicalizedIntent = value; }

        public UserIntentCategoryValues CanonicalizedIntentCategory { get; set; }

        /// <summary>
        /// First knowledge base call, which is a fast query to retrieve relevant information from the knowledge base to assist in understanding the user's request and providing context for further processing.
        /// It is based on ClassifiedUserRequest.EntitiesByDomain
        /// </summary>
        public KnowledgeBaseQueryResult FastDomainsKnowledgeBaseQueryResults { get; set; } = new KnowledgeBaseQueryResult();


        /// <summary>
        /// Past memories query, which is used to retrieve relevant information from the agent's memory to assist in understanding the user's request and providing context for further processing.
        /// </summary>
        public IEnumerable<string> PastMemoriesQuery { get; set; } = [];

        /// <summary>
        /// Domain knowledge base query, which is used to retrieve relevant information from the domain-specific knowledge base to assist in understanding the user's request and providing context for further processing.
        /// </summary>
        public IEnumerable<KnowledgeBaseQueryInputItem> DomainsKnowledgeBaseQuery { get; set; } = [];


        /// <summary>
        /// Results of the extracted agent memories, which are used to retrieve relevant information from the agent's memory to assist in understanding the user's request and providing context for further processing.
        /// </summary>
        public IEnumerable<AgentMemoryQueryResultItem> PastMemoriesQueryResults { get; set; } = [];

        /// <summary>
        /// Results of the domain knowledge base queries, which are used to retrieve relevant information from the domain-specific knowledge base to assist in understanding the user's request and providing context for further processing.
        /// </summary>
        public KnowledgeBaseQueryResult DomainsKnowledgeBaseQueryResults { get; set; } = new KnowledgeBaseQueryResult();

        /// <summary>
        /// Results of the domain knowledge base documents content, which are used to retrieve relevant information from the domain-specific knowledge base to assist in understanding the user's request and providing context for further processing.
        /// </summary>
        public IEnumerable<KnowledgeBaseDocumentContent> DomainsKnowledgeBaseDocumentsContent { get; set; } = [];


        public string? BusinessRequirements { get; set; }
        public bool FunctionalAnalystRejected { get; set; }
        public string? FunctionalAnalystRejectReasons { get; set; }
        public string? TechnicalSpecification { get; set; }
        public bool TechnicalAnalystRejected { get; set; }
        public string? TechnicalAnalystRejectReasons { get; set; }
        public bool ShouldEngageCoder { get; set; }

        public IEnumerable<string> FastAPISKnowledgeBaseQuery { get; set; } = [];

        public KnowledgeBaseQueryResult FastAPISKnowledgeBaseQueryResults { get; set; } = new KnowledgeBaseQueryResult();

        public IEnumerable<KnowledgeBaseQueryInputItem> CanonicalizedAPIQueries { get; set; } = [];

        /// <summary>
        /// APIs knowledge base query results, which are used to retrieve relevant information from the APIs knowledge base to assist in understanding the user's request and providing context for further processing.
        /// </summary>
        public KnowledgeBaseQueryResult APISKnowledgeBaseQueryResults { get; set; } = new KnowledgeBaseQueryResult();


        public IEnumerable<string> SelectedAPIsFileLocations { get; set; } = [];



        public string? DocumentationContent { get; set; }
        public string? GeneratedCode { get; set; }
        public string? LastCodeWithLineNumbers { get => SourceCodeUtils.GetSourceCodeWithLineNumbers(GeneratedCode); }
        public int CodeExecutionFailuresDetectorIterationCount { get; set; }
        public string? SandboxResult { get; set; }
        public string? SandboxExecutionId { get; set; }
        public SandboxResultType CodeExecutionResultType { get; set; }
        public bool ExecutionError { get => CodeExecutionResultType != SandboxResultType.Success; }
        public string? DomainExpertOutput { get; set; }

        public string? PersonalAssistantOpeningSentence { get; set; }
        public string? PersonalAssistantClosingSentence { get; set; }
        public string? PersonalAssistantConvenienceErrorSentence { get; set; }

        public string? FinalAnswer { get; set; }

        public List<WorkflowStepUsageEntry> TokenUsageEntries { get; set; } = [];



        public IEnumerable<KnowledgeBaseDocumentContent> KnowledgeBaseAPIDocumentsContent { get; set; } = [];

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
