using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Infrastructure.DocumentsCache.Models
{
    internal class CacheableAgentMemoryQueryResultItem : AgentMemoryQueryResultItem, IEquatable<CacheableAgentMemoryQueryResultItem>
    {
        public CacheableAgentMemoryQueryResultItem()
        {

        }

        public CacheableAgentMemoryQueryResultItem(AgentMemoryQueryResultItem item)
        {
            Memory = item.Memory;
            Confidence = item.Confidence;
        }

        public bool Equals(CacheableAgentMemoryQueryResultItem? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Memory, other.Memory, StringComparison.OrdinalIgnoreCase)
                && Confidence.Equals(other.Confidence);
        }

        public override bool Equals(object? obj)
        {
            return obj is CacheableAgentMemoryQueryResultItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Memory.ToUpperInvariant(), Confidence);
        }

        public static bool operator ==(CacheableAgentMemoryQueryResultItem? left, CacheableAgentMemoryQueryResultItem? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CacheableAgentMemoryQueryResultItem? left, CacheableAgentMemoryQueryResultItem? right)
        {
            return !Equals(left, right);
        }
    }
}
