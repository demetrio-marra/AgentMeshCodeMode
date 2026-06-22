using AgentMesh.Application.Models;

namespace AgentMesh.Application.Contracts
{
    /// <summary>
    /// Provides service to access knowledge base information, such as retrieving relevant knowledge base entries based on different criteria (eg. keywords, semantic similarity, etc.).
    /// This service can be used to enhance agent responses with relevant information from the knowledge base, improving the quality and relevance of the generated content.
    /// </summary>
    public interface IKnowledgeBaseService
    {
        /// <summary>
        /// Searches the knowledge base for entries that match the provided keywords. 
        /// This method is useful for retrieving specific information based on keywords or phrases, ensuring that the results are directly relevant to the search query.
        /// Use it for fast retrieval of information when you have specific terms in mind and want to find matches in the knowledge base.
        /// </summary>
        /// <param name="searchTerms">A collection of search terms to use for the keyword search. Each term is matched against the knowledge base entries.</param>
        /// <param name="collections">An optional collection of knowledge base collections to limit the search scope. If null or empty, the search will be performed across all collections.</param>
        /// <param name="rerank">If <see langword="true"/>, the results are re-ranked for improved relevance; otherwise, the default ranking
        /// is used.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A collection of <see
        /// cref="KnowledgeBaseQueryResult"/> objects matching the search terms. The collection is empty if no matches
        /// are found.</returns>
        Task<IEnumerable<KnowledgeBaseQueryResult>> KeywordsSearch(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, bool rerank = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a semantic search using the specified search terms and returns matching knowledge base results
        /// asynchronously.
        /// </summary>
        /// <param name="searchTerms">A collection of search terms to use for the semantic search. Each term is compared semantically against the
        /// knowledge base.</param>
        /// <param name="collections">An optional collection of knowledge base collections to limit the search scope. If null or empty, the search will be performed across all collections.</param>
        /// <param name="rerank">If <see langword="true"/>, the results are re-ranked for improved relevance; otherwise, the default ranking
        /// is used.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A collection of <see
        /// cref="KnowledgeBaseQueryResult"/> objects matching the search terms. The collection is empty if no matches
        /// are found.</returns>
        Task<IEnumerable<KnowledgeBaseQueryResult>> SemanticSearchAsync(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, bool rerank = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs both keyword and semantic search using the specified search terms and returns matching knowledge base results asynchronously. Also reranks automatically for improved relevance.
        /// </summary>
        /// <param name="searchTerms">A collection of search terms to use for the search. Each term is compared against the knowledge base.</param>
        /// <param name="collections">An optional collection of knowledge base collections to limit the search scope. If null or empty, the search will be performed across all collections.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A collection of <see cref="KnowledgeBaseQueryResult"/> objects matching the search terms. The collection is empty if no matches are found.</returns>
        Task<IEnumerable<KnowledgeBaseQueryResult>> FindAsync(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the content of a knowledge base entry identified by the specified ID.
        /// </summary>
        /// <param name="id">The unique identifier of the knowledge base entry to retrieve. Cannot be null or empty.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the content of the knowledge
        /// base entry as a string, or null if the entry does not exist.</returns>
        Task<string> GetKnowledgeBaseEntryContentAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the content of multiple knowledge base entries identified by the specified file names.
        /// </summary>
        /// <param name="fileNames">A collection of unique identifiers of the knowledge base entries to retrieve. Cannot be null or empty.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary mapping each
        /// requested file name to its content, or <see langword="null"/> if the entry does not exist.</returns>
        Task<IDictionary<string, string?>> GetKnowledgeBaseEntriesContentAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default);
    }
}
