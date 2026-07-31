using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.AgentMemoryQueryExpander;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Services
{
    public class AgentMemoryQueryExpanderAgent(
        [FromKeyedServices(AgentMemoryQueryExpanderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<AgentMemoryQueryExpanderAgent> logger) : AgentBase<AgentMemoryQueryExpanderAgent.ParsedResponse>(logger, AgentMemoryQueryExpanderAgentConfiguration.AgentName, openAIClient, resilience)
    {
        private readonly ILogger<AgentMemoryQueryExpanderAgent> _logger = logger;

        public async Task<AgentMemoryQueryExpanderAgentOutput> ExecuteAsync(
            AgentMemoryQueryExpanderAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userPayload = new
            {
                memoryTopics = input.MemoryTopics.Select(m => m.Memory)
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new AgentMemoryQueryExpanderAgentOutput
            {
                SearchQueries = result.Result.SearchQueries,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override ParsedResponse ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                responseDTO.SearchQueries ??= [];

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
            [JsonPropertyName("searchQueries")]
            public IEnumerable<string> SearchQueries { get; set; } = [];
        }

        protected override IEnumerable<AgentOutputParameterRecord> ParseOutputParameters(string rawResponseText)
        {
            return [
                CreateOutputParameter(CodeModeWorkflowParametersFactory.PastMemoriesQueryParameterName, ParseStructuredResponse(rawResponseText).SearchQueries),
            ];
        }
    }
}
