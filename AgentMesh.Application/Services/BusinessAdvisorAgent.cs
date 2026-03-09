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

        public BusinessAdvisorAgent(
            [FromKeyedServices(BusinessAdvisorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessAdvisorAgentConfiguration configuration,
            ILogger<BusinessAdvisorAgent> logger) : base(logger, BusinessAdvisorAgentConfiguration.AgentName, openAIClient)
        {
            _logger = logger;
        }

        public async Task<BusinessAdvisorAgentOutput> ExecuteAsync(
            BusinessAdvisorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>();

            if (!string.IsNullOrWhiteSpace(input.ApiDocumentation))
            {
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"API Documentation: {input.ApiDocumentation}" });
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
