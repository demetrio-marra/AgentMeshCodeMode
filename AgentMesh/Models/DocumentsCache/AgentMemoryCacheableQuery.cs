namespace AgentMesh.Models.DocumentsCache
{
    public class AgentMemoryCacheableQuery : IEquatable<AgentMemoryCacheableQuery>
    {
        public string Query { get; set; } = string.Empty;

        public bool Equals(AgentMemoryCacheableQuery? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Query, other.Query, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is AgentMemoryCacheableQuery other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Query.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator ==(AgentMemoryCacheableQuery? left, AgentMemoryCacheableQuery? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AgentMemoryCacheableQuery? left, AgentMemoryCacheableQuery? right)
        {
            return !Equals(left, right);
        }
    }
}
