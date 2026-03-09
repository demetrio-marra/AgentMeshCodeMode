using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.Router;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class RouterAgent : AgentBase<(string Recipient, string Rationale)>, IRouterAgent
    {
        private readonly ILogger<RouterAgent> _logger;
        private readonly RouterAgentConfiguration _configuration;

        public RouterAgent(
            [FromKeyedServices(RouterAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            RouterAgentConfiguration configuration,
            ILogger<RouterAgent> logger) : base(logger, RouterAgentConfiguration.AgentName, openAIClient)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RouterAgentOutput> ExecuteAsync(
            RouterAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new RouterAgentOutput
            {
                Recipient = result.Result.Recipient,
                Rationale = result.Result.Rationale,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override (string Recipient, string Rationale) ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var recipient = string.Empty;
                var rationale = string.Empty;

                var lines = rawResponseText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("Recipient:", StringComparison.OrdinalIgnoreCase))
                    {
                        recipient = line.Substring("Recipient:".Length).Trim();
                    }
                    else if (line.StartsWith("Rationale:", StringComparison.OrdinalIgnoreCase))
                    {
                        rationale = line.Substring("Rationale:".Length).Trim();
                    }
                }

                if (string.IsNullOrWhiteSpace(recipient))
                {
                    recipient = rawResponseText.Trim();
                }

                if (_configuration.AllowedRecipients.Count > 0 && !_configuration.AllowedRecipients.Contains(recipient, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("The recipient '{Recipient}' is not in the allowed recipients list. Response: {ResponseText}", recipient, rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, $"The recipient '{recipient}' is not in the allowed recipients list. Allowed recipients: {string.Join(", ", _configuration.AllowedRecipients)}");
                }

                return (recipient, rationale);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse RouterAgent response as JSON. Response: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse response as JSON.", ex);
            }
        }
    }
}
