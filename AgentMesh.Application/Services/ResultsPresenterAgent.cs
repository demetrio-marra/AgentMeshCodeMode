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
            var requestContext = string.Join(Environment.NewLine + Environment.NewLine, new[]
            {
                $"Original user request:\n{input.OriginalUserRequest}",
                $"Canonicalized intent:\n{input.CanonicalizedIntent}",
                $"Supporting intent information:\n{string.Join(Environment.NewLine, input.SupportingIntentInformation.Select(item => $"- {item}"))}",
                $"User preferences:\n{string.Join(Environment.NewLine, input.UserPreferences.Select(item => $"- {item}"))}"
            });

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.System, Content = "Data to present\n" + input.Data },
                new() { Role = AgentMessageRole.User, Content = requestContext },
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

