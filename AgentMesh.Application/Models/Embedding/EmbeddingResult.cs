namespace AgentMesh.Application.Models.Embedding
{
    public class EmbeddingResult
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public int TotalTokens { get; set; }
    }
}
