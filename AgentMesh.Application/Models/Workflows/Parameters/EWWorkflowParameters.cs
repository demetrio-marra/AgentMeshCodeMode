using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Workflows.Parameters
{
    public sealed class RequestDateTimeParameter: EWParameter<DateTime>
    {
        public RequestDateTimeParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.RequestDateTime;
            ParameterValue = DateTime.UtcNow;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

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
        public InitialContextMessagesParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
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

    public sealed class LanguageOfTheDocumentationParameter : EWParameter<string>
    {
        public LanguageOfTheDocumentationParameter(CodeModeWorkflowConfiguration codeModeWorkflowConfiguration)
        {
            Name = EWParameterNames.LanguageOfTheDocumentation;
            ParameterValue = codeModeWorkflowConfiguration.LanguageOfKnowledgeBase;
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
        public KnowledgeBaseAPIDocumentsContentParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.KnowledgeBaseAPIDocumentsContent;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class PastMemoriesQueryParameter : EWParameter<IEnumerable<AgentMemoryItem>>
    {
        public PastMemoriesQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.PastMemoriesQuery;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class DomainsKnowledgeBaseQueryParameter : EWParameter<IEnumerable<KnowledgeBaseQueryInputItem>>
    {
        public DomainsKnowledgeBaseQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.DomainsKnowledgeBaseQuery;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class PastMemoriesQueryResultsParameter : EWParameter<IEnumerable<AgentMemoryQueryResultItem>>
    {
        public PastMemoriesQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.PastMemoriesQueryResults;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class KnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public KnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.KnowledgeBaseQueryResults;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class DomainsKnowledgeBaseDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public DomainsKnowledgeBaseDocumentsContentParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
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

    public sealed class RequestRejectedReasonParameter : EWParameter<string>
    {
        public RequestRejectedReasonParameter()
        {
            Name = EWParameterNames.RequestRejectedReason;
        }
    }

    public sealed class TechnicalSpecificationParameter : EWParameter<string>
    {
        public TechnicalSpecificationParameter()
        {
            Name = EWParameterNames.TechnicalSpecification;
        }
    }

    public sealed class APISKnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public APISKnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = EWParameterNames.APISKnowledgeBaseQueryResults;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class GeneratedCodeParameter : EWParameter<string>
    {
        public GeneratedCodeParameter()
        {
            Name = EWParameterNames.GeneratedCode;
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

    public sealed class RequestRejectedFlagParameter : EWParameter<bool>
    {
        public RequestRejectedFlagParameter()
        {
            Name = EWParameterNames.RequestRejectedFlag;
        }
    }

    public sealed class ExecutionErrorParameter : EWParameter<bool>
    {
        public ExecutionErrorParameter()
        {
            Name = EWParameterNames.ExecutionError;
        }
    }

    public sealed class PipelineResultDataParameter : EWParameter<string>
    {
        public PipelineResultDataParameter()
        {
            Name = EWParameterNames.PipelineResultData;
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

    public sealed class QMDQueryTypesDocumentationParameter : EWParameter<string>
    {
        private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

        public QMDQueryTypesDocumentationParameter()
        {
            Name = EWParameterNames.QMDQueryTypesDocumentation;
            ParameterValue = LoadDocumentationQueriesGenerationReference();
        }

        private string? LoadDocumentationQueriesGenerationReference()
        {
            var candidatePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Prompts", QmdQueryTypesFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Prompts", QmdQueryTypesFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "AgentMeshCLI", "Prompts", QmdQueryTypesFileName)
            };

            foreach (var candidatePath in candidatePaths)
            {
                if (File.Exists(candidatePath))
                    return File.ReadAllText(candidatePath);
            }

            return null;
        }
    }
}
