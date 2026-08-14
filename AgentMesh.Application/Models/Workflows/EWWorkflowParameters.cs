using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Models.Workflows
{
    public sealed class RequestDateTimeParameter: EWParameter<DateTime>
    {
        public const string ParamName = "Current datetime";
        public RequestDateTimeParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            ParameterValue = DateTime.UtcNow;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class UserLastRequestParameter : EWParameter<string>
    {
        public const string ParamName = "User last request";
        public UserLastRequestParameter()
        {
            Name = ParamName;
            IsUserCurrentRequestParameter = true;
        }
    }

    public sealed class InitialContextMessagesParameter : EWParameter<IEnumerable<ContextMessage>>
    {
        public const string ParamName = "Initial context messages";
        public InitialContextMessagesParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            IsConversationHistoryParameter = true;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class MessagesToSummarizeParameter : EWParameter<IEnumerable<ContextMessage>>
    {
        public const string ParamName = "Messages to summarize";
        public MessagesToSummarizeParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class RelevantMessagesToSaveInAgentMemoryParameter : EWParameter<IEnumerable<ContextMessage>>
    {
        public const string ParamName = "Relevant messages to save in agent memory";
        public RelevantMessagesToSaveInAgentMemoryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class SummarizeLanguageParameter : EWParameter<string>
    {
        public const string ParamName = "Summarize in language";
        public SummarizeLanguageParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class UserIntentParameter : EWParameter<string>
    {
        public const string ParamName = "User intent";
        public UserIntentParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class IntentCategoryParameter : EWParameter<UserIntentCategory?>
    {
        public const string ParamName = "Intent category";
        public IntentCategoryParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class LanguageOfTheUserParameter : EWParameter<string>
    {
        public const string ParamName = "Language of the user";
        public LanguageOfTheUserParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class LanguageOfTheDocumentationParameter : EWParameter<string>
    {
        public const string ParamName = "Language the documentation is written in";
        public LanguageOfTheDocumentationParameter(CodeModeWorkflowConfiguration codeModeWorkflowConfiguration)
        {
            Name = ParamName;
            ParameterValue = codeModeWorkflowConfiguration.LanguageOfKnowledgeBase;
        }
    }

    public sealed class ConversationTopicParameter : EWParameter<string>
    {
        public const string ParamName = "Conversation topic";
        public ConversationTopicParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class UserPreferencesParameter : EWParameter<IEnumerable<string>>
    {
        public const string ParamName = "User preferences";
        public UserPreferencesParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class UserProvidedDataParameter : EWParameter<IEnumerable<string>>
    {
        public const string ParamName = "User provided data";
        public UserProvidedDataParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class UserRequestedActionsParameter : EWParameter<IEnumerable<string>>
    {
        public const string ParamName = "User requested actions";
        public UserRequestedActionsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class MissingValuesParameter : EWParameter<IEnumerable<string>>
    {
        public const string ParamName = "Missing values";
        public MissingValuesParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class KnowledgeBaseAPIDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public const string ParamName = "API knowledge base documents";
        public KnowledgeBaseAPIDocumentsContentParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class PastMemoriesQueryParameter : EWParameter<IEnumerable<AgentMemoryItem>>
    {
        public const string ParamName = "Past memories query";
        public PastMemoriesQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class DomainsKnowledgeBaseQueryParameter : EWParameter<IEnumerable<KnowledgeBaseQueryInputItem>>
    {
        public const string ParamName = "Domain knowledge base queries";
        public DomainsKnowledgeBaseQueryParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class PastMemoriesQueryResultsParameter : EWParameter<IEnumerable<AgentMemoryQueryResultItem>>
    {
        public const string ParamName = "Past memories query results";
        public PastMemoriesQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class KnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public const string ParamName = "Knowledge base query results";
        public KnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class DomainsKnowledgeBaseDocumentsContentParameter : EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>
    {
        public const string ParamName = "Domain knowledge base documents";
        public DomainsKnowledgeBaseDocumentsContentParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class BusinessRequirementsParameter : EWParameter<string>
    {
        public const string ParamName = "Business requirements";
        public BusinessRequirementsParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class RequestRejectedReasonParameter : EWParameter<string>
    {
        public const string ParamName = "Request rejected reason";
        public RequestRejectedReasonParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class TechnicalSpecificationParameter : EWParameter<string>
    {
        public const string ParamName = "Technical specification";
        public TechnicalSpecificationParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class APISKnowledgeBaseQueryResultsParameter : EWParameter<IEnumerable<KnowledgeBaseQueryResultItem>>
    {
        public const string ParamName = "API knowledge base query results";
        public APISKnowledgeBaseQueryResultsParameter([FromKeyedServices("DisplayParametersSerializer")] IEWParameterSerializer displayValueSerializer)
        {
            Name = ParamName;
            DisplayValueSerializer = displayValueSerializer;
        }
    }

    public sealed class GeneratedCodeParameter : EWParameter<string>
    {
        public const string ParamName = "Generated code";
        public GeneratedCodeParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class SandboxExecutionIdParameter : EWParameter<string>
    {
        public const string ParamName = "Code execution id";
        public SandboxExecutionIdParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class CodeExecutionResultTypeParameter : EWParameter<SandboxResultType>
    {
        public const string ParamName = "Code execution result type";
        public CodeExecutionResultTypeParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class RequestRejectedFlagParameter : EWParameter<bool>
    {
        public const string ParamName = "Request rejected flag";
        public RequestRejectedFlagParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class ExecutionErrorParameter : EWParameter<bool>
    {
        public const string ParamName = "Code execution error occurred flag";
        public ExecutionErrorParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class PipelineResultDataParameter : EWParameter<string>
    {
        public const string ParamName = "Pipeline result data";
        public PipelineResultDataParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class PersonalAssistantOpeningSentenceParameter : EWParameter<string>
    {
        public const string ParamName = "Personal assistant opening sentence";
        public PersonalAssistantOpeningSentenceParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class PersonalAssistantClosingSentenceParameter : EWParameter<string>
    {
        public const string ParamName = "Personal assistant closing sentence";
        public PersonalAssistantClosingSentenceParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class PersonalAssistantConvenienceErrorSentenceParameter : EWParameter<string>
    {
        public const string ParamName = "Personal assistant convenience error sentence";
        public PersonalAssistantConvenienceErrorSentenceParameter()
        {
            Name = ParamName;
        }
    }

    public sealed class FinalAnswerParameter : EWParameter<string>
    {
        public const string ParamName = "Final answer";
        public FinalAnswerParameter()
        {
            Name = ParamName;
            IsResponseForUserParameter = true;
        }
    }

    public sealed class QMDQueryTypesDocumentationParameter : EWParameter<string>
    {
        public const string ParamName = "QMD query types documentation";
        private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

        public QMDQueryTypesDocumentationParameter()
        {
            Name = ParamName;
            ParameterValue = LoadDocumentationQueriesGenerationReference();
        }

        private static string? LoadDocumentationQueriesGenerationReference()
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
