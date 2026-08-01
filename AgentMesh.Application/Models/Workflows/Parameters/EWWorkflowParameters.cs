using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Utils;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;

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
        public InitialContextMessagesParameter()
        {
            Name = EWParameterNames.InitialContextMessages;
            SerializeForVisualization = EWParameterDisplayUtils.GetContextMessagesDisplayValue;
            IsConversationHistoryParameter = true;
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
            SerializeForVisualization = EWParameterDisplayUtils.GetIntentCategoryDisplayValue;
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
            SerializeForVisualization = EWParameterDisplayUtils.GetStringEnumerableDisplayValue;
        }
    }

    public sealed class UserProvidedDataParameter : EWParameter<IEnumerable<string>>
    {
        public UserProvidedDataParameter()
        {
            Name = EWParameterNames.UserProvidedData;
            SerializeForVisualization = EWParameterDisplayUtils.GetStringEnumerableDisplayValue;
        }
    }

    public sealed class UserRequestedActionsParameter : EWParameter<IEnumerable<string>>
    {
        public UserRequestedActionsParameter()
        {
            Name = EWParameterNames.UserRequestedActions;
            SerializeForVisualization = EWParameterDisplayUtils.GetStringEnumerableDisplayValue;
        }
    }

    public sealed class MissingValuesParameter : EWParameter<IEnumerable<string>>
    {
        public MissingValuesParameter()
        {
            Name = EWParameterNames.MissingValues;
            SerializeForVisualization = EWParameterDisplayUtils.GetStringEnumerableDisplayValue;
        }
    }

    public sealed class KnowledgeBaseAPIDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public KnowledgeBaseAPIDocumentsContentParameter()
        {
            Name = EWParameterNames.KnowledgeBaseAPIDocumentsContent;
            SerializeForVisualization = EWParameterDisplayUtils.GetKnowledgeBaseDocumentsContentDisplayValue;
        }
    }

    public sealed class PastMemoriesQueryParameter : EWParameter<IEnumerable<AgentMemoryItem>>
    {
        public PastMemoriesQueryParameter()
        {
            Name = EWParameterNames.PastMemoriesQuery;
            SerializeForVisualization = EWParameterDisplayUtils.GetAgentMemoryItemsDisplayValue;
        }
    }

    public sealed class DomainsKnowledgeBaseQueryParameter : EWParameter<IEnumerable<KnowledgeBaseQueryInputItem>>
    {
        public DomainsKnowledgeBaseQueryParameter()
        {
            Name = EWParameterNames.DomainsKnowledgeBaseQuery;
            SerializeForVisualization = EWParameterDisplayUtils.GetKnowledgeBaseQueryInputItemsDisplayValue;
        }
    }

    public sealed class PastMemoriesQueryResultsParameter : EWParameter<IEnumerable<AgentMemoryQueryResultItem>>
    {
        public PastMemoriesQueryResultsParameter()
        {
            Name = EWParameterNames.PastMemoriesQueryResults;
            SerializeForVisualization = EWParameterDisplayUtils.GetAgentMemoryQueryResultsDisplayValue;
        }
    }

    public sealed class KnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public KnowledgeBaseQueryResultsParameter()
        {
            Name = EWParameterNames.KnowledgeBaseQueryResults;
            SerializeForVisualization = EWParameterDisplayUtils.GetKnowledgeBaseQueryResultsDisplayValue;
        }
    }

    public sealed class DomainsKnowledgeBaseDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public DomainsKnowledgeBaseDocumentsContentParameter()
        {
            Name = EWParameterNames.DomainsKnowledgeBaseDocumentsContent;
            SerializeForVisualization = EWParameterDisplayUtils.GetKnowledgeBaseDocumentsContentDisplayValue;
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
            SerializeForVisualization = EWParameterDisplayUtils.GetBooleanDisplayValue;
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

    public sealed class TechnicalAnalystRejectedParameter : EWParameter<bool?>
    {
        public TechnicalAnalystRejectedParameter()
        {
            Name = EWParameterNames.TechnicalAnalystRejected;
            SerializeForVisualization = EWParameterDisplayUtils.GetBooleanDisplayValue;
        }
    }

    public sealed class TechnicalAnalystRejectReasonsParameter : EWParameter<string>
    {
        public TechnicalAnalystRejectReasonsParameter()
        {
            Name = EWParameterNames.TechnicalAnalystRejectReasons;
        }
    }

    public sealed class ShouldEngageCoderParameter : EWParameter<bool?>
    {
        public ShouldEngageCoderParameter()
        {
            Name = EWParameterNames.ShouldEngageCoder;
            SerializeForVisualization = EWParameterDisplayUtils.GetBooleanDisplayValue;
        }
    }

    public sealed class APISKnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public APISKnowledgeBaseQueryResultsParameter()
        {
            Name = EWParameterNames.APISKnowledgeBaseQueryResults;
            SerializeForVisualization = EWParameterDisplayUtils.GetKnowledgeBaseQueryResultsDisplayValue;
        }
    }

    public sealed class SelectedAPIsFileLocationsParameter : EWParameter<IEnumerable<string>>
    {
        public SelectedAPIsFileLocationsParameter()
        {
            Name = EWParameterNames.SelectedAPIsFileLocations;
            SerializeForVisualization = EWParameterDisplayUtils.GetStringEnumerableDisplayValue;
        }
    }

    public sealed class DocumentationContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
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

    public sealed class LastCodeWithLineNumbersParameter : EWParameter<string>
    {
        public LastCodeWithLineNumbersParameter()
        {
            Name = EWParameterNames.LastCodeWithLineNumbers;
        }
    }

    public sealed class CodeExecutionFailuresDetectorIterationCountParameter : EWParameter<int?>
    {
        public CodeExecutionFailuresDetectorIterationCountParameter()
        {
            Name = EWParameterNames.CodeExecutionFailuresDetectorIterationCount;
            SerializeForVisualization = EWParameterDisplayUtils.GetInt32DisplayValue;
        }
    }

    public sealed class CodeExecutionAnalysisParameter : EWParameter<string>
    {
        public CodeExecutionAnalysisParameter()
        {
            Name = EWParameterNames.CodeExecutionAnalysis;
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

    public sealed class CodeExecutionResultTypeParameter : EWParameter<SandboxResultType?>
    {
        public CodeExecutionResultTypeParameter()
        {
            Name = EWParameterNames.CodeExecutionResultType;
            SerializeForVisualization = EWParameterDisplayUtils.GetSandboxResultTypeDisplayValue;
        }
    }

    public sealed class ExecutionErrorParameter : EWParameter<bool?>
    {
        public ExecutionErrorParameter()
        {
            Name = EWParameterNames.ExecutionError;
            SerializeForVisualization = EWParameterDisplayUtils.GetBooleanDisplayValue;
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
