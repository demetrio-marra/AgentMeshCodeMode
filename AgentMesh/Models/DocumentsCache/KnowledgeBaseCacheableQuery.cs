using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.DocumentsCache
{
    public class KnowledgeBaseCacheableQuery : IEquatable<KnowledgeBaseCacheableQuery>
    {
        public string Query { get; set; } = string.Empty;
        public KnowledgeBaseQuerySearchType SearchType { get; set; }

        public bool Equals(KnowledgeBaseCacheableQuery? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Query, other.Query, StringComparison.Ordinal)
                && SearchType == other.SearchType;
        }

        public override bool Equals(object? obj)
        {
            return obj is KnowledgeBaseCacheableQuery other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Query, SearchType);
        }

        public static bool operator ==(KnowledgeBaseCacheableQuery? left, KnowledgeBaseCacheableQuery? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KnowledgeBaseCacheableQuery? left, KnowledgeBaseCacheableQuery? right)
        {
            return !Equals(left, right);
        }
    }
}
