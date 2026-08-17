using AgentMesh.Application.Models.Embedding;

namespace AgentMesh.Application.Contracts
{
    /// <summary>
    /// Generates vector embeddings for input text
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates a vector embedding for the given input text. The embedding is represented as an array of floats.
        /// </summary>
        /// <param name="input">The input text to generate the embedding for.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated embedding as an array of floats and token usage information.</returns>
        Task<EmbeddingResult> GetEmbeddingAsync(string input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates vector embeddings for a batch of input texts. Each embedding is represented as an array of floats, and the result is a collection of embeddings corresponding to the input texts.
        /// </summary>
        /// <param name="inputs">The collection of input texts to generate embeddings for.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of generated embeddings with token usage information.</returns>
        Task<IEnumerable<EmbeddingResult>> GetEmbeddingAsync(IEnumerable<string> inputs, CancellationToken cancellationToken = default);
    }
}
