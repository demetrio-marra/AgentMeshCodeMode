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
                new() { Role = AgentMessageRole.System, Content = BuildApplicationDomainSystemMessage(input.ApplicationDomainList) },
                new() { Role = AgentMessageRole.User, Content = userMessage },
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            var ret = new IntentExtractorAgentOutput
            {
                OriginalUserRequest = result.Result.OriginalUserRequest,
                UserIntent = result.Result.UserIntent,
                UserIntentCategory = result.Result.UserIntentCategory,
                EntitiesByDomain = result.Result.EntitiesByDomain,
                SupportingIntentInformation = result.Result.SupportingIntentInformation,
                UserPreferences = result.Result.UserPreferences,
                MissingMemories = result.Result.MissingMemories,
                LanguageOfTheUser = result.Result.LanguageOfTheUser,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount,
                TokenCount = result.TotalTokenCount
            };

            return ret;
        }

        private static string BuildApplicationDomainSystemMessage(IEnumerable<string> applicationDomainList)
        {
            var domains = applicationDomainList
                .Where(domain => !string.IsNullOrWhiteSpace(domain))
                .Select(domain => domain.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return domains.Length == 0
                ? "Application domain list available for this request: none."
                : $"Application domain list available for this request: {string.Join(", ", domains)}.";
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

                responseDTO.LanguageOfTheUser = responseDTO.LanguageOfTheUser.Trim();

                responseDTO.EntitiesByDomain = ParseEntitiesByDomain(responseDTO.EntitiesByDomainRaw);

                responseDTO.SupportingIntentInformation = responseDTO.SupportingIntentInformation
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.UserPreferences = responseDTO.UserPreferences
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.MissingMemories = responseDTO.MissingMemories
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.UserIntentCategory = ParseUserIntentCategory(responseDTO.UserIntentCategoryRaw);

                return responseDTO;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private static Dictionary<string, IEnumerable<string>> ParseEntitiesByDomain(
            Dictionary<string, IEnumerable<string>>? entitiesByDomain)
        {
            var result = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

            if (entitiesByDomain != null && entitiesByDomain.Any())
            {
                foreach (var kvp in entitiesByDomain)
                {
                    var domain = kvp.Key?.Trim();
                    if (string.IsNullOrWhiteSpace(domain))
                        continue;

                    var entities = kvp.Value
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Select(e => e.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    if (entities.Any())
                    {
                        result[domain] = entities;
                    }
                }
            }

            return result;
        }

        private static UserIntentCategoryValues ParseUserIntentCategory(string userIntentCategory)
        {
            if (Enum.TryParse<UserIntentCategoryValues>(userIntentCategory, true, out var parsedCategory))
            {
                return parsedCategory;
            }

            throw new BadStructuredResponseException(userIntentCategory, $"Unknown user intent category: {userIntentCategory}");
        }

        public class ParsedResponse
        {
            [JsonPropertyName("originalUserRequest")]
            public string OriginalUserRequest { get; set; } = string.Empty;

            [JsonPropertyName("userIntent")]
            public string UserIntent { get; set; } = string.Empty;

            [JsonPropertyName("userIntentCategory")]
            public string UserIntentCategoryRaw { get; set; } = string.Empty;

            [JsonIgnore]
            public UserIntentCategoryValues UserIntentCategory { get; set; }

            [JsonPropertyName("entitiesByDomain")]
            public Dictionary<string, IEnumerable<string>>? EntitiesByDomainRaw { get; set; }

            [JsonIgnore]
            public Dictionary<string, IEnumerable<string>> EntitiesByDomain { get; set; } = new();

            [JsonPropertyName("supportingIntentInformation")]
            public IEnumerable<string> SupportingIntentInformation { get; set; } = [];

            [JsonPropertyName("userPreferences")]
            public IEnumerable<string> UserPreferences { get; set; } = [];

            [JsonPropertyName("missingMemories")]
            public IEnumerable<string> MissingMemories { get; set; } = [];

            [JsonPropertyName("languageOfTheUser")]
            public string LanguageOfTheUser { get; set; } = string.Empty;

            [JsonPropertyName("userRequestDomains")]
            public IEnumerable<string> LegacyUserRequestDomains { get; set; } = [];
        }
    }
}
