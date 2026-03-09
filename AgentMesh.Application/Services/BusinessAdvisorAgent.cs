using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.BusinessAdvisor;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class BusinessAdvisorAgent : AgentBase<string>, IBusinessAdvisorAgent
    {
        private readonly ILogger<BusinessAdvisorAgent> _logger;
        private readonly ISemanticSearchService _semanticSearchService;

        public BusinessAdvisorAgent(
            [FromKeyedServices(BusinessAdvisorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessAdvisorAgentConfiguration configuration,
            ILogger<BusinessAdvisorAgent> logger,
            ISemanticSearchService semanticSearchService) : base(logger, BusinessAdvisorAgentConfiguration.AgentName, openAIClient)
        {
            _logger = logger;
            _semanticSearchService = semanticSearchService;
        }

        public async Task<BusinessAdvisorAgentOutput> ExecuteAsync(
            BusinessAdvisorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<SemanticSearchResult> similarDocs = [];
            if (input.ActionableRequirements != null && input.ActionableRequirements.Any())
            {
                similarDocs = await _semanticSearchService.SearchByActionableRequirements(input.ActionableRequirements,
                    BusinessAdvisorAgentConfiguration.AgentName,
                    cancellationToken);
            }

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
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest });

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new BusinessAdvisorAgentOutput
            {
                Content = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}
