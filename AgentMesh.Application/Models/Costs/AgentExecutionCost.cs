namespace AgentMesh.Application.Models.Costs
{
    public readonly record struct AgentExecutionCost(string AgentName,
        decimal CostPerMillionInputTokens,
        decimal CostPerMillionOutputTokens,
        int ConsumedInputTokens,
        int ConsumedOutputTokens)
    {
        public readonly decimal InputCost => ConsumedInputTokens / 1_000_000m * CostPerMillionInputTokens;
        public readonly decimal OutputCost => ConsumedOutputTokens / 1_000_000m * CostPerMillionOutputTokens;
        public readonly decimal TotalCost => InputCost + OutputCost;
        public readonly int TotalTokens => ConsumedInputTokens + ConsumedOutputTokens;
    }
}
