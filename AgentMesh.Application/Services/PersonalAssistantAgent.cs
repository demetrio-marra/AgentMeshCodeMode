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

            var inputJson = JsonSerializer.Serialize(new
            {
                sentence = input.Sentence,
                data = input.Data,
                targetLanguage = input.TargetLanguage
            });

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = inputJson }
            };

            var stopwatch = Stopwatch.StartNew();

            var response = await _openAIClient.GenerateResponseAsync(inputMessages);

            stopwatch.Stop();
            _logger.LogDebug(
                "PersonalAssistantAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                response.TotalTokenCount);

            var responseText = response.Text?.Trim() ?? string.Empty;

            var output = new PersonalAssistantAgentOutput
            {
                Response = responseText,
                TokenCount = response.TotalTokenCount,
                InputTokenCount = response.InputTokenCount,
                OutputTokenCount = response.OutputTokenCount
            };
            _logger.LogDebug("PersonalAssistantAgent Output: {Output}", JsonSerializer.Serialize(output));
            return output;
        }
    }
}
