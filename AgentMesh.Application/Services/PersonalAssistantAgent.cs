using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class PersonalAssistantAgent : IPersonalAssistantAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<PersonalAssistantAgent> _logger;

        public PersonalAssistantAgent(
            [FromKeyedServices(PersonalAssistantAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ILogger<PersonalAssistantAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<PersonalAssistantAgentOutput> ExecuteAsync(
            PersonalAssistantAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing PersonalAssistantAgent.");
            _logger.LogDebug("PersonalAssistantAgent Input: {Input}", JsonSerializer.Serialize(input));

            var userMessage = MessageSerializationUtils.SerializeRequestAndContext(input.RequestContext, input.UserRequest);
            userMessage = MessageSerializationUtils.AddAdditionalSectionToSerializedMessage(userMessage, "data", input.Data);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Respond in {input.OutputLanguage}." },
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = userMessage }
            };

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                return new PersonalAssistantAgentOutput
                {
                    Response = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, PersonalAssistantAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "PersonalAssistantAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            var output = result;
            _logger.LogDebug("PersonalAssistantAgent Output: {Output}", JsonSerializer.Serialize(result));
            return result;
        }
    }
}
