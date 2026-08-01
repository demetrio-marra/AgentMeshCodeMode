using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Utils;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Workflows.ParameterSerializers
{
    public class DisplayValuesEWParameterSerializer : IEWParameterSerializer
    {
        public string Serialize<T>(T obj)
        {
            return obj switch
            {
                null => EWParameterConstants.NoDataPlaceholder,
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
