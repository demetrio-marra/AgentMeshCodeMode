using AgentMesh.Services;

namespace AgentMesh.Infrastructure.SemanticSearch.Services
{
    public class QDrantSemanticSearchService : ISemanticSearchService
    {
        public async Task<IEnumerable<string>> SearchAsync(IEnumerable<string> actionableRequirements, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
