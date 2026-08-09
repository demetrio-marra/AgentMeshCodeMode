using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Agents
{
    public abstract class AgentBase<T>(ILogger logger,
        string agentName,
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience)
    {
        private readonly ILogger _logger = logger;
        private readonly string _agentName = agentName;
        private readonly IOpenAIClient _openAIClient = openAIClientFactory.CreateOpenAIClient(agentName);
        private readonly Resilience _resilience = resilience;

        /// <summary>
        /// Executes the agent's main logic with retry mechanism. It sends the input messages to the OpenAI client, checks for empty responses, and parses the response into a structured format of type T. If the response is empty or if parsing fails (throwing a specialized exception), it triggers the retry logic defined in the Resilience class.
        /// </summary>
        /// <param name="inputMessages"></param>
        /// <returns></returns>
        /// <exception cref="EmptyAgentResponseException"></exception>
        public async Task<AgentResponse<T>> ExecuteWithRetryAsync(IEnumerable<AgentMessage> inputMessages, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing {agentName} using Input: {Input}", _agentName, System.Text.Json.JsonSerializer.Serialize(inputMessages));

            var stopwatch = Stopwatch.StartNew();

            var result = await _resilience.AgentRunWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages, cancellationToken);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                var parsedResult = ParseStructuredResponse(responseText);

                var ret = new AgentResponse<T>
                {
                    Result = parsedResult,
                    TotalTokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
                return ret;

            }, _agentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug("{agentName} completed in {ElapsedMilliseconds}ms. Output: {Output}", _agentName, stopwatch.ElapsedMilliseconds, System.Text.Json.JsonSerializer.Serialize(result));

            return result;
        }

        protected abstract T ParseStructuredResponse(string rawResponseText);
    }
}
