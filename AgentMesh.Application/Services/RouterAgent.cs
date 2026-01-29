using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class RouterAgent : IRouterAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<RouterAgent> _logger;
        private readonly RouterAgentConfiguration _configuration;

        public RouterAgent(
            [FromKeyedServices(RouterAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            RouterAgentConfiguration configuration,
            ILogger<RouterAgent> logger)
        {
            _openAIClient = openAIClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RouterAgentOutput> ExecuteAsync(
            RouterAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing RouterAgent.");
            _logger.LogDebug("RouterAgent Input: {Input}", JsonSerializer.Serialize(input));

            var inputMessages = new List<AgentMessage>();
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.Message });

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                try
                {
                    if (string.IsNullOrWhiteSpace(responseText))
                    {
                        _logger.LogWarning("The model's response was empty.");
                        throw new EmptyAgentResponseException();
                    }

                    var recipient = responseText;
                    if (_configuration.AllowedRecipients.Count > 0 && !_configuration.AllowedRecipients.Contains(recipient, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("The recipient '{Recipient}' is not in the allowed recipients list. Response: {ResponseText}", recipient, responseText);
                        throw new BadStructuredResponseException(responseText, $"The recipient '{recipient}' is not in the allowed recipients list. Allowed recipients: {string.Join(", ", _configuration.AllowedRecipients)}");
                    }

                    return new RouterAgentOutput
                    {
                        Recipient = recipient,
                        TokenCount = response.TotalTokenCount,
                        InputTokenCount = response.InputTokenCount,
                        OutputTokenCount = response.OutputTokenCount
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse RouterAgent response as JSON. Response: {ResponseText}", responseText);
                    throw new BadStructuredResponseException(responseText, "Failed to parse response as JSON.", ex);
                }
            }, RouterAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "RouterAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("RouterAgent Output: {Output}", JsonSerializer.Serialize(result));
            return result;
        }
    }
}
