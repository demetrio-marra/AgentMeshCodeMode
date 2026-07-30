using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Parameters;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Services;
using AgentMesh.Utils;

namespace AgentMesh.Application.Services
{
    public class CodeModeWorkflowParametersFactory : IEasyWorkflowParametersFactory
    {
        public const string UserLastRequestParameterName = "User last request";
        public const string InitialContextMessagesParameterName = "Initial context messages";
        public const string UserIntentParameterName = "User intent";
        public const string IntentCategoryParameterName = "Intent category";
        public const string LanguageOfTheUserParameterName = "Language of the user";
        public const string ConversationTopicParameterName = "Conversation topic";
        public const string UserPreferencesParameterName = "User preferences";
        public const string UserProvidedDataParameterName = "User provided data";
        public const string UserRequestedActionsParameterName = "User requested actions";
        public const string KnowledgeBaseAPIDocumentsContentParameterName = "API knowledge base documents";
        public const string PastMemoriesQueryParameterName = "Past memories query";
        public const string DomainsKnowledgeBaseQueryParameterName = "Domain knowledge base queries";
        public const string PastMemoriesQueryResultsParameterName = "Past memories query results";
        public const string KnowledgeBaseQueryResultsParameterName = "Knowledge base query results";
        public const string DomainsKnowledgeBaseDocumentsContentParameterName = "Domain knowledge base documents";
        public const string BusinessRequirementsParameterName = "Business requirements";
        public const string FunctionalAnalystRejectedParameterName = "Functional analyst rejected";
        public const string FunctionalAnalystRejectReasonsParameterName = "Functional analyst reject reasons";
        public const string TechnicalSpecificationParameterName = "Technical specification";
        public const string TechnicalAnalystRejectedParameterName = "Technical analyst rejected";
        public const string TechnicalAnalystRejectReasonsParameterName = "Technical analyst reject reasons";
        public const string ShouldEngageCoderParameterName = "Should engage coder";
        public const string APISKnowledgeBaseQueryResultsParameterName = "API knowledge base query results";
        public const string SelectedAPIsFileLocationsParameterName = "Selected API file locations";
        public const string DocumentationContentParameterName = "Documentation content";
        public const string GeneratedCodeParameterName = "Generated code";
        public const string LastCodeWithLineNumbersParameterName = "Generated code with line numbers";
        public const string CodeExecutionFailuresDetectorIterationCountParameterName = "Code execution failures detector iteration count";
        public const string CodeExecutionAnalysisParameterName = "Code execution analysis";
        public const string SandboxResultParameterName = "Code execution result";
        public const string SandboxExecutionIdParameterName = "Code execution id";
        public const string CodeExecutionResultTypeParameterName = "Code execution result type";
        public const string ExecutionErrorParameterName = "Execution error";
        public const string DomainExpertOutputParameterName = "Domain expert output";
        public const string PersonalAssistantOpeningSentenceParameterName = "Personal assistant opening sentence";
        public const string PersonalAssistantClosingSentenceParameterName = "Personal assistant closing sentence";
        public const string PersonalAssistantConvenienceErrorSentenceParameterName = "Personal assistant convenience error sentence";
        public const string FinalAnswerParameterName = "Final answer";


        private static Parameter InitUserLastRequestParameter()
        {
            var userLastRequestParameter = new Parameter
            {
                Name = UserLastRequestParameterName,
                IsUserCurrentRequestParameter = true
            };
            return userLastRequestParameter;
        }

        private static Parameter InitInitialContextMessagesParameter()
        {
            var initialContextMessagesParameter = new Parameter
            {
                Name = InitialContextMessagesParameterName,
                GetDisplayValue = GetContextMessagesDisplayValue,
                IsConversationHistoryParameter = true
            };
            return initialContextMessagesParameter;
        }

        private static Parameter InitUserIntentParameter()
        {
            var userIntentParameter = new Parameter
            {
                Name = UserIntentParameterName                
            };
            return userIntentParameter;
        }

        private static Parameter InitIntentCategoryParameter()
        {
            var intentCategoryParameter = new Parameter
            {
                Name = IntentCategoryParameterName,
                GetDisplayValue = GetIntentCategoryDisplayValue
            };
            return intentCategoryParameter;
        }

        private static Parameter InitLanguageOfTheUserParameter()
        {
            var languageOfTheUserParameter = new Parameter
            {
                Name = LanguageOfTheUserParameterName
            };
            return languageOfTheUserParameter;
        }

        private static Parameter InitConversationTopicParameter()
        {
            var conversationTopicParameter = new Parameter
            {
                Name = ConversationTopicParameterName
            };
            return conversationTopicParameter;
        }

        private static Parameter InitUserPreferencesParameter()
        {
            var userPreferencesParameter = new Parameter
            {
                Name = UserPreferencesParameterName,
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return userPreferencesParameter;
        }

        private static Parameter InitUserProvidedDataParameter()
        {
            var userProvidedDataParameter = new Parameter
            {
                Name = UserProvidedDataParameterName,
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return userProvidedDataParameter;
        }

        private static Parameter InitUserRequestedActionsParameter()
        {
            var userRequestedActionsParameter = new Parameter
            {
                Name = UserRequestedActionsParameterName,
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return userRequestedActionsParameter;
        }

        private static Parameter InitKnowledgeBaseAPIDocumentsContentParameter()
        {
            var knowledgeBaseAPIDocumentsContentParameter = new Parameter
            {
                Name = KnowledgeBaseAPIDocumentsContentParameterName,
                GetDisplayValue = GetKnowledgeBaseDocumentsContentDisplayValue
            };
            return knowledgeBaseAPIDocumentsContentParameter;
        }

        private static Parameter InitPastMemoriesQueryParameter()
        {
            var pastMemoriesQueryParameter = new Parameter
            {
                Name = PastMemoriesQueryParameterName,
                GetDisplayValue = GetAgentMemoryItemsDisplayValue
            };
            return pastMemoriesQueryParameter;
        }

        private static Parameter InitDomainsKnowledgeBaseQueryParameter()
        {
            var domainsKnowledgeBaseQueryParameter = new Parameter
            {
                Name = DomainsKnowledgeBaseQueryParameterName,
                GetDisplayValue = GetKnowledgeBaseQueryInputItemsDisplayValue
            };
            return domainsKnowledgeBaseQueryParameter;
        }

        private static Parameter InitPastMemoriesQueryResultsParameter()
        {
            var pastMemoriesQueryResultsParameter = new Parameter
            {
                Name = PastMemoriesQueryResultsParameterName,
                GetDisplayValue = GetAgentMemoryQueryResultsDisplayValue
            };
            return pastMemoriesQueryResultsParameter;
        }

        private static Parameter InitKnowledgeBaseQueryResultsParameter()
        {
            var knowledgeBaseQueryResultsParameter = new Parameter
            {
                Name = KnowledgeBaseQueryResultsParameterName,
                GetDisplayValue = GetKnowledgeBaseQueryResultsDisplayValue
            };
            return knowledgeBaseQueryResultsParameter;
        }

        private static Parameter InitDomainsKnowledgeBaseDocumentsContentParameter()
        {
            var domainsKnowledgeBaseDocumentsContentParameter = new Parameter
            {
                Name = DomainsKnowledgeBaseDocumentsContentParameterName,
                GetDisplayValue = GetKnowledgeBaseDocumentsContentDisplayValue
            };
            return domainsKnowledgeBaseDocumentsContentParameter;
        }

        private static Parameter InitBusinessRequirementsParameter()
        {
            var businessRequirementsParameter = new Parameter
            {
                Name = BusinessRequirementsParameterName
            };
            return businessRequirementsParameter;
        }

        private static Parameter InitFunctionalAnalystRejectedParameter()
        {
            var functionalAnalystRejectedParameter = new Parameter
            {
                Name = FunctionalAnalystRejectedParameterName,
                GetDisplayValue = GetBooleanDisplayValue
            };
            return functionalAnalystRejectedParameter;
        }

        private static Parameter InitFunctionalAnalystRejectReasonsParameter()
        {
            var functionalAnalystRejectReasonsParameter = new Parameter
            {
                Name = FunctionalAnalystRejectReasonsParameterName
            };
            return functionalAnalystRejectReasonsParameter;
        }

        private static Parameter InitTechnicalSpecificationParameter()
        {
            var technicalSpecificationParameter = new Parameter
            {
                Name = TechnicalSpecificationParameterName
            };
            return technicalSpecificationParameter;
        }

        private static Parameter InitTechnicalAnalystRejectedParameter()
        {
            var technicalAnalystRejectedParameter = new Parameter
            {
                Name = TechnicalAnalystRejectedParameterName,
                GetDisplayValue = GetBooleanDisplayValue
            };
            return technicalAnalystRejectedParameter;
        }

        private static Parameter InitTechnicalAnalystRejectReasonsParameter()
        {
            var technicalAnalystRejectReasonsParameter = new Parameter
            {
                Name = TechnicalAnalystRejectReasonsParameterName
            };
            return technicalAnalystRejectReasonsParameter;
        }

        private static Parameter InitShouldEngageCoderParameter()
        {
            var shouldEngageCoderParameter = new Parameter
            {
                Name = ShouldEngageCoderParameterName,
                GetDisplayValue = GetBooleanDisplayValue
            };
            return shouldEngageCoderParameter;
        }

        private static Parameter InitAPISKnowledgeBaseQueryResultsParameter()
        {
            var apisKnowledgeBaseQueryResultsParameter = new Parameter
            {
                Name = APISKnowledgeBaseQueryResultsParameterName,
                GetDisplayValue = GetKnowledgeBaseQueryResultsDisplayValue
            };
            return apisKnowledgeBaseQueryResultsParameter;
        }

        private static Parameter InitSelectedAPIsFileLocationsParameter()
        {
            var selectedAPIsFileLocationsParameter = new Parameter
            {
                Name = SelectedAPIsFileLocationsParameterName,
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return selectedAPIsFileLocationsParameter;
        }

        private static Parameter InitDocumentationContentParameter()
        {
            var documentationContentParameter = new Parameter
            {
                Name = DocumentationContentParameterName
            };
            return documentationContentParameter;
        }

        private static Parameter InitGeneratedCodeParameter()
        {
            var generatedCodeParameter = new Parameter
            {
                Name = GeneratedCodeParameterName
            };
            return generatedCodeParameter;
        }

        private static Parameter InitLastCodeWithLineNumbersParameter()
        {
            var lastCodeWithLineNumbersParameter = new Parameter
            {
                Name = LastCodeWithLineNumbersParameterName
            };
            return lastCodeWithLineNumbersParameter;
        }

        private static Parameter InitCodeExecutionFailuresDetectorIterationCountParameter()
        {
            var codeExecutionFailuresDetectorIterationCountParameter = new Parameter
            {
                Name = CodeExecutionFailuresDetectorIterationCountParameterName,
                GetDisplayValue = GetInt32DisplayValue
            };
            return codeExecutionFailuresDetectorIterationCountParameter;
        }

        private static Parameter InitCodeExecutionAnalysisParameter()
        {
            var codeExecutionAnalysisParameter = new Parameter
            {
                Name = CodeExecutionAnalysisParameterName
            };
            return codeExecutionAnalysisParameter;
        }

        private static Parameter InitSandboxResultParameter()
        {
            var sandboxResultParameter = new Parameter
            {
                Name = SandboxResultParameterName
            };
            return sandboxResultParameter;
        }

        private static Parameter InitSandboxExecutionIdParameter()
        {
            var sandboxExecutionIdParameter = new Parameter
            {
                Name = SandboxExecutionIdParameterName
            };
            return sandboxExecutionIdParameter;
        }

        private static Parameter InitCodeExecutionResultTypeParameter()
        {
            var codeExecutionResultTypeParameter = new Parameter
            {
                Name = CodeExecutionResultTypeParameterName,
                GetDisplayValue = GetSandboxResultTypeDisplayValue
            };
            return codeExecutionResultTypeParameter;
        }

        private static Parameter InitExecutionErrorParameter()
        {
            var executionErrorParameter = new Parameter
            {
                Name = ExecutionErrorParameterName,
                GetDisplayValue = GetBooleanDisplayValue
            };
            return executionErrorParameter;
        }

        private static Parameter InitDomainExpertOutputParameter()
        {
            var domainExpertOutputParameter = new Parameter
            {
                Name = DomainExpertOutputParameterName
            };
            return domainExpertOutputParameter;
        }

        private static Parameter InitPersonalAssistantOpeningSentenceParameter()
        {
            var personalAssistantOpeningSentenceParameter = new Parameter
            {
                Name = PersonalAssistantOpeningSentenceParameterName
            };
            return personalAssistantOpeningSentenceParameter;
        }

        private static Parameter InitPersonalAssistantClosingSentenceParameter()
        {
            var personalAssistantClosingSentenceParameter = new Parameter
            {
                Name = PersonalAssistantClosingSentenceParameterName
            };
            return personalAssistantClosingSentenceParameter;
        }

        private static Parameter InitPersonalAssistantConvenienceErrorSentenceParameter()
        {
            var personalAssistantConvenienceErrorSentenceParameter = new Parameter
            {
                Name = PersonalAssistantConvenienceErrorSentenceParameterName
            };
            return personalAssistantConvenienceErrorSentenceParameter;
        }

        private static Parameter InitFinalAnswerParameter()
        {
            var finalAnswerParameter = new Parameter
            {
                Name = FinalAnswerParameterName,
                IsResponseForUserParameter = true
            };
            return finalAnswerParameter;
        }

        private static IEnumerable<Parameter> InitParameters()
        {
            var parameters = new List<Parameter>
            {
                InitUserLastRequestParameter(),
                InitInitialContextMessagesParameter(),
                InitUserIntentParameter(),
                InitIntentCategoryParameter(),
                InitLanguageOfTheUserParameter(),
                InitConversationTopicParameter(),
                InitUserPreferencesParameter(),
                InitUserProvidedDataParameter(),
                InitUserRequestedActionsParameter(),
                InitKnowledgeBaseAPIDocumentsContentParameter(),
                InitPastMemoriesQueryParameter(),
                InitDomainsKnowledgeBaseQueryParameter(),
                InitPastMemoriesQueryResultsParameter(),
                InitKnowledgeBaseQueryResultsParameter(),
                InitDomainsKnowledgeBaseDocumentsContentParameter(),
                InitBusinessRequirementsParameter(),
                InitFunctionalAnalystRejectedParameter(),
                InitFunctionalAnalystRejectReasonsParameter(),
                InitTechnicalSpecificationParameter(),
                InitTechnicalAnalystRejectedParameter(),
                InitTechnicalAnalystRejectReasonsParameter(),
                InitShouldEngageCoderParameter(),
                InitAPISKnowledgeBaseQueryResultsParameter(),
                InitSelectedAPIsFileLocationsParameter(),
                InitDocumentationContentParameter(),
                InitGeneratedCodeParameter(),
                InitLastCodeWithLineNumbersParameter(),
                InitCodeExecutionFailuresDetectorIterationCountParameter(),
                InitCodeExecutionAnalysisParameter(),
                InitSandboxResultParameter(),
                InitSandboxExecutionIdParameter(),
                InitCodeExecutionResultTypeParameter(),
                InitExecutionErrorParameter(),
                InitDomainExpertOutputParameter(),
                InitPersonalAssistantOpeningSentenceParameter(),
                InitPersonalAssistantClosingSentenceParameter(),
                InitPersonalAssistantConvenienceErrorSentenceParameter(),
                InitFinalAnswerParameter()
            };

            return parameters;
        }

        private static string GetBooleanDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var value = Parameter.AsObject<bool>(rawValue);
            return value.ToString();
        }

        private static string GetInt32DisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var value = Parameter.AsObject<int>(rawValue);
            return value.ToString();
        }

        private static string GetIntentCategoryDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var value = Parameter.AsObject<UserIntentCategory>(rawValue);
            return value.ToString();
        }

        private static string GetSandboxResultTypeDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var value = Parameter.AsObject<SandboxResultType>(rawValue);
            return value.ToString();
        }

        private static string GetStringEnumerableDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var values = Parameter.AsObject<IEnumerable<string>>(rawValue);
            if (values == null || !values.Any())
            {
                return Parameter.NoDisplayValue;
            }

            return ListsFormatter.ToBulletList(values);
        }

        private static string GetContextMessagesDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var messages = Parameter.AsObject<IEnumerable<ContextMessage>>(rawValue)?.ToList();
            if (messages == null || messages.Count == 0)
            {
                return Parameter.NoDisplayValue;
            }

            return $"Messages count: {messages.Count}";
        }

        private static string GetAgentMemoryItemsDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var items = Parameter.AsObject<IEnumerable<AgentMemoryItem>>(rawValue);
            if (items == null || !items.Any())
            {
                return Parameter.NoDisplayValue;
            }

            return ListsFormatter.ToBulletList(items.Select(item => item.Memory));
        }

        private static string GetAgentMemoryQueryResultsDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var items = Parameter.AsObject<IEnumerable<AgentMemoryQueryResultItem>>(rawValue);
            if (items == null || !items.Any())
            {
                return Parameter.NoDisplayValue;
            }

            return ListsFormatter.ToBulletList(items.Select(item => $"{item.Memory} Confidence: {item.Confidence}"));
        }

        private static string GetKnowledgeBaseQueryInputItemsDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var items = Parameter.AsObject<IEnumerable<KnowledgeBaseQueryInputItem>>(rawValue);
            if (items == null || !items.Any())
            {
                return Parameter.NoDisplayValue;
            }

            return ListsFormatter.ToBulletList(items.Select(item => item.ToString()));
        }

        private static string GetKnowledgeBaseQueryResultsDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var knowledgeBaseQueryResult = Parameter.AsObject<KnowledgeBaseQueryResult>(rawValue);
            var results = knowledgeBaseQueryResult?.Results ?? Parameter.AsObject<IEnumerable<KnowledgeBaseQueryResultItem>>(rawValue);
            if (results == null || !results.Any())
            {
                return Parameter.NoDisplayValue;
            }

            return ListsFormatter.ToBulletList(results.Select(item => $"{item.File} - Title: {item.Title} - Relevance: {item.Relevance}"));
        }

        private static string GetKnowledgeBaseDocumentsContentDisplayValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return Parameter.NoDisplayValue;
            }

            var documents = Parameter.AsObject<IEnumerable<KnowledgeBaseDocumentContent>>(rawValue);
            if (documents == null || !documents.Any())
            {
                return Parameter.NoDisplayValue;
            }

            var files = documents
                .Select(document => document.File)
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Cast<string>()
                .ToList();

            if (!files.Any())
            {
                return Parameter.NoDisplayValue;
            }

            return ListsFormatter.ToBulletList(files);
        }

        public IEnumerable<Parameter> CreateParameters()
        {
            return InitParameters();
        }
    }
}
