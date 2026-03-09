using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.BusinessRequirementsCreator;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class BusinessRequirementsCreatorAgent : AgentBase<BusinessRequirementsCreatorAgent.ParsedResponse>, IBusinessRequirementsCreatorAgent
    {
        private readonly ILogger<BusinessRequirementsCreatorAgent> _logger;

        public BusinessRequirementsCreatorAgent(
            [FromKeyedServices(BusinessRequirementsCreatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessRequirementsCreatorAgentConfiguration configuration,
            ILogger<BusinessRequirementsCreatorAgent> logger) : base(logger, BusinessRequirementsCreatorAgentConfiguration.AgentName, openAIClient)
        {
            _logger = logger;
        }

        public async Task<BusinessRequirementsCreatorAgentOutput> ExecuteAsync(
            BusinessRequirementsCreatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>();

            if (!string.IsNullOrWhiteSpace(input.ApiDocumentation))
            {
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"API Documentation: {input.ApiDocumentation}" });
            }

            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest });

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new BusinessRequirementsCreatorAgentOutput
            {
                BusinessRequirements = result.Result.BusinessRequirements,
                MentionedApis = result.Result.MentionedApis,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
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

                if (string.IsNullOrWhiteSpace(responseDTO.BusinessRequirements))
                {
                    _logger.LogWarning("The model's response contains empty business requirements. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty business requirements.");
                }

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
            [JsonPropertyName("businessRequirements")]
            public string BusinessRequirements { get; set; } = string.Empty;

            [JsonPropertyName("mentionedApis")]
            public IEnumerable<string> MentionedApis { get; set; } = Enumerable.Empty<string>();
        }
    }
}
