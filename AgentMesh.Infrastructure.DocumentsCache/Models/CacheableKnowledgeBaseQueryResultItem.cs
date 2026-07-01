using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Infrastructure.DocumentsCache.Models
{
    internal class CacheableKnowledgeBaseQueryResultItem : KnowledgeBaseQueryResultItem, IEquatable<CacheableKnowledgeBaseQueryResultItem>
    {
        public CacheableKnowledgeBaseQueryResultItem()
        {
            
        }

        public CacheableKnowledgeBaseQueryResultItem(KnowledgeBaseQueryResultItem item)
        {
            Id = item.Id;
            Title = item.Title;
            Summary = item.Summary;
            File = item.File;
            Relevance = item.Relevance;
        }

        public bool Equals(CacheableKnowledgeBaseQueryResultItem? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Summary, other.Summary, StringComparison.OrdinalIgnoreCase)
                && string.Equals(File, other.File, StringComparison.OrdinalIgnoreCase)
                && Relevance == other.Relevance;
        }

        public override bool Equals(object? obj)
        {
            return obj is CacheableKnowledgeBaseQueryResultItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Id.ToUpperInvariant(),
                Title.ToUpperInvariant(),
                Summary?.ToUpperInvariant(),
                File?.ToUpperInvariant(),
                Relevance);
        }

        public static bool operator ==(CacheableKnowledgeBaseQueryResultItem? left, CacheableKnowledgeBaseQueryResultItem? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CacheableKnowledgeBaseQueryResultItem? left, CacheableKnowledgeBaseQueryResultItem? right)
        {
            return !Equals(left, right);
        }
    }
}
