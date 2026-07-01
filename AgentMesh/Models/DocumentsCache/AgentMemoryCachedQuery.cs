namespace AgentMesh.Models.DocumentsCache
{
    public class AgentMemoryCachedQuery
    {
        public string Query { get; set; } = string.Empty;

        public override int GetHashCode() => Query.GetHashCode(StringComparison.Ordinal);

        public override bool Equals(object? obj) =>
            obj is AgentMemoryCachedQuery other
            && Query == other.Query;
    }
}
