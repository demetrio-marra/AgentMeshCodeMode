using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Models;
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

        public static string GetKnowledgeDisplayValue(KnowledgeQuery query)
        {
            if (query == null)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }
            var displayValue = $"Query Text: {query.QueryText}\n" +
                               $"Retrieval Kind: {query.QueryRetrievalKind}\n" +
                               $"Max Results: {query.MaxResults}\n" +
                               $"Include Entities: {query.IncludeEntities}\n" +
                               $"Include Relations: {query.IncludeRelations}\n" +
                               $"Primary Relevance Keywords: {GetListOfStringsDisplayValue(query.PrimaryRelevanceKeywords)}\n" +
                               $"Secondary Relevance Keywords: {GetListOfStringsDisplayValue(query.SecondaryRelevanceKeywords)}";
            return displayValue;
        }

        public static string GetKnowledgeQueryResultDisplayValue(KnowledgeQueryResult result)
        {
            if (result == null)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            var displayValue = $"Contents:\n{GetListOfStringsDisplayValue(result.Contents?.Select(c => GetDisplayValueForKnowledgeContentItem(c)))}\n" +
                               $"Entities:\n{GetListOfStringsDisplayValue(result.Entities?.Select(e => GetDisplayValueForKnowledgeEntityItem(e)))}\n" +
                               $"Relations:\n{GetListOfStringsDisplayValue(result.Relations?.Select(r => GetDisplayValueForKnowledgeRelationItem(r)))}";
            return displayValue;
        }

        private static string GetDisplayValueForKnowledgeContentItem(KnowledgeContentItem item)
        {
            if (item == null)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            var truncatedContent = item.Content.Length > 100 ? item.Content.Substring(0, 100) + "...(lenght: " + item.Content.Length + ")": item.Content;

            return $"Id: {item.Id}, Source: {item.Source}, Content: {truncatedContent}";
        }

        public static string GetDisplayValueForKnowledgeContentItem(IEnumerable<KnowledgeContentItem> items)
        {
            if (!items.Any())
            {
                return EWParameterConstants.NoDataPlaceholder;
            }

            return ListsFormatter.ToBulletList(items.Select(item => GetDisplayValueForKnowledgeContentItem(item)));
        }

        private static string GetDisplayValueForKnowledgeEntityItem(KnowledgeEntityItem item)
        {
            if (item == null)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }
            return $"Entity: {item.Entity}, Type: {item.Type}, Description: {item.Description}, Source: {item.ContentItem.Source}";
        }

        private static string GetDisplayValueForKnowledgeRelationItem(KnowledgeRelationItem item)
        {
            if (item == null)
            {
                return EWParameterConstants.NoDataPlaceholder;
            }
            return $"Relation: {item.Description}, Source: {item.ContentItem.Source}";
        }
    }
}
