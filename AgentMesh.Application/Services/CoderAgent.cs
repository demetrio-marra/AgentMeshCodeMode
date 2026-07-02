using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.Coder;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class CoderAgent([FromKeyedServices(CoderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                      CoderAgentConfiguration configuration,
                      Resilience resilience,
                      ILogger<CoderAgent> logger) : AgentBase<string>(logger, CoderAgentConfiguration.AgentName, openAIClient, resilience), ICoderAgent
    {
        private readonly Regex JavascriptCodeRegex = new(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly ILogger<CoderAgent> _logger = logger;

        public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input, CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.System, Content = "API Reference:\n" + input.ApiDocumentation },
                new() { Role = AgentMessageRole.User, Content = input.BusinessRequirements }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new CoderAgentOutput
            {
                CodeToRun = result.Result,
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
