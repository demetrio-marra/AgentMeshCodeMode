using AgentMesh.Models.ApiDocumentation;

namespace AgentMesh.Services
{
    /// <summary>
    /// A non-smart executor responsible for retrieving API documentation for the mentioned APIs,
    /// decoupling data retrieval from agent logic.
    /// </summary>
    public interface IApiDocumentationExecutor : IExecutor<ApiDocumentationExecutorInput, ApiDocumentationExecutorOutput>
    {
    }
}
