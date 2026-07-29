using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Parameters;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Utils;

namespace AgentMesh.Application.Parameters
{
    public static class ParametersInit
    {
        private static Parameter InitUserLastRequestParameter()
        {
            var userLastRequestParameter = new Parameter
            {
                Name = "User last request"                
            };
            return userLastRequestParameter;
        }

        private static Parameter InitInitialContextMessagesParameter()
        {
            var initialContextMessagesParameter = new Parameter
            {
                Name = "Initial context messages",
                GetDisplayValue = GetContextMessagesDisplayValue
            };
            return initialContextMessagesParameter;
        }

        private static Parameter InitUserIntentParameter()
        {
            var userIntentParameter = new Parameter
            {
                Name = "User intent"                
            };
            return userIntentParameter;
        }

        private static Parameter InitIntentCategoryParameter()
        {
            var intentCategoryParameter = new Parameter
            {
                Name = "Intent category",
                GetDisplayValue = GetIntentCategoryDisplayValue
            };
            return intentCategoryParameter;
        }

        private static Parameter InitLanguageOfTheUserParameter()
        {
            var languageOfTheUserParameter = new Parameter
            {
                Name = "Language of the user"
            };
            return languageOfTheUserParameter;
        }

        private static Parameter InitConversationTopicParameter()
        {
            var conversationTopicParameter = new Parameter
            {
                Name = "Conversation topic"
            };
            return conversationTopicParameter;
        }

        private static Parameter InitUserPreferencesParameter()
        {
            var userPreferencesParameter = new Parameter
            {
                Name = "User preferences",
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return userPreferencesParameter;
        }

        private static Parameter InitUserProvidedDataParameter()
        {
            var userProvidedDataParameter = new Parameter
            {
                Name = "User provided data",
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return userProvidedDataParameter;
        }

        private static Parameter InitUserRequestedActionsParameter()
        {
            var userRequestedActionsParameter = new Parameter
            {
                Name = "User requested actions",
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return userRequestedActionsParameter;
        }

        private static Parameter InitKnowledgeBaseAPIDocumentsContentParameter()
        {
            var knowledgeBaseAPIDocumentsContentParameter = new Parameter
            {
                Name = "API knowledge base documents",
                GetDisplayValue = GetKnowledgeBaseDocumentsContentDisplayValue
            };
            return knowledgeBaseAPIDocumentsContentParameter;
        }

        private static Parameter InitPastMemoriesQueryParameter()
        {
            var pastMemoriesQueryParameter = new Parameter
            {
                Name = "Past memories query",
                GetDisplayValue = GetAgentMemoryItemsDisplayValue
            };
            return pastMemoriesQueryParameter;
        }

        private static Parameter InitDomainsKnowledgeBaseQueryParameter()
        {
            var domainsKnowledgeBaseQueryParameter = new Parameter
            {
                Name = "Domain knowledge base queries",
                GetDisplayValue = GetKnowledgeBaseQueryInputItemsDisplayValue
            };
            return domainsKnowledgeBaseQueryParameter;
        }

        private static Parameter InitPastMemoriesQueryResultsParameter()
        {
            var pastMemoriesQueryResultsParameter = new Parameter
            {
                Name = "Past memories query results",
                GetDisplayValue = GetAgentMemoryQueryResultsDisplayValue
            };
            return pastMemoriesQueryResultsParameter;
        }

        private static Parameter InitKnowledgeBaseQueryResultsParameter()
        {
            var knowledgeBaseQueryResultsParameter = new Parameter
            {
                Name = "Knowledge base query results",
                GetDisplayValue = GetKnowledgeBaseQueryResultsDisplayValue
            };
            return knowledgeBaseQueryResultsParameter;
        }

        private static Parameter InitDomainsKnowledgeBaseDocumentsContentParameter()
        {
            var domainsKnowledgeBaseDocumentsContentParameter = new Parameter
            {
                Name = "Domain knowledge base documents",
                GetDisplayValue = GetKnowledgeBaseDocumentsContentDisplayValue
            };
            return domainsKnowledgeBaseDocumentsContentParameter;
        }

        private static Parameter InitBusinessRequirementsParameter()
        {
            var businessRequirementsParameter = new Parameter
            {
                Name = "Business requirements"
            };
            return businessRequirementsParameter;
        }

        private static Parameter InitFunctionalAnalystRejectedParameter()
        {
            var functionalAnalystRejectedParameter = new Parameter
            {
                Name = "Functional analyst rejected",
                GetDisplayValue = GetBooleanDisplayValue
            };
            return functionalAnalystRejectedParameter;
        }

        private static Parameter InitFunctionalAnalystRejectReasonsParameter()
        {
            var functionalAnalystRejectReasonsParameter = new Parameter
            {
                Name = "Functional analyst reject reasons"
            };
            return functionalAnalystRejectReasonsParameter;
        }

        private static Parameter InitTechnicalSpecificationParameter()
        {
            var technicalSpecificationParameter = new Parameter
            {
                Name = "Technical specification"
            };
            return technicalSpecificationParameter;
        }

        private static Parameter InitTechnicalAnalystRejectedParameter()
        {
            var technicalAnalystRejectedParameter = new Parameter
            {
                Name = "Technical analyst rejected",
                GetDisplayValue = GetBooleanDisplayValue
            };
            return technicalAnalystRejectedParameter;
        }

        private static Parameter InitTechnicalAnalystRejectReasonsParameter()
        {
            var technicalAnalystRejectReasonsParameter = new Parameter
            {
                Name = "Technical analyst reject reasons"
            };
            return technicalAnalystRejectReasonsParameter;
        }

        private static Parameter InitShouldEngageCoderParameter()
        {
            var shouldEngageCoderParameter = new Parameter
            {
                Name = "Should engage coder",
                GetDisplayValue = GetBooleanDisplayValue
            };
            return shouldEngageCoderParameter;
        }

        private static Parameter InitAPISKnowledgeBaseQueryResultsParameter()
        {
            var apisKnowledgeBaseQueryResultsParameter = new Parameter
            {
                Name = "API knowledge base query results",
                GetDisplayValue = GetKnowledgeBaseQueryResultsDisplayValue
            };
            return apisKnowledgeBaseQueryResultsParameter;
        }

        private static Parameter InitSelectedAPIsFileLocationsParameter()
        {
            var selectedAPIsFileLocationsParameter = new Parameter
            {
                Name = "Selected API file locations",
                GetDisplayValue = GetStringEnumerableDisplayValue
            };
            return selectedAPIsFileLocationsParameter;
        }

        private static Parameter InitDocumentationContentParameter()
        {
            var documentationContentParameter = new Parameter
            {
                Name = "Documentation content"
            };
            return documentationContentParameter;
        }

        private static Parameter InitGeneratedCodeParameter()
        {
            var generatedCodeParameter = new Parameter
            {
                Name = "Generated code"
            };
            return generatedCodeParameter;
        }

        private static Parameter InitLastCodeWithLineNumbersParameter()
        {
            var lastCodeWithLineNumbersParameter = new Parameter
            {
                Name = "Generated code with line numbers"
            };
            return lastCodeWithLineNumbersParameter;
        }

        private static Parameter InitCodeExecutionFailuresDetectorIterationCountParameter()
        {
            var codeExecutionFailuresDetectorIterationCountParameter = new Parameter
            {
                Name = "Code execution failures detector iteration count",
                GetDisplayValue = GetInt32DisplayValue
            };
            return codeExecutionFailuresDetectorIterationCountParameter;
        }

        private static Parameter InitCodeExecutionAnalysisParameter()
        {
            var codeExecutionAnalysisParameter = new Parameter
            {
                Name = "Code execution analysis"
            };
            return codeExecutionAnalysisParameter;
        }

        private static Parameter InitSandboxResultParameter()
        {
            var sandboxResultParameter = new Parameter
            {
                Name = "Sandbox result"
            };
            return sandboxResultParameter;
        }

        private static Parameter InitSandboxExecutionIdParameter()
        {
            var sandboxExecutionIdParameter = new Parameter
            {
                Name = "Sandbox execution id"
            };
            return sandboxExecutionIdParameter;
        }

        private static Parameter InitCodeExecutionResultTypeParameter()
        {
            var codeExecutionResultTypeParameter = new Parameter
            {
                Name = "Code execution result type",
                GetDisplayValue = GetSandboxResultTypeDisplayValue
            };
            return codeExecutionResultTypeParameter;
        }

        private static Parameter InitExecutionErrorParameter()
        {
            var executionErrorParameter = new Parameter
            {
                Name = "Execution error",
                GetDisplayValue = GetBooleanDisplayValue
            };
            return executionErrorParameter;
        }

        private static Parameter InitDomainExpertOutputParameter()
        {
            var domainExpertOutputParameter = new Parameter
            {
                Name = "Domain expert output"
            };
            return domainExpertOutputParameter;
        }

        private static Parameter InitPersonalAssistantOpeningSentenceParameter()
        {
            var personalAssistantOpeningSentenceParameter = new Parameter
            {
                Name = "Personal assistant opening sentence"
            };
            return personalAssistantOpeningSentenceParameter;
        }

        private static Parameter InitPersonalAssistantClosingSentenceParameter()
        {
            var personalAssistantClosingSentenceParameter = new Parameter
            {
                Name = "Personal assistant closing sentence"
            };
            return personalAssistantClosingSentenceParameter;
        }

        private static Parameter InitPersonalAssistantConvenienceErrorSentenceParameter()
        {
            var personalAssistantConvenienceErrorSentenceParameter = new Parameter
            {
                Name = "Personal assistant convenience error sentence"
            };
            return personalAssistantConvenienceErrorSentenceParameter;
        }

        private static Parameter InitFinalAnswerParameter()
        {
            var finalAnswerParameter = new Parameter
            {
                Name = "Final answer"
            };
            return finalAnswerParameter;
        }

        public static IEnumerable<Parameter> InitParameters()
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
    }
}
