using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class ResultsPresenterAgent : IResultsPresenterAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<ResultsPresenterAgent> _logger;

        public ResultsPresenterAgent(
            [FromKeyedServices(ResultsPresenterAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ILogger<ResultsPresenterAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<ResultsPresenterAgentOutput> ExecuteAsync(
            ResultsPresenterAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing ResultsPresenterAgent.");
            _logger.LogDebug("ResultsPresenterAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var inputs = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = "Today date is " + DateTime.UtcNow.ToString("yyyy-MM-dd") + "." },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.Content },
            };

            var stopwatch = Stopwatch.StartNew();

            var response = await _openAIClient.GenerateResponseAsync(inputs);

            stopwatch.Stop();
            _logger.LogDebug(
                "ResultsPresenterAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                response.TotalTokenCount);

            var output = new ResultsPresenterAgentOutput
            {
                Content = response.Text,
                TokenCount = response.TotalTokenCount,
                InputTokenCount = response.InputTokenCount,
                OutputTokenCount = response.OutputTokenCount
            };
            _logger.LogDebug("ResultsPresenterAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(output));
            return output;
        }
    }
}
