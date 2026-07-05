using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.RequirementsCollector;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class RequirementsCollectorAgent(
        [FromKeyedServices(RequirementsCollectorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RequirementsCollectorAgent> logger) : AgentBase<RequirementsCollectorAgent.ParsedResponse>(logger, RequirementsCollectorAgentConfiguration.AgentName, openAIClient, resilience), IRequirementsCollectorAgent
    {
        private readonly ILogger<RequirementsCollectorAgent> _logger = logger;

        public async Task<RequirementsCollectorAgentOutput> ExecuteAsync(
            RequirementsCollectorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userMessage = $"""
Captured user intent:
{input.UserIntent}

Supporting intent information:
{string.Join("\n", input.SupportingIntentInformation.Select(info => $"- {info}"))}

User request domains:
{string.Join("\n", input.UserRequestDomains.Select(domain => $"- {domain}"))}
""";

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = userMessage },
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new RequirementsCollectorAgentOutput
            {
                MissingKnowledgeBaseSearchEntries = result.Result.MissingKnowledgeBaseSearchEntries,
                MissingPastMemories = result.Result.MissingPastMemories,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount,
                TokenCount = result.TotalTokenCount
            };
        }

        protected override ParsedResponse ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                responseDTO.MissingPastMemories ??= [];
                responseDTO.MissingKnowledgeBaseSearchEntries ??= [];

                return responseDTO;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        public class ParsedResponse
        {
            [JsonPropertyName("missingPastMemories")]
            public IEnumerable<string> MissingPastMemories { get; set; } = [];

            [JsonPropertyName("missingKnowledgeBaseSearchEntries")]
            public IEnumerable<RequirementsCollectorAgentOutput.RequirementsCollectorKnowledgeBase> MissingKnowledgeBaseSearchEntries { get; set; } = [];
        }
    }
}
