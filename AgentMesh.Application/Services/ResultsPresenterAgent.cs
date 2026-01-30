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

            var userMessage = MessageSerializationUtils.SerializeRequestAndContext(input.RequestContext, input.UserRequest);
            userMessage = MessageSerializationUtils.AddAdditionalSectionToSerializedMessage(userMessage, "data", input.Data);

            var inputs = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = "Today date is " + DateTime.UtcNow.ToString("yyyy-MM-dd") + "." },
                new AgentMessage { Role = AgentMessageRole.User, Content = userMessage },
            };

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputs);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                return new ResultsPresenterAgentOutput
                {
                    Content = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, ResultsPresenterAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "ResultsPresenterAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            var output = result;
            _logger.LogDebug("ResultsPresenterAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
