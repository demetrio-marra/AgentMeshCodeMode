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
        private readonly string _apiReference;

        public CoderAgent([FromKeyedServices(CoderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                          CoderAgentConfiguration configuration,
                          ILogger<CoderAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _apiReference = configuration.ApiReference;
        }

        public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing CoderAgent.");
            _logger.LogDebug("CoderAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = "Today date is " + DateTime.UtcNow.ToString("yyyy-MM-dd") + "." },
                new AgentMessage { Role = AgentMessageRole.System, Content = "API Reference:\n" + _apiReference },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.BusinessRequirements }
            };

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);

                var codeRegexMatch = JavascriptCodeRegex.Match(response.Text);
                if (!codeRegexMatch.Success)
                {
                    throw new BadStructuredResponseException(response.Text, "The model's response did not contain any valid JavaScript code block.");
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
