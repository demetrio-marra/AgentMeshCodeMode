using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Utils;

namespace AgentMesh.Application.Utils
{
    public static class EWParameterDisplayUtils
    {
        public static string GetBooleanDisplayValue(bool? value)
        {
            return value?.ToString() ?? EWParameter<bool>.NoDataPlaceholder;
        }

        public static string GetInt32DisplayValue(int? value)
        {
            return value?.ToString() ?? EWParameter<int>.NoDataPlaceholder;
        }

        public static string GetIntentCategoryDisplayValue(UserIntentCategory? value)
        {
            return value?.ToString() ?? EWParameter<UserIntentCategory>.NoDataPlaceholder;
        }

        public static string GetSandboxResultTypeDisplayValue(SandboxResultType? value)
        {
            return value?.ToString() ?? EWParameter<SandboxResultType>.NoDataPlaceholder;
        }

        public static string GetStringEnumerableDisplayValue(IEnumerable<string>? values)
        {
            if (values == null || !values.Any())
            {
                return EWParameter<IEnumerable<string>>.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(values);
        }

        public static string GetContextMessagesDisplayValue(IEnumerable<ContextMessage>? messages)
        {
            if (messages == null)
            {
                return EWParameter<IEnumerable<ContextMessage>>.NoDataPlaceholder;
            }

            var count = messages.Count();
            if (count == 0)
            {
                return EWParameter<IEnumerable<ContextMessage>>.NoDataPlaceholder;
            }

            return $"Messages count: {count}";
        }

        public static string GetAgentMemoryItemsDisplayValue(IEnumerable<AgentMemoryItem>? items)
        {
            if (items == null || !items.Any())
            {
                return EWParameter<IEnumerable<AgentMemoryItem>>.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => item.Memory));
        }

        public static string GetAgentMemoryQueryResultsDisplayValue(IEnumerable<AgentMemoryQueryResultItem>? items)
        {
            if (items == null || !items.Any())
            {
                return EWParameter<IEnumerable<AgentMemoryQueryResultItem>>.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => $"{item.Memory} Confidence: {item.Confidence}"));
        }

        public static string GetKnowledgeBaseQueryInputItemsDisplayValue(IEnumerable<KnowledgeBaseQueryInputItem>? items)
        {
            if (items == null || !items.Any())
            {
                return EWParameter<IEnumerable<KnowledgeBaseQueryInputItem>>.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => item.ToString()));
        }

        public static string GetKnowledgeBaseQueryResultsDisplayValue(object? rawValue)
        {
            var knowledgeBaseQueryResult = DeserializeRawValue<KnowledgeBaseQueryResult>(rawValue);
            var results = knowledgeBaseQueryResult?.Results ?? DeserializeRawValue<IEnumerable<KnowledgeBaseQueryResultItem>>(rawValue);
            if (results == null || !results.Any())
            {
                return EWParameter<object>.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(results.Select(item => $"{item.File} - Title: {item.Title} - Relevance: {item.Relevance}"));
        }

        public static string GetKnowledgeBaseDocumentsContentDisplayValue(IEnumerable<KnowledgeBaseDocumentContent>? documents)
        {
            if (documents == null || !documents.Any())
            {
                return EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>.NoDataPlaceholder;
            }

            var files = documents
                .Select(document => document.File)
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Cast<string>()
                .ToList();

            if (!files.Any())
            {
                return EWParameter<IEnumerable<KnowledgeBaseDocumentContent>>.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(files);
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
    }
}
