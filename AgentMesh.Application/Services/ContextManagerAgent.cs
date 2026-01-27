using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class ContextManagerAgent : ChatManagerAgentBaseClass, IContextManagerAgent
    {
        public const string NO_RELEVANT_CONTEXT_FOUND = "NO RELEVANT CONTEXT FOUND";

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<ContextManagerAgent> _logger;

        public ContextManagerAgent(
            [FromKeyedServices(ChatManagerAgentConfiguration.AgentName)] IOpenAIClient chatManagerOpenAIClient,
            ChatManagerAgentConfiguration chatManagerConfiguration,
            [FromKeyedServices(ContextManagerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ContextManagerAgentConfiguration configuration,
            ILogger<ContextManagerAgent> logger) : base(
                openAIClient,
                chatManagerOpenAIClient,
                chatManagerConfiguration,
                logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<ContextManagerAgentOutput> ExecuteAsync(
            ContextManagerAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing ContextManagerAgent.");
            _logger.LogDebug("ContextManagerAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            // add last message from user at the end of the context
            AddUserMessage(input.UserSentenceText);

            var inputMessages = new List<AgentMessage>();
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.AddRange(Context);

            var stopwatch = Stopwatch.StartNew();

            var response = await GenerateResponseAsync(inputMessages);

            stopwatch.Stop();
            _logger.LogDebug(
                "ContextManagerAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                response.TotalTokenCount);

            var responseText = response.Text?.Trim() ?? string.Empty;

            var output = new ContextManagerAgentOutput
            {
                RelevantContext = responseText,
                TokenCount = response.TotalTokenCount,
                InputTokenCount = response.InputTokenCount,
                OutputTokenCount = response.OutputTokenCount
            };
            _logger.LogDebug("ContextManagerAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(output));
            return output;
        }

        public async Task<ContextManagerAgentState> GetState()
        {
            var result = new ContextManagerAgentState
            {
                ChatHistory = Context.ToList()
            };
            return await Task.FromResult(result);
        }

        public async Task SetState(ContextManagerAgentState state)
        {
            Context.Clear();
            Context.AddRange(state.ChatHistory);
            await Task.CompletedTask;
        }
    }
}
