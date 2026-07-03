using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class IntentExtractorAgent(
        [FromKeyedServices(IntentExtractorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<IntentExtractorAgent> logger) : AgentBase<IntentExtractorAgent.ParsedResponse>(logger, IntentExtractorAgentConfiguration.AgentName, openAIClient, resilience), IIntentExtractorAgent
    {
        private readonly ILogger<IntentExtractorAgent> _logger = logger;

        public async Task<IntentExtractorAgentOutput> ExecuteAsync(
            IntentExtractorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userMessage = MessageSerializationUtils.SerializeConversationHistory(input.ContextMessages, input.UserLastRequest);

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = userMessage },
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            var ret = new IntentExtractorAgentOutput
            {
                UserIntent = result.Result.UserIntent,
                Entities = result.Result.Entities,
                Domains = result.Result.Domains,
                SupportingIntentInformation = result.Result.SupportingIntentInformation,
                LanguageOfTheUser = result.Result.LanguageOfTheUser,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount,
                TokenCount = result.TotalTokenCount
            };

            return ret;
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

                if (string.IsNullOrWhiteSpace(responseDTO.UserIntent))
                {
                    _logger.LogWarning("The model's response contains empty user intent. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty user intent.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.LanguageOfTheUser))
                {
                    _logger.LogWarning("The model's response contains empty language of the user. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty language of the user.");
                }

                responseDTO.Entities = responseDTO.Entities
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.Domains = responseDTO.Domains
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.LanguageOfTheUser = responseDTO.LanguageOfTheUser.Trim();

                var legacyDomains = responseDTO.LegacyUserRequestDomains
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (!responseDTO.Domains.Any() && legacyDomains.Any())
                {
                    responseDTO.Domains = legacyDomains;
                }

                responseDTO.SupportingIntentInformation = responseDTO.SupportingIntentInformation
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

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
            [JsonPropertyName("userIntent")]
            public string UserIntent { get; set; } = string.Empty;

            [JsonPropertyName("entities")]
            public IEnumerable<string> Entities { get; set; } = [];

            [JsonPropertyName("domains")]
            public IEnumerable<string> Domains { get; set; } = [];

            [JsonPropertyName("supportingIntentInformation")]
            public IEnumerable<string> SupportingIntentInformation { get; set; } = [];

            [JsonPropertyName("languageOfTheUser")]
            public string LanguageOfTheUser { get; set; } = string.Empty;

            [JsonPropertyName("userRequestDomains")]
            public IEnumerable<string> LegacyUserRequestDomains { get; set; } = [];
        }
    }
}
