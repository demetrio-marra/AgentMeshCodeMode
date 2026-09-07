namespace AgentMesh.Application.Models.Costs
{
    public readonly record struct AgentExecutionCost(string AgentName,
        decimal CostPerMillionInputTokens,
        decimal CostPerMillionOutputTokens,
        int ConsumedInputTokens,
        int ConsumedOutputTokens,
        decimal? CostPerHour,
        TimeSpan Elapsed)
    {
        public readonly bool IsHourlyCost => CostPerHour.HasValue;
        public readonly decimal InputCost => IsHourlyCost ? 0 : ConsumedInputTokens / 1_000_000m * CostPerMillionInputTokens;
        public readonly decimal OutputCost => IsHourlyCost ? 0 : ConsumedOutputTokens / 1_000_000m * CostPerMillionOutputTokens;
        public readonly decimal HourlyCost => !IsHourlyCost ? 0 : (decimal)Elapsed.TotalHours * CostPerHour!.Value;
        public readonly decimal TotalCost => IsHourlyCost ? HourlyCost : InputCost + OutputCost;
        public readonly int TotalTokens => ConsumedInputTokens + ConsumedOutputTokens;
    }
}
