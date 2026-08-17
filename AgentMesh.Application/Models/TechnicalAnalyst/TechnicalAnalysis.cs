namespace AgentMesh.Application.Models.TechnicalAnalyst
{
    public readonly record struct TechnicalAnalysis
    {
        public string? TechnicalSpecification { get; init; }
        public bool RequestRejected { get; init; }
        public string? RequestRejectionReason { get; init; }
        public IEnumerable<string>? FilteredApisDocumentationFiles { get; init; }
    }
}
