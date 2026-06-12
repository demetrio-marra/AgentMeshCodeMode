using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;

namespace AgentMesh.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Dummy implementation of the IApiDocumentationService interface that returns empty results.
    /// </summary>
    public class DummyApiDocumentationRepository : IApiDocumentationService
    {
        public Task<ApiDocumentation> GetApiDocumentationAsync(string apiName)
        {
            throw new KeyNotFoundException($"API documentation not found for API: {apiName}");
        }

        public Task<IEnumerable<ApiDocumentation>> GetApiDocumentationAsync(IEnumerable<string> apiNames)
        {
            return Task.FromResult(Enumerable.Empty<ApiDocumentation>());
        }
    }
}
