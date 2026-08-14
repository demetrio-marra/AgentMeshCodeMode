using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Utils;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Helpers
{
    public class DisplayValuesEWParameterSerializer : IEWParameterSerializer
    {
        public string Serialize<T>(T obj)
        {
            return obj switch
            {
                null => EWParameterConstants.NoDataPlaceholder,
                DateTime datetimeValue => datetimeValue.ToString("yyyy-MM-dd HH:mm:ss"),
                IEnumerable<string> missingValues => EWParameterDisplayUtils.GetListOfStringsDisplayValue(missingValues),
                IEnumerable<ContextMessage> contextMessages => EWParameterDisplayUtils.GetContextMessagesDisplayValue(contextMessages),
                IEnumerable<AgentMemoryQueryResultItem> queryResults => EWParameterDisplayUtils.GetAgentMemoryQueryResultsDisplayValue(queryResults),
                IEnumerable<AgentMemoryItem> memoryItems => EWParameterDisplayUtils.GetAgentMemoryItemsDisplayValue(memoryItems),
                IEnumerable<KnowledgeBaseQueryInputItem> queryInputItems => EWParameterDisplayUtils.GetKnowledgeBaseQueryInputItemsDisplayValue(queryInputItems),
                IEnumerable<KnowledgeBaseQueryResultItem> queryResultItems => EWParameterDisplayUtils.GetKnowledgeBaseQueryResultsDisplayValue(queryResultItems),
                IEnumerable<KnowledgeBaseDocumentContent> documents => EWParameterDisplayUtils.GetKnowledgeBaseDocumentsContentDisplayValue(documents),
                _ => throw new ArgumentException($"Unsupported type '{obj.GetType().FullName}' for display value serialization.", nameof(obj))
            };
        }
    }
}
