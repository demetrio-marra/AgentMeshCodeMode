using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class CodeFixerAgent : ICodeFixerAgent
    {
        private readonly Regex JavascriptCodeRegex = new Regex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<CodeFixerAgent> _logger;

        public CodeFixerAgent(
            [FromKeyedServices(CodeFixerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ILogger<CodeFixerAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<CodeFixerAgentOutput> ExecuteAsync(CodeFixerAgentInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing CodeFixerAgent.");
            _logger.LogDebug("CodeFixerAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var inputMessages = new List<AgentMessage>();

            inputMessages.Add(new AgentMessage
            {
                Role = AgentMessageRole.System,
                Content = "The following issues were detected in the code:\n- " + string.Join("\n- ", input.Issues)
            });

            inputMessages.Add(new AgentMessage
            {
                Role = AgentMessageRole.User,
                Content = "Fix the following code:\n\n" + input.CodeToFix
            });

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

                var codeRegexMatch = JavascriptCodeRegex.Match(responseText);
                if (!codeRegexMatch.Success)
                {
                    throw new BadStructuredResponseException(responseText, "The model's response did not contain any valid JavaScript code block.");
                }

                var fixedCode = codeRegexMatch.Groups["code"].Value.Trim();

                return new CodeFixerAgentOutput
                {
                    FixedCode = fixedCode,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, CodeFixerAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug("CodeFixerAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds, result.TokenCount);

            _logger.LogDebug("CodeFixerAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
