using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.CodeFixer;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class CodeFixerAgent : AgentBase<string>, ICodeFixerAgent
    {
        private readonly Regex JavascriptCodeRegex = new Regex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly ILogger<CodeFixerAgent> _logger;

        public CodeFixerAgent(
            [FromKeyedServices(CodeFixerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            Resilience resilience,
            ILogger<CodeFixerAgent> logger) : base(logger, CodeFixerAgentConfiguration.AgentName, openAIClient, resilience)
        {
            _logger = logger;
        }

        public async Task<CodeFixerAgentOutput> ExecuteAsync(CodeFixerAgentInput input, CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = "The following issues were detected in the code:\n- " + string.Join("\n- ", input.Issues)
                },
                new AgentMessage
                {
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
