using AgentMesh.Application.Contracts;
using AgentMesh.Models.SemanticSearch;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    /// <summary>
    /// Implements <see cref="ISemanticSearchExecutor"/> by delegating to <see cref="ISemanticSearchService"/>
    /// and formatting the results into a single documentation string.
    /// </summary>
    public class SemanticSearchExecutor : ISemanticSearchExecutor
    {
        private readonly ILogger<SemanticSearchExecutor> _logger;
        private readonly ISemanticSearchService _semanticSearchService;

        public SemanticSearchExecutor(
            ISemanticSearchService semanticSearchService,
            ILogger<SemanticSearchExecutor> logger)
        {
            _semanticSearchService = semanticSearchService;
            _logger = logger;
        }

        public async Task<SemanticSearchExecutorOutput> ExecuteAsync(
            SemanticSearchExecutorInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing SemanticSearchExecutor.");
            _logger.LogDebug("SemanticSearchExecutor - Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            var apiDocumentation = string.Empty;

            if (input.ActionableRequirements != null && input.ActionableRequirements.Any())
            {
                var results = await _semanticSearchService.SearchByActionableRequirements(
                    input.ActionableRequirements,
                    input.AgentRole,
                    cancellationToken);

                if (results.Any())
                {
                    apiDocumentation = string.Join("\n\n", results.Select(d => d.FoundInformation));
                }
                else
                {
                    _logger.LogInformation("SemanticSearchExecutor - No relevant API documentation found.");
                }
            }

            stopwatch.Stop();

            var output = new SemanticSearchExecutorOutput { ApiDocumentation = apiDocumentation };

            _logger.LogDebug("SemanticSearchExecutor completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("SemanticSearchExecutor - Output length: {Length}", apiDocumentation.Length);

            return output;
        }
    }
}
