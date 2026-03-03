using AgentMesh.Application.Configuration;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class CoderAgent : ICoderAgent
    {
        private readonly Regex JavascriptCodeRegex = new Regex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<CoderAgent> _logger;
        private readonly IApiDocumentationService _apiDocumentationService;

        public CoderAgent([FromKeyedServices(CoderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                          CoderAgentConfiguration configuration,
                          ILogger<CoderAgent> logger,
                          IApiDocumentationService apiDocumentationService)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _apiDocumentationService = apiDocumentationService;
        }

        public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing CoderAgent.");
            _logger.LogDebug("CoderAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            // Fetch API documentation for mentioned APIs
            var mentionedApis = new HashSet<string>(input.MentionedApis, StringComparer.OrdinalIgnoreCase);
            var apiDocsTasks = await _apiDocumentationService.GetApiDocumentationAsync(mentionedApis);
            _logger.LogDebug("Fetched {CountFound} documentation for {Count} APIs.", apiDocsTasks.Count(), mentionedApis.Count);

            // check if apiDocsTasks contains ALL mentioned APIs, if not log a warning
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
                new AgentMessage { Role = AgentMessageRole.System, Content = "Today date is " + DateTime.UtcNow.ToString("yyyy-MM-dd") + "." },
                new AgentMessage { Role = AgentMessageRole.System, Content = "API Reference:\n" + apiReference },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.BusinessRequirements }
            };

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

                var codeToRun = codeRegexMatch.Groups["code"].Value.Trim();

                return new CoderAgentOutput
                {
                    CodeToRun = codeToRun,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, CoderAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug("CoderAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds, result.TokenCount);

            _logger.LogDebug("CoderAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
