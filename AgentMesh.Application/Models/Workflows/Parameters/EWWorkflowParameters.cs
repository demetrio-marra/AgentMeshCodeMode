using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Models.Workflows.Parameters
{
    public sealed class UserLastRequestParameter : EWParameter<string>
    {
        public UserLastRequestParameter()
        {
            Name = EWParameterNames.UserLastRequest;
            IsUserCurrentRequestParameter = true;
        }
    }

    public sealed class InitialContextMessagesParameter : EWParameter<IEnumerable<ContextMessage>>
    {
        public InitialContextMessagesParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.InitialContextMessages;
            IsConversationHistoryParameter = true;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class UserIntentParameter : EWParameter<string>
    {
        public UserIntentParameter()
        {
            Name = EWParameterNames.UserIntent;
        }
    }

    public sealed class IntentCategoryParameter : EWParameter<UserIntentCategory?>
    {
        public IntentCategoryParameter()
        {
            Name = EWParameterNames.IntentCategory;
        }
    }

    public sealed class LanguageOfTheUserParameter : EWParameter<string>
    {
        public LanguageOfTheUserParameter()
        {
            Name = EWParameterNames.LanguageOfTheUser;
        }
    }

    public sealed class ConversationTopicParameter : EWParameter<string>
    {
        public ConversationTopicParameter()
        {
            Name = EWParameterNames.ConversationTopic;
        }
    }

    public sealed class UserPreferencesParameter : EWParameter<IEnumerable<string>>
    {
        public UserPreferencesParameter()
        {
            Name = EWParameterNames.UserPreferences;
        }
    }

    public sealed class UserProvidedDataParameter : EWParameter<IEnumerable<string>>
    {
        public UserProvidedDataParameter()
        {
            Name = EWParameterNames.UserProvidedData;
        }
    }

    public sealed class UserRequestedActionsParameter : EWParameter<IEnumerable<string>>
    {
        public UserRequestedActionsParameter()
        {
            Name = EWParameterNames.UserRequestedActions;
        }
    }

    public sealed class MissingValuesParameter : EWParameter<IEnumerable<string>>
    {
        public MissingValuesParameter()
        {
            Name = EWParameterNames.MissingValues;
        }
    }

    public sealed class KnowledgeBaseAPIDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public KnowledgeBaseAPIDocumentsContentParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.KnowledgeBaseAPIDocumentsContent;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class PastMemoriesQueryParameter : EWParameter<IEnumerable<AgentMemoryItem>>
    {
        public PastMemoriesQueryParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.PastMemoriesQuery;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class DomainsKnowledgeBaseQueryParameter : EWParameter<IEnumerable<KnowledgeBaseQueryInputItem>>
    {
        public DomainsKnowledgeBaseQueryParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.DomainsKnowledgeBaseQuery;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class PastMemoriesQueryResultsParameter : EWParameter<IEnumerable<AgentMemoryQueryResultItem>>
    {
        public PastMemoriesQueryResultsParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.PastMemoriesQueryResults;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class KnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public KnowledgeBaseQueryResultsParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.KnowledgeBaseQueryResults;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class DomainsKnowledgeBaseDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public DomainsKnowledgeBaseDocumentsContentParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.DomainsKnowledgeBaseDocumentsContent;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class BusinessRequirementsParameter : EWParameter<string>
    {
        public BusinessRequirementsParameter()
        {
            Name = EWParameterNames.BusinessRequirements;
        }
    }

    public sealed class FunctionalAnalystRejectedParameter : EWParameter<bool?>
    {
        public FunctionalAnalystRejectedParameter()
        {
            Name = EWParameterNames.FunctionalAnalystRejected;
        }
    }

    public sealed class FunctionalAnalystRejectReasonsParameter : EWParameter<string>
    {
        public FunctionalAnalystRejectReasonsParameter()
        {
            Name = EWParameterNames.FunctionalAnalystRejectReasons;
        }
    }

    public sealed class TechnicalSpecificationParameter : EWParameter<string>
    {
        public TechnicalSpecificationParameter()
        {
            Name = EWParameterNames.TechnicalSpecification;
        }
    }

    public sealed class TechnicalAnalystRejectedParameter : EWParameter<bool>
    {
        public TechnicalAnalystRejectedParameter()
        {
            Name = EWParameterNames.TechnicalAnalystRejected;
        }
    }

    public sealed class TechnicalAnalystRejectReasonsParameter : EWParameter<string>
    {
        public TechnicalAnalystRejectReasonsParameter()
        {
            Name = EWParameterNames.TechnicalAnalystRejectReasons;
        }
    }

    public sealed class ShouldEngageCoderParameter : EWParameter<bool>
    {
        public ShouldEngageCoderParameter()
        {
            Name = EWParameterNames.ShouldEngageCoder;
        }
    }

    public sealed class APISKnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public APISKnowledgeBaseQueryResultsParameter(IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.APISKnowledgeBaseQueryResults;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class SelectedAPIsFileLocationsParameter : EWParameter<IEnumerable<string>>
    {
        public SelectedAPIsFileLocationsParameter()
        {
            Name = EWParameterNames.SelectedAPIsFileLocations;
        }
    }

    public sealed class DocumentationContentParameter : EWParameter<string>
    {
        public DocumentationContentParameter()
        {
            Name = EWParameterNames.DocumentationContent;
        }
    }

    public sealed class GeneratedCodeParameter : EWParameter<string>
    {
        public GeneratedCodeParameter()
        {
            Name = EWParameterNames.GeneratedCode;
        }
    }

    public sealed class SandboxResultParameter : EWParameter<string>
    {
        public SandboxResultParameter()
        {
            Name = EWParameterNames.SandboxResult;
        }
    }

    public sealed class SandboxExecutionIdParameter : EWParameter<string>
    {
        public SandboxExecutionIdParameter()
        {
            Name = EWParameterNames.SandboxExecutionId;
        }
    }

    public sealed class CodeExecutionResultTypeParameter : EWParameter<SandboxResultType>
    {
        public CodeExecutionResultTypeParameter()
        {
            Name = EWParameterNames.CodeExecutionResultType;
        }
    }

    public sealed class ExecutionErrorParameter : EWParameter<bool>
    {
        public ExecutionErrorParameter()
        {
            Name = EWParameterNames.ExecutionError;
        }
    }

    public sealed class DomainExpertOutputParameter : EWParameter<string>
    {
        public DomainExpertOutputParameter()
        {
            Name = EWParameterNames.DomainExpertOutput;
        }
    }

    public sealed class PersonalAssistantOpeningSentenceParameter : EWParameter<string>
    {
        public PersonalAssistantOpeningSentenceParameter()
        {
            Name = EWParameterNames.PersonalAssistantOpeningSentence;
        }
    }

    public sealed class PersonalAssistantClosingSentenceParameter : EWParameter<string>
    {
        public PersonalAssistantClosingSentenceParameter()
        {
            Name = EWParameterNames.PersonalAssistantClosingSentence;
        }
    }

    public sealed class PersonalAssistantConvenienceErrorSentenceParameter : EWParameter<string>
    {
        public PersonalAssistantConvenienceErrorSentenceParameter()
        {
            Name = EWParameterNames.PersonalAssistantConvenienceErrorSentence;
        }
    }

    public sealed class FinalAnswerParameter : EWParameter<string>
    {
        public FinalAnswerParameter()
        {
            Name = EWParameterNames.FinalAnswer;
            IsResponseForUserParameter = true;
        }
    }
}
