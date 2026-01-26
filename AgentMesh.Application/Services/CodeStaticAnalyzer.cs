using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class CodeStaticAnalyzer : ICodeStaticAnalyzer
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ICodeSmellDetector _codeSmellDetector;
        private readonly ILogger<CodeStaticAnalyzer> _logger;

        public CodeStaticAnalyzer(
            [FromKeyedServices(CodeStaticAnalyzerConfiguration.AgentName)] IOpenAIClient openAIClient,
            ICodeSmellDetector codeSmellDetector,
            ILogger<CodeStaticAnalyzer> logger)
        {
            _openAIClient = openAIClient;
            _codeSmellDetector = codeSmellDetector;
            _logger = logger;
        }

        public async Task<CodeStaticAnalyzerOutput> ExecuteAsync(CodeStaticAnalyzerInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing CodeStaticAnalyzer.");

            var violations = new List<string>();

            var codeSmellInput = new CodeSmellDetectorInput
            {
                CodeWithLineNumbers = input.CodeToFix
            };
            var codeSmellOutput = await _codeSmellDetector.DetectCodeSmellsAsync(codeSmellInput);

            if (!codeSmellOutput.Valid)
            {
                violations.AddRange(codeSmellOutput.Feedbacks);
                _logger.LogDebug("CodeSmellDetector found {Count} violations.", codeSmellOutput.Feedbacks.Length);
            }

            //var inputMessages = new List<AgentMessage>
            //{
            //    new AgentMessage
            //    {
            //        Role = AgentMessageRole.User,
            //        Content = input.CodeToFix
            //    }
            //};

            //var stopwatch = Stopwatch.StartNew();

            //var response = await _openAIClient.GenerateResponseAsync(inputMessages);

            //stopwatch.Stop();
            //_logger.LogDebug("CodeStaticAnalyzer LLM completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
            //    stopwatch.ElapsedMilliseconds, response.TotalTokenCount);

            //try
            //{
            //    using var jsonResponse = ParseJsonResponse(response.Text);
            //    var root = jsonResponse.RootElement;

            //    if (root.TryGetProperty("violations", out var violationsElement) && 
            //        violationsElement.ValueKind == JsonValueKind.Array)
            //    {
            //        foreach (var violation in violationsElement.EnumerateArray())
            //        {
            //            if (violation.ValueKind == JsonValueKind.String)
            //            {
            //                var violationText = violation.GetString();
            //                if (!string.IsNullOrWhiteSpace(violationText))
            //                {
            //                    violations.Add(violationText);
            //                }
            //            }
            //        }

            //        _logger.LogDebug("LLM analysis found {Count} additional violations.", 
            //            violationsElement.GetArrayLength());
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogWarning(ex, "Failed to parse LLM response as JSON. Response: {Response}", response.Text);
            //    throw new BadStructuredResponseException(response.Text, 
            //        "The model's response was not valid JSON or did not contain a 'violations' array.");
            //}

            //return new CodeStaticAnalyzerOutput
            //{
            //    Violations = violations,
            //    TokenCount = response.TotalTokenCount,
            //    InputTokenCount = response.InputTokenCount,
            //    OutputTokenCount = response.OutputTokenCount
            //};
            return new CodeStaticAnalyzerOutput
            {
                Violations = violations,
                TokenCount = 0,
                InputTokenCount = 0,
                OutputTokenCount = 0
            };
        }

        private JsonDocument ParseJsonResponse(string responseText)
        {
            var trimmedResponse = responseText.Trim();
            
            if (trimmedResponse.StartsWith("```json"))
            {
                var startIndex = trimmedResponse.IndexOf('\n') + 1;
                var endIndex = trimmedResponse.LastIndexOf("```");
                if (endIndex > startIndex)
                {
                    trimmedResponse = trimmedResponse.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            else if (trimmedResponse.StartsWith("```"))
            {
                var startIndex = trimmedResponse.IndexOf('\n') + 1;
                var endIndex = trimmedResponse.LastIndexOf("```");
                if (endIndex > startIndex)
                {
                    trimmedResponse = trimmedResponse.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            return JsonDocument.Parse(trimmedResponse);
        }
    }
}
