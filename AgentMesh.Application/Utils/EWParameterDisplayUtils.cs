using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Utils;

namespace AgentMesh.Application.Utils
{
    public static class EWParameterDisplayUtils
    {
        public static string GetListOfStringsDisplayValue(IEnumerable<string>? values)
        {
            if (values == null || !values.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }
            return ListsFormatter.ToBulletList(values.Select(value => $"{value}"));
        }

        public static string GetContextMessagesDisplayValue(IEnumerable<ContextMessage>? messages)
        {
            if (messages == null)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            var count = messages.Count();
            if (count == 0)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return $"{count}";
        }

        public static string GetAgentMemoryItemsDisplayValue(IEnumerable<AgentMemoryItem>? items)
        {
            if (items == null || !items.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => $"{item.Memory}"));
        }

        public static string GetAgentMemoryQueryResultsDisplayValue(IEnumerable<AgentMemoryQueryResultItem>? items)
        {
            if (items == null || !items.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => $"{item.Memory} - Confidence: {item.Confidence}"));
        }

        public static string GetKnowledgeBaseQueryInputItemsDisplayValue(IEnumerable<KnowledgeBaseQueryInputItem>? items)
        {
            if (items == null || !items.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => item.ToString()));
        }

        public static string GetKnowledgeBaseQueryResultsDisplayValue(IEnumerable<KnowledgeBaseQueryResultItem>? items)
        {
            if (items == null || !items.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => $"{item.File} - Title: {item.Title} - Relevance: {item.Relevance}"));
        }

        public static string GetKnowledgeBaseDocumentsContentDisplayValue(IEnumerable<KnowledgeBaseDocumentContent>? documents)
        {
            if (documents == null || !documents.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            var files = documents
                .Select(document => document.File)
                .Where(file => !string.IsNullOrWhiteSpace(file))
                .Cast<string>()
                .ToList();

            if (!files.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(files);
        }
    }
}
