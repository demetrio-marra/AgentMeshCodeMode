using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Utils;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services.Agents
{
    public abstract class AbstractAgent<T>(ILogger logger,
        string agentName,
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        IAgentInputSerializer agentInputSerializer)
    {
        private readonly IOpenAIClient _openAIClient = openAIClientFactory.CreateOpenAIClient(agentName);

        /// <summary>
        /// Executes the agent's main logic with retry mechanism. It sends the input messages to the OpenAI client, checks for empty responses, and parses the response into a structured format of type T. If the response is empty or if parsing fails (throwing a specialized exception), it triggers the retry logic defined in the Resilience class.
        /// </summary>
        /// <param name="inputParameters">A collection of input parameters that will be serialized and sent to the OpenAI client.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns></returns>
        /// <exception cref="EmptyAgentResponseException"></exception>
        public async Task<AgentResponse<T>> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("Executing {agentName} using Input: {Input}", agentName, System.Text.Json.JsonSerializer.Serialize(inputParameters));

            var inputMessages = agentInputSerializer.SerializeInput(inputParameters, GetAgentInputParameterConfiguration())
                .ToList();

            inputMessages.Insert(0, new AgentMessage
            {
                Role = AgentMessageRole.System,
                Content = $"Current datetime is {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}."
            });

            var stopwatch = Stopwatch.StartNew();

            var result = await resilience.AgentRunWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages, cancellationToken);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    logger.LogWarning("The model's response is empty");
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

            }, agentName, logger);

            stopwatch.Stop();
            logger.LogDebug("{agentName} completed in {ElapsedMilliseconds}ms. Output: {Output}", agentName, 
                stopwatch.ElapsedMilliseconds, System.Text.Json.JsonSerializer.Serialize(result));

            return result;
        }

        /// <summary>
        /// Abstract method that must be implemented by derived classes to provide the configuration for the agent's input parameters. This configuration is used to serialize the input parameters before sending them to the OpenAI client.
        /// </summary>
        /// <returns>A collection of AgentInputParameterConfiguration objects representing the configuration for the agent's input parameters.</returns>
        protected abstract IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration();
        protected abstract T ParseStructuredResponse(string rawResponseText);
    }
}
