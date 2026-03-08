using AgentMesh.Application.Configuration;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class BusinessAdvisorAgent : IBusinessAdvisorAgent
    {
        private static readonly string AgentRole = "BusinessAdvisor";

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<BusinessAdvisorAgent> _logger;
        private readonly ISemanticSearchService _semanticSearchService;

        public BusinessAdvisorAgent(
            [FromKeyedServices(BusinessAdvisorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessAdvisorAgentConfiguration configuration,
            ILogger<BusinessAdvisorAgent> logger,
            ISemanticSearchService semanticSearchService)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _semanticSearchService = semanticSearchService;
        }

        public async Task<BusinessAdvisorAgentOutput> ExecuteAsync(
            BusinessAdvisorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing BusinessAdvisorAgent.");
            _logger.LogDebug("BusinessAdvisorAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            IEnumerable<SemanticSearchResult> similarDocs = [];
            if (input.ActionableRequirements != null && input.ActionableRequirements.Any())
            {
                similarDocs = await _semanticSearchService.SearchByActionableRequirements(input.ActionableRequirements,
                    AgentRole,
                    cancellationToken);
            }

            var userMessage = input.EnrichedUserRequest;

            var inputMessages = new List<AgentMessage>();
            if (similarDocs.Any())
            {
                var apiDocumentation = string.Join("\n\n", similarDocs.Select(d => d.FoundInformation));
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"API Documentation: {apiDocumentation}" });
            }
            else
            {
                _logger.LogInformation("No relevant API documentation found for the given actionable requirements.");
            }
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = userMessage });

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                return new BusinessAdvisorAgentOutput
                {
                    Content = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, BusinessAdvisorAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "BusinessAdvisorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("BusinessAdvisorAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
