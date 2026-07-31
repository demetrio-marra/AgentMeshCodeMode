using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class EWParametersFactory : IEWParametersFactory
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
        public const string MissingValuesParameterName = "Missing values";
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


        private static EWParameter InitUserLastRequestParameter()
        {
            var userLastRequestParameter = new EWParameter
            {
                Name = UserLastRequestParameterName,
                IsUserCurrentRequestParameter = true
            };
            return userLastRequestParameter;
        }

        private static EWParameter InitInitialContextMessagesParameter()
        {
            var initialContextMessagesParameter = new EWParameter
            {
                Name = InitialContextMessagesParameterName,
                ParameterType = typeof(IEnumerable<ContextMessage>),
                SerializeForVisualization = GetContextMessagesDisplayValue,
                IsConversationHistoryParameter = true
            };
            return initialContextMessagesParameter;
        }

        private static EWParameter InitUserIntentParameter()
        {
            var userIntentParameter = new EWParameter
            {
                Name = UserIntentParameterName                
            };
            return userIntentParameter;
        }

        private static EWParameter InitIntentCategoryParameter()
        {
            var intentCategoryParameter = new EWParameter
            {
                Name = IntentCategoryParameterName,
                ParameterType = typeof(UserIntentCategory),
                SerializeForVisualization = GetIntentCategoryDisplayValue
            };
            return intentCategoryParameter;
        }

        private static EWParameter InitLanguageOfTheUserParameter()
        {
            var languageOfTheUserParameter = new EWParameter
            {
                Name = LanguageOfTheUserParameterName
            };
            return languageOfTheUserParameter;
        }

        private static EWParameter InitConversationTopicParameter()
        {
            var conversationTopicParameter = new EWParameter
            {
                Name = ConversationTopicParameterName
            };
            return conversationTopicParameter;
        }

        private static EWParameter InitUserPreferencesParameter()
        {
            var userPreferencesParameter = new EWParameter
            {
                Name = UserPreferencesParameterName,
                ParameterType = typeof(IEnumerable<string>),
                SerializeForVisualization = GetStringEnumerableDisplayValue
            };
            return userPreferencesParameter;
        }

        private static EWParameter InitUserProvidedDataParameter()
        {
            var userProvidedDataParameter = new EWParameter
            {
                Name = UserProvidedDataParameterName,
                ParameterType = typeof(IEnumerable<string>),
                SerializeForVisualization = GetStringEnumerableDisplayValue
            };
            return userProvidedDataParameter;
        }

        private static EWParameter InitUserRequestedActionsParameter()
        {
            var userRequestedActionsParameter = new EWParameter
            {
                Name = UserRequestedActionsParameterName,
                ParameterType = typeof(IEnumerable<string>),
                SerializeForVisualization = GetStringEnumerableDisplayValue
            };
            return userRequestedActionsParameter;
        }

        private static EWParameter InitMissingValuesParameter()
        {
            var missingValuesParameter = new EWParameter
            {
                Name = MissingValuesParameterName,
                ParameterType = typeof(IEnumerable<string>),
                SerializeForVisualization = GetStringEnumerableDisplayValue
            };
            return missingValuesParameter;
        }

        private static EWParameter InitKnowledgeBaseAPIDocumentsContentParameter()
        {
            var knowledgeBaseAPIDocumentsContentParameter = new EWParameter
            {
                Name = KnowledgeBaseAPIDocumentsContentParameterName,
                ParameterType = typeof(IEnumerable<KnowledgeBaseDocumentContent>),
                SerializeForVisualization = GetKnowledgeBaseDocumentsContentDisplayValue
            };
            return knowledgeBaseAPIDocumentsContentParameter;
        }

        private static EWParameter InitPastMemoriesQueryParameter()
        {
            var pastMemoriesQueryParameter = new EWParameter
            {
                Name = PastMemoriesQueryParameterName,
                ParameterType = typeof(IEnumerable<AgentMemoryItem>),
                SerializeForVisualization = GetAgentMemoryItemsDisplayValue
            };
            return pastMemoriesQueryParameter;
        }

        private static EWParameter InitDomainsKnowledgeBaseQueryParameter()
        {
            var domainsKnowledgeBaseQueryParameter = new EWParameter
            {
                Name = DomainsKnowledgeBaseQueryParameterName,
                ParameterType = typeof(IEnumerable<KnowledgeBaseQueryInputItem>),
                SerializeForVisualization = GetKnowledgeBaseQueryInputItemsDisplayValue
            };
            return domainsKnowledgeBaseQueryParameter;
        }

        private static EWParameter InitPastMemoriesQueryResultsParameter()
        {
            var pastMemoriesQueryResultsParameter = new EWParameter
            {
                Name = PastMemoriesQueryResultsParameterName,
                ParameterType = typeof(IEnumerable<AgentMemoryQueryResultItem>),
                SerializeForVisualization = GetAgentMemoryQueryResultsDisplayValue
            };
            return pastMemoriesQueryResultsParameter;
        }

        private static EWParameter InitKnowledgeBaseQueryResultsParameter()
        {
            var knowledgeBaseQueryResultsParameter = new EWParameter
            {
                Name = KnowledgeBaseQueryResultsParameterName,
                ParameterType = typeof(IEnumerable<KnowledgeBaseQueryResultItem>),
                SerializeForVisualization = GetKnowledgeBaseQueryResultsDisplayValue
            };
            return knowledgeBaseQueryResultsParameter;
        }

        private static EWParameter InitDomainsKnowledgeBaseDocumentsContentParameter()
        {
            var domainsKnowledgeBaseDocumentsContentParameter = new EWParameter
            {
                Name = DomainsKnowledgeBaseDocumentsContentParameterName,
                ParameterType = typeof(IEnumerable<KnowledgeBaseDocumentContent>),
                SerializeForVisualization = GetKnowledgeBaseDocumentsContentDisplayValue
            };
            return domainsKnowledgeBaseDocumentsContentParameter;
        }

        private static EWParameter InitBusinessRequirementsParameter()
        {
            var businessRequirementsParameter = new EWParameter
            {
                Name = BusinessRequirementsParameterName
            };
            return businessRequirementsParameter;
        }

        private static EWParameter InitFunctionalAnalystRejectedParameter()
        {
            var functionalAnalystRejectedParameter = new EWParameter
            {
                Name = FunctionalAnalystRejectedParameterName,
                ParameterType = typeof(bool),
                SerializeForVisualization = GetBooleanDisplayValue
            };
            return functionalAnalystRejectedParameter;
        }

        private static EWParameter InitFunctionalAnalystRejectReasonsParameter()
        {
            var functionalAnalystRejectReasonsParameter = new EWParameter
            {
                Name = FunctionalAnalystRejectReasonsParameterName
            };
            return functionalAnalystRejectReasonsParameter;
        }

        private static EWParameter InitTechnicalSpecificationParameter()
        {
            var technicalSpecificationParameter = new EWParameter
            {
                Name = TechnicalSpecificationParameterName
            };
            return technicalSpecificationParameter;
        }

        private static EWParameter InitTechnicalAnalystRejectedParameter()
        {
            var technicalAnalystRejectedParameter = new EWParameter
            {
                Name = TechnicalAnalystRejectedParameterName,
                ParameterType = typeof(bool),
                SerializeForVisualization = GetBooleanDisplayValue
            };
            return technicalAnalystRejectedParameter;
        }

        private static EWParameter InitTechnicalAnalystRejectReasonsParameter()
        {
            var technicalAnalystRejectReasonsParameter = new EWParameter
            {
                Name = TechnicalAnalystRejectReasonsParameterName
            };
            return technicalAnalystRejectReasonsParameter;
        }

        private static EWParameter InitShouldEngageCoderParameter()
        {
            var shouldEngageCoderParameter = new EWParameter
            {
                Name = ShouldEngageCoderParameterName,
                ParameterType = typeof(bool),
                SerializeForVisualization = GetBooleanDisplayValue
            };
            return shouldEngageCoderParameter;
        }

        private static EWParameter InitAPISKnowledgeBaseQueryResultsParameter()
        {
            var apisKnowledgeBaseQueryResultsParameter = new EWParameter
            {
                Name = APISKnowledgeBaseQueryResultsParameterName,
                ParameterType = typeof(IEnumerable<KnowledgeBaseQueryResultItem>),
                SerializeForVisualization = GetKnowledgeBaseQueryResultsDisplayValue
            };
            return apisKnowledgeBaseQueryResultsParameter;
        }

        private static EWParameter InitSelectedAPIsFileLocationsParameter()
        {
            var selectedAPIsFileLocationsParameter = new EWParameter
            {
                Name = SelectedAPIsFileLocationsParameterName,
                ParameterType = typeof(IEnumerable<string>),
                SerializeForVisualization = GetStringEnumerableDisplayValue
            };
            return selectedAPIsFileLocationsParameter;
        }

        private static EWParameter InitDocumentationContentParameter()
        {
            var documentationContentParameter = new EWParameter
            {
                Name = DocumentationContentParameterName,
                ParameterType = typeof(IEnumerable<KnowledgeBaseDocumentContent>)
            };
            return documentationContentParameter;
        }

        private static EWParameter InitGeneratedCodeParameter()
        {
            var generatedCodeParameter = new EWParameter
            {
                Name = GeneratedCodeParameterName
            };
            return generatedCodeParameter;
        }

        private static EWParameter InitLastCodeWithLineNumbersParameter()
        {
            var lastCodeWithLineNumbersParameter = new EWParameter
            {
                Name = LastCodeWithLineNumbersParameterName
            };
            return lastCodeWithLineNumbersParameter;
        }

        private static EWParameter InitCodeExecutionFailuresDetectorIterationCountParameter()
        {
            var codeExecutionFailuresDetectorIterationCountParameter = new EWParameter
            {
                Name = CodeExecutionFailuresDetectorIterationCountParameterName,
                ParameterType = typeof(int),
                SerializeForVisualization = GetInt32DisplayValue
            };
            return codeExecutionFailuresDetectorIterationCountParameter;
        }

        private static EWParameter InitCodeExecutionAnalysisParameter()
        {
            var codeExecutionAnalysisParameter = new EWParameter
            {
                Name = CodeExecutionAnalysisParameterName
            };
            return codeExecutionAnalysisParameter;
        }

        private static EWParameter InitSandboxResultParameter()
        {
            var sandboxResultParameter = new EWParameter
            {
                Name = SandboxResultParameterName
            };
            return sandboxResultParameter;
        }

        private static EWParameter InitSandboxExecutionIdParameter()
        {
            var sandboxExecutionIdParameter = new EWParameter
            {
                Name = SandboxExecutionIdParameterName
            };
            return sandboxExecutionIdParameter;
        }

        private static EWParameter InitCodeExecutionResultTypeParameter()
        {
            var codeExecutionResultTypeParameter = new EWParameter
            {
                Name = CodeExecutionResultTypeParameterName,
                ParameterType = typeof(SandboxResultType),
                SerializeForVisualization = GetSandboxResultTypeDisplayValue
            };
            return codeExecutionResultTypeParameter;
        }

        private static EWParameter InitExecutionErrorParameter()
        {
            var executionErrorParameter = new EWParameter
            {
                Name = ExecutionErrorParameterName,
                ParameterType = typeof(bool),
                SerializeForVisualization = GetBooleanDisplayValue
            };
            return executionErrorParameter;
        }

        private static EWParameter InitDomainExpertOutputParameter()
        {
            var domainExpertOutputParameter = new EWParameter
            {
                Name = DomainExpertOutputParameterName
            };
            return domainExpertOutputParameter;
        }

        private static EWParameter InitPersonalAssistantOpeningSentenceParameter()
        {
            var personalAssistantOpeningSentenceParameter = new EWParameter
            {
                Name = PersonalAssistantOpeningSentenceParameterName
            };
            return personalAssistantOpeningSentenceParameter;
        }

        private static EWParameter InitPersonalAssistantClosingSentenceParameter()
        {
            var personalAssistantClosingSentenceParameter = new EWParameter
            {
                Name = PersonalAssistantClosingSentenceParameterName
            };
            return personalAssistantClosingSentenceParameter;
        }

        private static EWParameter InitPersonalAssistantConvenienceErrorSentenceParameter()
        {
            var personalAssistantConvenienceErrorSentenceParameter = new EWParameter
            {
                Name = PersonalAssistantConvenienceErrorSentenceParameterName
            };
            return personalAssistantConvenienceErrorSentenceParameter;
        }

        private static EWParameter InitFinalAnswerParameter()
        {
            var finalAnswerParameter = new EWParameter
            {
                Name = FinalAnswerParameterName,
                IsResponseForUserParameter = true
            };
            return finalAnswerParameter;
        }

        private static IEnumerable<EWParameter> InitParameters()
        {
            var parameters = new List<EWParameter>
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
                InitMissingValuesParameter(),
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

        private static T? DeserializeRawValue<T>(object? rawValue)
        {
            if (rawValue == null)
            {
                return default;
            }

            if (rawValue is T typedValue)
            {
                return typedValue;
            }

            var targetType = typeof(T);
            var nonNullableTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (nonNullableTargetType.IsEnum)
                {
                    if (rawValue is string enumString && Enum.TryParse(nonNullableTargetType, enumString, true, out var enumValueFromString))
                    {
                        return (T)enumValueFromString;
                    }

                    var enumValueFromNumber = Enum.ToObject(nonNullableTargetType, rawValue);
                    return (T)enumValueFromNumber;
                }

                if (rawValue is IConvertible && typeof(IConvertible).IsAssignableFrom(nonNullableTargetType))
                {
                    return (T)Convert.ChangeType(rawValue, nonNullableTargetType);
                }
            }
            catch
            {
                return default;
            }

            return default;
        }

        private static string GetBooleanDisplayValue(object? rawValue)
        {
            if (rawValue == null)
            {
                return EWParameter.NoDataPlaceholder;
            }

            var value = DeserializeRawValue<bool>(rawValue);
            return value.ToString();
        }

        private static string GetInt32DisplayValue(object? rawValue)
        {
            if (rawValue == null)
            {
                return EWParameter.NoDataPlaceholder;
            }

            var value = DeserializeRawValue<int>(rawValue);
            return value.ToString();
        }

        private static string GetIntentCategoryDisplayValue(object? rawValue)
        {
            if (rawValue == null)
            {
                return EWParameter.NoDataPlaceholder;
            }

            var value = DeserializeRawValue<UserIntentCategory>(rawValue);
            return value.ToString();
        }

        private static string GetSandboxResultTypeDisplayValue(object? rawValue)
        {
            if (rawValue == null)
            {
                return EWParameter.NoDataPlaceholder;
            }

            var value = DeserializeRawValue<SandboxResultType>(rawValue);
            return value.ToString();
        }

        private static string GetStringEnumerableDisplayValue(object? rawValue)
        {
            var values = DeserializeRawValue<IEnumerable<string>>(rawValue);
            if (values == null || !values.Any())
            {
                return EWParameter.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(values);
        }

        private static string GetContextMessagesDisplayValue(object? rawValue)
        {
            if (rawValue == null)
            {
                return EWParameter.NoDataPlaceholder;
            }

            var messages = rawValue as IEnumerable<ContextMessage>;
            if (messages == null || messages.Count() == 0)
            {
                return EWParameter.NoDataPlaceholder;
            }

            return $"Messages count: {messages.Count()}";
        }

        private static string GetAgentMemoryItemsDisplayValue(object? rawValue)
        {
            var items = DeserializeRawValue<IEnumerable<AgentMemoryItem>>(rawValue);
            if (items == null || !items.Any())
            {
                return EWParameter.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => item.Memory));
        }

        private static string GetAgentMemoryQueryResultsDisplayValue(object? rawValue)
        {
            var items = DeserializeRawValue<IEnumerable<AgentMemoryQueryResultItem>>(rawValue);
            if (items == null || !items.Any())
            {
                return EWParameter.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => $"{item.Memory} Confidence: {item.Confidence}"));
        }

        private static string GetKnowledgeBaseQueryInputItemsDisplayValue(object? rawValue)
        {
            var items = DeserializeRawValue<IEnumerable<KnowledgeBaseQueryInputItem>>(rawValue);
            if (items == null || !items.Any())
            {
                return EWParameter.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => item.ToString()));
        }

        private static string GetKnowledgeBaseQueryResultsDisplayValue(object? rawValue)
        {
            var knowledgeBaseQueryResult = DeserializeRawValue<KnowledgeBaseQueryResult>(rawValue);
            var results = knowledgeBaseQueryResult?.Results ?? DeserializeRawValue<IEnumerable<KnowledgeBaseQueryResultItem>>(rawValue);
            if (results == null || !results.Any())
            {
                return EWParameter.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(results.Select(item => $"{item.File} - Title: {item.Title} - Relevance: {item.Relevance}"));
        }

        private static string GetKnowledgeBaseDocumentsContentDisplayValue(object? rawValue)
        {
            var documents = DeserializeRawValue<IEnumerable<KnowledgeBaseDocumentContent>>(rawValue);
            if (documents == null || !documents.Any())
            {
                return EWParameter.NoDataPlaceholder;
            }

            var files = documents
                .Select(document => document.File)
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Cast<string>()
                .ToList();

            if (!files.Any())
            {
                return EWParameter.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(files);
        }

        IEnumerable<EWParameter> IEWParametersFactory.CreateParameters()
        {
            return InitParameters();
        }
    }
}
