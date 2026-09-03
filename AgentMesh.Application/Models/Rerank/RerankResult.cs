namespace AgentMesh.Application.Models.Rerank
{
    public readonly record struct RerankResult
    {
        public required List<RerankResultItem> RerankedDocuments { get; init; }
        public int CompletionTokens { get; init; }
        public int PromptTokens { get; init; }
        public int TotalTokens => CompletionTokens + PromptTokens;
    }
}
