using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services
{
    public sealed partial class CoderAgent(IOpenAIClientFactory openAIClientFactory,
                      Resilience resilience,
                      ILogger<CoderAgent> logger,
                      IAgentInputSerializer agentInputSerializer) : AbstractAgent<string>(logger,
                          "Coder", 
                          openAIClientFactory, 
                          resilience,
                          agentInputSerializer)
    {
        private readonly Regex JavascriptCodeRegex = JavascriptCodeRegexCompiled();

        private readonly ILogger<CoderAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new () { ParameterName = EWParameterNames.BusinessRequirements, ParameterTags = [] },
                new () { ParameterName = EWParameterNames.TechnicalSpecification, ParameterTags = [] },
                new () { ParameterName = EWParameterNames.KnowledgeBaseAPIDocumentsContent, ParameterTags = [ApplicationConstants.AgentSystemParameterTag] },
                ];
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

        [GeneratedRegex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "it-IT")]
        private static partial Regex JavascriptCodeRegexCompiled();
    }
}
