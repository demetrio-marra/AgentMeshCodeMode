using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class JavascriptCodeExecutionFailuresDetectorAgent : ICodeExecutionFailuresDetectorAgent
    {
        public const string NO_ERROR = "NO_ERROR";

        private static readonly Regex StackTraceRegex = new Regex(
            @"(?i)stacktrace.*?(?:\r?\n\s*at\s+.+)+",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<JavascriptCodeExecutionFailuresDetectorAgent> _logger;

        public JavascriptCodeExecutionFailuresDetectorAgent(
            [FromKeyedServices(CodeExecutionFailuresDetectorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ILogger<JavascriptCodeExecutionFailuresDetectorAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<CodeExecutionFailuresDetectorAgentOutput> ExecuteAsync(CodeExecutionFailuresDetectorAgentInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing JavascriptCodeExecutionFailuresDetectorAgent.");
            _logger.LogDebug("JavascriptCodeExecutionFailuresDetectorAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            var hasStackTrace = StackTraceRegex.IsMatch(input.ExecutionResult);

            string analysis;
            if (hasStackTrace)
            {
                var match = StackTraceRegex.Match(input.ExecutionResult);
                _logger.LogDebug("Stack trace detected in execution result.");
                analysis = $"Error detected:\n\n{match.Value}";
            }
            else
            {
                _logger.LogDebug("No stack trace detected in execution result.");
                analysis = NO_ERROR;
            }

            var result = new CodeExecutionFailuresDetectorAgentOutput
            {
                Analysis = analysis,
                TokenCount = 0,
                InputTokenCount = 0,
                OutputTokenCount = 0
            };

            // Commented out LLM-based detection - currently using regex-only approach
            /*
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage
                {
                    Role = AgentMessageRole.User,
                    Content = $"Source code with line numbers:\n\n{input.CodeWithLineNumbers}\n\nExecution result:\n\n{input.ExecutionResult}"
                }
            };

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                return new CodeExecutionFailuresDetectorAgentOutput
                {
                    Analysis = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, CodeExecutionFailuresDetectorAgentConfiguration.AgentName, _logger);
            */

            stopwatch.Stop();
            _logger.LogDebug("JavascriptCodeExecutionFailuresDetectorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds, result.TokenCount);

            _logger.LogDebug("JavascriptCodeExecutionFailuresDetectorAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
