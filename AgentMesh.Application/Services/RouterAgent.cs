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

        public RouterAgent(
            [FromKeyedServices(RouterAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            RouterAgentConfiguration configuration,
            ILogger<RouterAgent> logger)
        {
            _openAIClient = openAIClient;
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

            var response = await _openAIClient.GenerateResponseAsync(inputMessages);

            stopwatch.Stop();
            _logger.LogDebug(
                "RouterAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                response.TotalTokenCount);

            var responseText = response.Text?.Trim() ?? string.Empty;

            try
            {
                var jsonResponse = JsonSerializer.Deserialize<RouterResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (jsonResponse == null || string.IsNullOrWhiteSpace(jsonResponse.Recipient))
                {
                    _logger.LogWarning("The model's response did not contain a valid recipient. Response: {ResponseText}", responseText);
                    throw new BadStructuredResponseException(responseText, "The model's response did not contain a valid recipient.");
                }

                var output = new RouterAgentOutput
                {
                    Recipient = jsonResponse.Recipient.Trim(),
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
                _logger.LogDebug("RouterAgent Output: {Output}", JsonSerializer.Serialize(output));
                return output;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse RouterAgent response as JSON. Response: {ResponseText}", responseText);
                throw new BadStructuredResponseException(responseText, "Failed to parse response as JSON.", ex);
            }
        }

        private class RouterResponse
        {
            public string Recipient { get; set; } = string.Empty;
        }
    }
}
