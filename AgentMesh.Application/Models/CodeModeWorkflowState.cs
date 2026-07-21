using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState(string userQuestion, IEnumerable<ContextMessage> contextMessages) : BaseWorkflowState()
    {
        public string UserLastRequest { get; } = userQuestion;

        public IEnumerable<ContextMessage> InitialContextMessages { get; set; } = [.. contextMessages];

        public StructuredUserRequest NewStructuredUserRequest { get; set; } = new();
        public StructuredUserRequest? NewCanonicalizedStructuredUserRequest { get; set; }

        public string Intent { get => NewCanonicalizedStructuredUserRequest?.Intent ?? NewStructuredUserRequest.Intent; }
        public UserIntentCategory IntentCategory { get => NewCanonicalizedStructuredUserRequest?.IntentCategory ?? NewStructuredUserRequest.IntentCategory; }
        public string LanguageOfTheUser { get => NewStructuredUserRequest.LanguageOfTheUser; }
        public string ConversationTopic { get => (NewCanonicalizedStructuredUserRequest?.ConversationTopic ?? NewStructuredUserRequest.ConversationTopic) ?? "(no topic)"; }
        public IEnumerable<string> UserPreferences { get => NewCanonicalizedStructuredUserRequest?.UserPreferences ?? NewStructuredUserRequest.UserPreferences; }
        public IEnumerable<string> UserProvidedData { get => NewCanonicalizedStructuredUserRequest?.UserProvidedData ?? NewStructuredUserRequest.UserProvidedData; }
        public IEnumerable<string> UserRequestedActions { get => NewCanonicalizedStructuredUserRequest?.UserRequestedActions ?? NewStructuredUserRequest.UserRequestedActions; }

        /// <summary>
        /// First knowledge base call, which is a fast query to retrieve relevant information from the knowledge base to assist in understanding the user's request and providing context for further processing.
        /// It is based on NewStructuredUserRequest.EntitiesByDomain
        /// </summary>
        public KnowledgeBaseQueryResult FastDomainsKnowledgeBaseQueryResults { get; set; } = new KnowledgeBaseQueryResult();


        public IEnumerable<KnowledgeBaseDocumentContent> KnowledgeBaseAPIDocumentsContent { get; set; } = [];


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
    }
}
