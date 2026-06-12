using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class DummySemanticSearchService : ISemanticSearchService
    {
        private readonly ILogger<DummySemanticSearchService> _logger;

        public DummySemanticSearchService(ILogger<DummySemanticSearchService> logger)
        {
            _logger = logger;
        }

        public Task<IEnumerable<SemanticSearchResult>> SearchByActionableRequirements(IEnumerable<string> actionableRequirements,
            string? agentRole = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Dummy semantic search service returning empty results for actionable requirements: {0}, agentRole: {1}", 
                string.Join(", ", actionableRequirements), agentRole ?? "none");

            return Task.FromResult(Enumerable.Empty<SemanticSearchResult>());
        }
    }
}
