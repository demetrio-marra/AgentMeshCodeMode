using AgentMesh.Utils;

namespace AgentMesh.Application.Models.SemanticSearch
{
    /// <summary>
    /// Input for the semantic search executor, containing the actionable requirements to search for
    /// and the optional role of the requesting agent.
    /// </summary>
    public class SemanticSearchExecutorInput
    {
        /// <summary>
        /// The actionable requirements used to drive the semantic search.
        /// </summary>
        public IEnumerable<string> ActionableRequirements { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// Optional role of the agent requesting the search, used to scope or filter results.
        /// </summary>
        public string? AgentRole { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Actionable requirements", ActionableRequirements.Any() ? ListsFormatter.ToBulletList(ActionableRequirements) : "(No actionable requirements)" },
                { "Agent role", AgentRole ?? string.Empty }
            };
        }
    }
}
