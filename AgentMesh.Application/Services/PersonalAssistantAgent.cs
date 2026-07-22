using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Utils;
using AgentMesh.Models.PersonalAssistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class PersonalAssistantAgent(
        [FromKeyedServices(PersonalAssistantAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<PersonalAssistantAgent> logger) : AgentBase<PersonalAssistantAgent.ParsedResponse>(logger, PersonalAssistantAgentConfiguration.AgentName, openAIClient, resilience)
    {
        private readonly ILogger<PersonalAssistantAgent> _logger = logger;

        public async Task<PersonalAssistantAgentOutput> ExecuteAsync(
            PersonalAssistantAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}.",
                $"Respond in {input.LanguageOfTheUser}.",
                $"The request " + (input.RequestFailed ? "failed" : "succeeded") + (string.IsNullOrWhiteSpace(input.RequestFailureReason) ? "." : $" with reason: {input.RequestFailureReason}."),
            };

            if (!string.IsNullOrWhiteSpace(input.Data))
            {
                systemMessages.Add($"The request data is:\n{input.Data}");
            }

            var userPayload = new
            {
                input.CanonicalizedIntent,
                input.ConversationTopic,
                input.UserRequestedActions,
                input.UserProvidedData,
                input.UserPreferences,
                Memories = input.Memories
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new PersonalAssistantAgentOutput
            {
                OpeningSentence = result.Result.OpeningSentence,
                ClosingSentence = result.Result.ClosingSentence,
                ConvenienceErrorSentence = result.Result.ConvenienceErrorSentence,
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

                // per decommentare dovremmo far arrivare in questa funzione il parametro di input RequestFailed, e poi fare un check su quello, ma non è chiaro se sia il caso di farlo qui o in un altro punto del flusso
                //if (responseDTO.IsDataAnActualError && string.IsNullOrWhiteSpace(responseDTO.ConvenienceErrorSentence))
                //{
                //    _logger.LogWarning("The model's response signals an error but contains no convenienceErrorSentence. Response text: {ResponseText}", rawResponseText);
                //    throw new BadStructuredResponseException(rawResponseText, "The model's response signals an error but contains no convenienceErrorSentence.");
                //}

                //if (!responseDTO.IsDataAnActualError &&
                //    (string.IsNullOrWhiteSpace(responseDTO.OpeningSentence) || string.IsNullOrWhiteSpace(responseDTO.ClosingSentence)))
                //{
                //    _logger.LogWarning("The model's response is missing openingSentence or closingSentence. Response text: {ResponseText}", rawResponseText);
                //    throw new BadStructuredResponseException(rawResponseText, "The model's response is missing openingSentence or closingSentence.");
                //}

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

            [JsonPropertyName("openingSentence")]
            public string? OpeningSentence { get; set; }

            [JsonPropertyName("closingSentence")]
            public string? ClosingSentence { get; set; }

            [JsonPropertyName("convenienceErrorSentence")]
            public string? ConvenienceErrorSentence { get; set; }
        }
    }
}

