using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.DocumentsCache
{
    public class KnowledgeBaseCachedQuery
    {
        public IEnumerable<string> Collections { get; set; } = [];
        public IEnumerable<KnowledgeBaseQueryInputItem> Queries { get; set; } = [];
        public string? UserIntent { get; set; }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var collection in Collections.Order())
                hash.Add(collection);

            foreach (var query in Queries.OrderBy(q => q.Query).ThenBy(q => q.SearchType))
            {
                hash.Add(query.Query);
                hash.Add(query.SearchType);
            }

            hash.Add(UserIntent);

            return hash.ToHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is not KnowledgeBaseCachedQuery other)
                return false;

            return UserIntent == other.UserIntent
                && Collections.Order().SequenceEqual(other.Collections.Order())
                && Queries.OrderBy(q => q.Query).ThenBy(q => q.SearchType)
                    .Select(q => (q.Query, q.SearchType))
                    .SequenceEqual(
                        other.Queries.OrderBy(q => q.Query).ThenBy(q => q.SearchType)
                            .Select(q => (q.Query, q.SearchType)));
        }
    }
}
