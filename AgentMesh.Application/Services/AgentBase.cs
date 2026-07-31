using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Utils;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentMesh.Application.Services
{
    public abstract class AgentBase<T>(ILogger logger,
        string agentName,
        IOpenAIClient openAIClient,
        Resilience resilience) : IAgent
    {
        private readonly ILogger _logger = logger;
        private readonly string _agentName = agentName;
        private readonly IOpenAIClient _openAIClient = openAIClient;
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


        #region new IAgent implementation


        public async Task<AgentResultRecord> ExecuteAsync(IEnumerable<AgentInputParameterRecord> inputParameters, CancellationToken cancellationToken = default)
        {
            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}."
            };

            foreach (var param in inputParameters.Where(p => p.AsSystemPromptParameter))
            {
                if (param.Value.Contains(Environment.NewLine))
                {
                    systemMessages.Add($"{param.Name}:{Environment.NewLine}{param.Value}");
                }
                else
                {
                    systemMessages.Add($"{param.Name}: {param.Value}");
                }
            }

            var userParameters = inputParameters.Where(p => !p.AsSystemPromptParameter)
                .ToDictionary(p => p.Name, p => p.Value);

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userParameters, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync2(inputMessages, cancellationToken);

            return new AgentResultRecord
            {
                OutputParameters = result.Item1,
                InputTokens = result.Item2,
                OutputTokens = result.Item3
            };
        }


        /// <summary>
        /// Parses the raw response text from the model into a structured format of type T (even plain string).
        /// Can throw specialized exception to trigger retry logic in case of parsing failure, or return default(T) if the response is valid but indicates no actionable result.
        /// </summary>
        /// <param name="inputMessages"></param>
        /// <returns></returns>
        /// <exception cref="EmptyAgentResponseException"></exception>
        private async Task<(IEnumerable<AgentOutputParameterRecord>, int, int)> ExecuteWithRetryAsync2(IEnumerable<AgentMessage> inputMessages, CancellationToken cancellationToken = default)
        {
            var result = await _resilience.AgentRunWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages, cancellationToken);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                var parsedResult = ParseOutputParameters(responseText);

                var ret = new
                {
                    parsedResult,
                    response.InputTokenCount,
                    response.OutputTokenCount
                };
                return ret;

            }, _agentName, _logger);


            return (result.parsedResult, result.InputTokenCount, result.OutputTokenCount);
        }

        protected abstract IEnumerable<AgentOutputParameterRecord> ParseOutputParameters(string rawResponseText);

        protected AgentOutputParameterRecord CreateOutputParameter<K>(string name, K? value)
        {
            if (value is null)
            {
                return new AgentOutputParameterRecord
                {
                    Name = name,
                    Value = null
                };
            }
            return new AgentOutputParameterRecord
            {
                Name = name,
                Value = JsonSerializer.Serialize(value, AgentResponseJsonSerializationUtils.DefaultSerializeOptions)
            };           
        }

        #endregion
    }
}
