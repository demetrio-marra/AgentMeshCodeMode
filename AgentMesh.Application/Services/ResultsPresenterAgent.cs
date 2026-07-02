using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.ResultsPresenter;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class ResultsPresenterAgent(
        [FromKeyedServices(ResultsPresenterAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<ResultsPresenterAgent> logger) : AgentBase<string>(logger, ResultsPresenterAgentConfiguration.AgentName, openAIClient, resilience), IResultsPresenterAgent
    {
        public async Task<ResultsPresenterAgentOutput> ExecuteAsync(
            ResultsPresenterAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.System, Content = "Data to present\n" + input.Data },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest },
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new ResultsPresenterAgentOutput
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
