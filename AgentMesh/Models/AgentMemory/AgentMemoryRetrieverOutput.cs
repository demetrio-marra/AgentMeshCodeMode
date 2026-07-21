using AgentMesh.Utils;

namespace AgentMesh.Models.AgentMemory
{
    /// <summary>
    /// Output of the agent memory retriever, containing the matching memory items.
    /// </summary>
    public class AgentMemoryRetrieverOutput
    {
        /// <summary>
        /// The list of memory items that match the search query.
        /// </summary>
        public IEnumerable<AgentMemoryQueryResultItem> Items { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Items", Items.Any() ? ListsFormatter.ToBulletList(Items.Select(item => $"{item.Memory} Confidence: {item.Confidence}")) : "(No items found)" }
            };
        }
    }
}
