using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class IntentExtractorAgent : IIntentExtractorAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<IntentExtractorAgent> _logger;

        public IntentExtractorAgent(
            [FromKeyedServices(IntentExtractorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            IntentExtractorAgentConfiguration configuration,
            ILogger<IntentExtractorAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<IntentExtractorAgentOutput> ExecuteAsync(
            IntentExtractorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing IntentExtractorAgent.");
            _logger.LogDebug("IntentExtractorAgent Input: {Input}", JsonSerializer.Serialize(input));

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

                return new IntentExtractorAgentOutput
                {
                    Query = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, IntentExtractorAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "IntentExtractorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("IntentExtractorAgent Output: {Output}", JsonSerializer.Serialize(result));
            return result;
        }
    }
}
