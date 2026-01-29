using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class ContextAnalyzerAgent : IContextAnalyzerAgent
    {
        public const string NO_RELEVANT_CONTEXT_FOUND = "NO RELEVANT CONTEXT FOUND";

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<ContextAnalyzerAgent> _logger;

        public ContextAnalyzerAgent(
            [FromKeyedServices(ContextAnalyzerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ContextAnalyzerAgentConfiguration configuration,
            ILogger<ContextAnalyzerAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<ContextAnalyzerAgentOutput> ExecuteAsync(
            ContextAnalyzerAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing ContextAnalyzerAgent.");
            _logger.LogDebug("ContextAnalyzerAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var userMessage = MessageSerializationUtils.SerializeConversationHistory(input.ContextMessages, input.UserLastRequest);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = userMessage }
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

                return new ContextAnalyzerAgentOutput
                {
                    RelevantContext = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, ContextAnalyzerAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "ContextAnalyzerAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("ContextAnalyzerAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
