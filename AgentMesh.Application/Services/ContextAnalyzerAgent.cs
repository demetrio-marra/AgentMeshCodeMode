using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AgentMesh.Application.Services
{
    public class ContextAnalyzerAgent : IContextAnalyzerAgent
    {
        public const string NO_RELEVANT_CONTEXT_FOUND = "NO RELEVANT CONTEXT FOUND";
        private const string MESSAGES_SEPARATOR = "════════";

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

            var userMessage = BuildUserMessage(input.ContextMessages, input.UserLastRequest);

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

        private string BuildUserMessage(List<ContextMessage> contextMessages, string userLastRequest)
        {
            var sb = new StringBuilder();

            if (contextMessages.Any())
            {
                sb.AppendLine("<conversation_history>");
                
                foreach (var message in contextMessages)
                {
                    var role = message.Role == ContextMessageRole.User ? "User" : "Assistant";
                    sb.AppendLine($"{role} {message.Date:yyyy-MM-ddTHH:mm:ssZ}");
                    sb.AppendLine(message.Text);
                    sb.AppendLine(MESSAGES_SEPARATOR);
                }
                
                sb.AppendLine("</conversation_history>");
            }

            sb.AppendLine($"<user_last_request>");
            sb.AppendLine(userLastRequest);
            sb.AppendLine("</user_last_request>");

            return sb.ToString();
        }
    }
}
