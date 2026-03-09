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
    public class CoderAgent : AgentBase<string>, ICoderAgent
    {
        private readonly Regex JavascriptCodeRegex = new Regex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly ILogger<CoderAgent> _logger;
        private readonly IApiDocumentationService _apiDocumentationService;

        public CoderAgent([FromKeyedServices(CoderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                          CoderAgentConfiguration configuration,
                          ILogger<CoderAgent> logger,
                          IApiDocumentationService apiDocumentationService) : base(logger, CoderAgentConfiguration.AgentName, openAIClient)
        {
            _logger = logger;
            _apiDocumentationService = apiDocumentationService;
        }

        public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input, CancellationToken cancellationToken = default)
        {
            var mentionedApis = new HashSet<string>(input.MentionedApis, StringComparer.OrdinalIgnoreCase);
            var apiDocsTasks = await _apiDocumentationService.GetApiDocumentationAsync(mentionedApis);
            _logger.LogDebug("Fetched {CountFound} documentation for {Count} APIs.", apiDocsTasks.Count(), mentionedApis.Count);

            foreach (var api in mentionedApis)
            {
                if (!apiDocsTasks.Any(doc => doc.ApiName.Equals(api, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("API documentation for '{Api}' was not found.", api);
                }
            }

            var apiReference = string.Join("\n\n", apiDocsTasks.Select(doc => $"API: {doc.ApiName}\nDescription: {doc.Documentation}"));

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.System, Content = "API Reference:\n" + apiReference },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.BusinessRequirements }
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
