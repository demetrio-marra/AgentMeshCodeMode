using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class ConversationSummarizerAgent : IConversationSummarizerAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<ConversationSummarizerAgent> _logger;
        private readonly string _systemPrompt;

        public ConversationSummarizerAgent([FromKeyedServices(ConversationSummarizerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                                          ConversationSummarizerAgentConfiguration configuration,
                                          ILogger<ConversationSummarizerAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _systemPrompt = configuration.SystemPrompt;
        }

        public async Task<ConversationSummarizerAgentOutput> ExecuteAsync(ConversationSummarizerAgentInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing ConversationSummarizerAgent.");
            _logger.LogDebug("ConversationSummarizerAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = "Today date is " + DateTime.UtcNow.ToString("yyyy-MM-dd") + "." },
                new AgentMessage { Role = AgentMessageRole.System, Content = _systemPrompt },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.ConversationHistory }
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

                return new ConversationSummarizerAgentOutput
                {
                    Summary = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, ConversationSummarizerAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug("ConversationSummarizerAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds, result.TokenCount);

            _logger.LogDebug("ConversationSummarizerAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
