using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class ContextAggregatorAgent : IContextAggregatorAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<ContextAggregatorAgent> _logger;

        public ContextAggregatorAgent(
            [FromKeyedServices(ContextAggregatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ILogger<ContextAggregatorAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<ContextAggregatorAgentOutput> ExecuteAsync(
            ContextAggregatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing ContextAggregatorAgent.");
            _logger.LogDebug("ContextAggregatorAgent Input: {Input}", JsonSerializer.Serialize(input));

            var inputJson = JsonSerializer.Serialize(new
            {
                lastStatement = input.LastStatement,
                contextualInformation = input.ContextualInformation
            });

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.User, Content = inputJson }
            };

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

                return new ContextAggregatorAgentOutput
                {
                    AggregatedSentence = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, ContextAggregatorAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "ContextAggregatorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("ContextAggregatorAgent Output: {Output}", JsonSerializer.Serialize(result));
            return result;
        }
    }
}
