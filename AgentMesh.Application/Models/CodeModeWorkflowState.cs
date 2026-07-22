using AgentMesh.Application.Utils;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Application.Models
{
    public class CodeModeWorkflowState(string userQuestion, IEnumerable<ContextMessage> contextMessages) : BaseWorkflowState()
    {
        public string UserLastRequest { get; } = userQuestion;

        public IEnumerable<ContextMessage> InitialContextMessages { get; set; } = [.. contextMessages];

        public StructuredUserRequest UserRequest { get; set; } = new();
        public StructuredUserRequest? CanonicalizedUserRequest { get; set; }

        public string Intent { get => CanonicalizedUserRequest?.Intent ?? UserRequest.Intent; }
        public UserIntentCategory IntentCategory { get => CanonicalizedUserRequest?.IntentCategory ?? UserRequest.IntentCategory; }
        public string LanguageOfTheUser { get => UserRequest.LanguageOfTheUser; }
        public string ConversationTopic { get => (CanonicalizedUserRequest?.ConversationTopic ?? UserRequest.ConversationTopic) ?? "(no topic)"; }
        public IEnumerable<string> UserPreferences { get => CanonicalizedUserRequest?.UserPreferences ?? UserRequest.UserPreferences; }
        public IEnumerable<string> UserProvidedData { get => CanonicalizedUserRequest?.UserProvidedData ?? UserRequest.UserProvidedData; }
        public IEnumerable<string> UserRequestedActions { get => CanonicalizedUserRequest?.UserRequestedActions ?? UserRequest.UserRequestedActions; }

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


        /// <summary>
        /// APIs knowledge base query results, which are used to retrieve relevant information from the APIs knowledge base to assist in understanding the user's request and providing context for further processing.
        /// </summary>
        public KnowledgeBaseQueryResult APISKnowledgeBaseQueryResults { get; set; } = new KnowledgeBaseQueryResult();


        public IEnumerable<string> SelectedAPIsFileLocations { get; set; } = [];

        public string? DocumentationContent { get; set; }

        public string? GeneratedCode { get; set; }

        public string? LastCodeWithLineNumbers { get => SourceCodeUtils.GetSourceCodeWithLineNumbers(GeneratedCode); }

        public int CodeExecutionFailuresDetectorIterationCount { get; set; }

        public string? CodeExecutionAnalysis { get; set; }

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
