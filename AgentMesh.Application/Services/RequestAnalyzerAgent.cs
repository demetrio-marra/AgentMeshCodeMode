
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.RequestAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class RequestAnalyzerAgent(
        [FromKeyedServices(RequestAnalyzerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RequestAnalyzerAgent> logger) : AgentBase<RequestAnalyzerAgent.ParsedResponse>(logger, RequestAnalyzerAgentConfiguration.AgentName, openAIClient, resilience)
    {
        public const string AgentName = RequestAnalyzerAgentConfiguration.AgentName;
        private readonly ILogger<RequestAnalyzerAgent> _logger = logger;

        public async Task<RequestAnalyzerAgentOutput> ExecuteAsync(
            RequestAnalyzerAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userMessage = MessageSerializationUtils.SerializeConversationHistory(input.ContextMessages, input.UserLastRequest);

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = userMessage },
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            var ret = new RequestAnalyzerAgentOutput
            {
                Intent = result.Result.Intent,
                IntentCategory = result.Result.IntentCategory,
                ConversationTopic = result.Result.ConversationTopic,
                UserRequestedActions = result.Result.UserRequestedActions,
                UserPreferences = result.Result.UserPreferences,
                UserProvidedData = result.Result.UserProvidedData,
                MissingValues = result.Result.MissingValues,
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
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.Intent))
                {
                    _logger.LogWarning("The model's response contains empty intent. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty intent.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.IntentCategoryRaw))
                {
                    _logger.LogWarning("The model's response contains empty intent category. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty intent category.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.LanguageOfTheUser))
                {
                    _logger.LogWarning("The model's response contains empty language of the user. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty language of the user.");
                }

                responseDTO.LanguageOfTheUser = responseDTO.LanguageOfTheUser.Trim();
                responseDTO.Intent = responseDTO.Intent.Trim();
                responseDTO.IntentCategory = ParseIntentCategory(responseDTO.IntentCategoryRaw);
                responseDTO.ConversationTopic = responseDTO.ConversationTopic?.Trim() ?? string.Empty;

                responseDTO.UserRequestedActions = responseDTO.UserRequestedActions
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.UserPreferences = responseDTO.UserPreferences
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.UserProvidedData = responseDTO.UserProvidedData
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.MissingValues = responseDTO.MissingValues
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

        private static UserIntentCategory ParseIntentCategory(string intentCategory)
        {
            if (Enum.TryParse<UserIntentCategory>(intentCategory, true, out var parsedIntentCategory))
            {
                return parsedIntentCategory;
            }

            throw new BadStructuredResponseException(intentCategory, $"Unknown intent category: {intentCategory}");
        }

        public class ParsedResponse
        {
            [JsonPropertyName("intent")]
            public string Intent { get; set; } = string.Empty;

            [JsonPropertyName("intentCategory")]
            public string IntentCategoryRaw { get; set; } = string.Empty;

            [JsonIgnore]
            public UserIntentCategory IntentCategory { get; set; }

            [JsonPropertyName("conversationTopic")]
            public string ConversationTopic { get; set; } = string.Empty;

            [JsonPropertyName("userRequestedActions")]
            public IEnumerable<string> UserRequestedActions { get; set; } = [];

            [JsonPropertyName("userPreferences")]
            public IEnumerable<string> UserPreferences { get; set; } = [];

            [JsonPropertyName("userProvidedData")]
            public IEnumerable<string> UserProvidedData { get; set; } = [];

            [JsonPropertyName("missingValues")]
            public IEnumerable<string> MissingValues { get; set; } = [];

            [JsonPropertyName("languageOfTheUser")]
            public string LanguageOfTheUser { get; set; } = string.Empty;
        }
    }
}
