using AgentMesh.Application.Contracts;
using AgentMesh.Models.ApiDocumentation;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    /// <summary>
    /// Implements <see cref="IApiDocumentationExecutor"/> by delegating to <see cref="IApiDocumentationService"/>
    /// and formatting the results into a single documentation string.
    /// </summary>
    public class ApiDocumentationExecutor : IApiDocumentationExecutor
    {
        private readonly ILogger<ApiDocumentationExecutor> _logger;
        private readonly IApiDocumentationService _apiDocumentationService;

        public ApiDocumentationExecutor(
            IApiDocumentationService apiDocumentationService,
            ILogger<ApiDocumentationExecutor> logger)
        {
            _apiDocumentationService = apiDocumentationService;
            _logger = logger;
        }

        public async Task<ApiDocumentationExecutorOutput> ExecuteAsync(
            ApiDocumentationExecutorInput input,
            CancellationToken cancellationToken = default)
        {
            var mentionedApis = new HashSet<string>(input.MentionedApis, StringComparer.OrdinalIgnoreCase);

            var apiDocs = await _apiDocumentationService.GetApiDocumentationAsync(mentionedApis);
            _logger.LogDebug("Fetched {CountFound} documentation for {Count} APIs.", apiDocs.Count(), mentionedApis.Count);

            foreach (var api in mentionedApis)
            {
                if (!apiDocs.Any(doc => doc.ApiName.Equals(api, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("API documentation for '{Api}' was not found.", api);
                }
            }

            var apiDocumentation = string.Join("\n\n", apiDocs.Select(doc => $"API: {doc.ApiName}\nDescription: {doc.Documentation}"));

            return new ApiDocumentationExecutorOutput
            {
                ApiDocumentation = apiDocumentation
            };
        }
    }
}
