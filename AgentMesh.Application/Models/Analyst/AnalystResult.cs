namespace AgentMesh.Application.Models.Analyst
{
    public readonly record struct AnalystResult
    {
        public required bool Accepted { get; init; }
        public string? Specification { get; init; }
        public IEnumerable<string> ContentIds { get; init; }
        public string? RejectReason { get; init; }
        public IEnumerable<string>? RejectReasons { get; init; }
    }
}
