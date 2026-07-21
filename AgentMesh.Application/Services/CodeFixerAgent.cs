using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Utils;
using AgentMesh.Models.CodeFixer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class CodeFixerAgent(
        [FromKeyedServices(CodeFixerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<CodeFixerAgent> logger) : AgentBase<string>(logger, CodeFixerAgentConfiguration.AgentName, openAIClient, resilience)
    {
        private readonly Regex JavascriptCodeRegex = new(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly ILogger<CodeFixerAgent> _logger = logger;

        public async Task<CodeFixerAgentOutput> ExecuteAsync(CodeFixerAgentInput input, CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new() {
                    Role = AgentMessageRole.System,
                    Content = "The following issues were detected in the code:\n- " + string.Join("\n- ", input.Issues)
                },
                new() {
                    Role = AgentMessageRole.User,
                    Content = "Fix the following code:\n\n" + input.CodeToFix
                }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new CodeFixerAgentOutput
            {
                FixedCode = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override string ParseStructuredResponse(string rawResponseText)
        {
            var codeRegexMatch = JavascriptCodeRegex.Match(rawResponseText);
            if (!codeRegexMatch.Success)
            {
                throw new BadStructuredResponseException(rawResponseText, "The model's response did not contain any valid JavaScript code block.");
            }

            return codeRegexMatch.Groups["code"].Value.Trim();
        }
    }
}
