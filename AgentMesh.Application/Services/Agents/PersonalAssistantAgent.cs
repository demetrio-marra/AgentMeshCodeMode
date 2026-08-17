using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class PersonalAssistantAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<PersonalAssistantAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<UserRequestResult>(logger, 
            "PersonalAssistant",
            openAIClientFactory,
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<PersonalAssistantAgent> _logger = logger;


        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new () { ParameterName = RequestDateTimeParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = LanguageOfTheUserParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = RequestRejectedFlagParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = RequestRejectedReasonParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = ExecutionErrorParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = PipelineResultDataParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] }
            ];
        }

        protected override UserRequestResult ParseStructuredResponse(string rawResponseText)
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

                return new UserRequestResult
                {
                    OpeningSentence = responseDTO.OpeningSentence,
                    ClosingSentence = responseDTO.ClosingSentence,
                    ConvenienceErrorSentence = responseDTO.ConvenienceErrorSentence
                };
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

