using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.DocumentsCache
{
    public class KnowledgeBaseCachedQuery
    {
        public IEnumerable<KnowledgeBaseQueryInputItem> Queries { get; set; } = [];

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var query in Queries.OrderBy(q => q.Query).ThenBy(q => q.SearchType))
            {
                hash.Add(query.Query);
                hash.Add(query.SearchType);
            }

            return hash.ToHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is not KnowledgeBaseCachedQuery other)
                return false;

            return Queries.OrderBy(q => q.Query).ThenBy(q => q.SearchType)
                .Select(q => (q.Query, q.SearchType))
                .SequenceEqual(
                    other.Queries.OrderBy(q => q.Query).ThenBy(q => q.SearchType)
                        .Select(q => (q.Query, q.SearchType)));
        }
    }
}
