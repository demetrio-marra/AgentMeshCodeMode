namespace AgentMesh.Application.Models.FunctionalAnalyst
{
    public readonly record struct FunctionalAnalysisResult
    {
        public string BusinessRequirements { get; init; }
        public required bool RequestRejected { get; init; }
        public string? ReasonOfRejection { get; init; }
    }
}
