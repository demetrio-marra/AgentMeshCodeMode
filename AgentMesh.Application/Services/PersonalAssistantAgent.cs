using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class PersonalAssistantAgent : ChatManagerAgentBaseClass, IPersonalAssistantAgent
    {
        private static readonly Regex ResponseRegex = new("```(?<responseType>(userResponse|businessAnalyst))\\s*(?<content>[\\s\\S]+)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<PersonalAssistantAgent> _logger;

        public PersonalAssistantAgent(
            [FromKeyedServices(ChatManagerAgentConfiguration.AgentName)] IOpenAIClient chatManagerOpenAIClient,
            ChatManagerAgentConfiguration chatManagerConfiguration,
            [FromKeyedServices(PersonalAssistantAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            PersonalAssistantAgentConfiguration configuration,
            ILogger<PersonalAssistantAgent> logger) : base(
                openAIClient,
                chatManagerOpenAIClient,
                chatManagerConfiguration,
                logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<PersonalAssistantAgentOutput> ExecuteAsync(
            PersonalAssistantAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing PersonalAssistantAgent.");

            // add last message from user at the end of the context
            AddUserMessage(input.UserQuestionText);

            var inputMessages = new List<AgentMessage>();
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.AddRange(Context);

            var stopwatch = Stopwatch.StartNew();

            var response = await GenerateResponseAsync(inputMessages);

            stopwatch.Stop();
            _logger.LogDebug(
                "PersonalAssistantAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                response.TotalTokenCount);

            var responseText = response.Text?.Trim() ?? string.Empty;

            var match = ResponseRegex.Match(responseText);
            if (!match.Success)
            {
                _logger.LogWarning("The model's response did not match the expected format. Response: {ResponseText}", responseText);
                throw new BadStructuredResponseException(responseText, "The model's response did not match the expected format.");
            }

            var responseType = match.Groups["responseType"].Value.Trim().ToLowerInvariant();
            if (responseType == "userresponse")
            {
                return new PersonalAssistantAgentOutput
                {
                    ResponseText = match.Groups["content"].Value.Trim(),
                    EngageBusinessAnalyst = false,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }

            return new PersonalAssistantAgentOutput
            {
                ResponseText = match.Groups["content"].Value.Trim(),
                EngageBusinessAnalyst = true,
                TokenCount = response.TotalTokenCount,
                InputTokenCount = response.InputTokenCount,
                OutputTokenCount = response.OutputTokenCount
            };
        }
    }
}
