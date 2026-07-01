using static AgentMesh.Models.SearchQueriesConciliator.SearchQueriesConciliatorAgentOutput;

namespace AgentMesh.Models.SearchQueriesConciliator
{
    public class SearchQueriesConciliatorAgentInput
    {
        public IEnumerable<KnowledgeBaseSearchQuery> ExtractedKnowledgeBaseSearchQueries { get; set; } = [];
        public IEnumerable<KnowledgeBaseSearchQuery> CachedKnowledgeBaseSearchQueries { get; set; } = [];
        public IEnumerable<MemorySearchQuery> ExtractedMemorySearchQueries { get; set; } = [];
        public IEnumerable<MemorySearchQuery> CachedMemorySearchQueries { get; set; } = [];
    }
}
