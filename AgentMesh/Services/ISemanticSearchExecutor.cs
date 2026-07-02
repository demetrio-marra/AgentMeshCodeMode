using AgentMesh.Models.SemanticSearch;

namespace AgentMesh.Services
{
    /// <summary>
    /// A non-smart executor responsible for retrieving relevant API documentation
    /// via semantic search, decoupling data retrieval from agent logic.
    /// </summary>
    public interface ISemanticSearchExecutor : IExecutor<SemanticSearchExecutorInput, SemanticSearchExecutorOutput>
    {
    }
}
