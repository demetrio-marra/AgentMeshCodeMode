using AgentMesh.Application.Contracts;
using AgentMesh.Models.CodeSmellDetector;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public class CodeSmellDetector : ICodeSmellDetector, IExecutor<CodeSmellDetectorInput, CodeSmellDetectorOutput>
    {
        private readonly Regex ResultIssueCodeRegex = new Regex(@"(\?)?\.result(\?)?\.result", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly ILogger<CodeSmellDetector> _logger;
        private readonly IEnumerable<ValidationRule> _validationRules;


        public CodeSmellDetector(ILogger<CodeSmellDetector> logger)
        {
            _logger = logger;
            _validationRules =
            [
                new ValidationRule
                {
                    Pattern = ResultIssueCodeRegex,
                    ErrorMessage = "the 'result' identifier must be dereferenced only once. Not: 'result.result'"
                }
            ];
        }


        public async Task<CodeSmellDetectorOutput> DetectCodeSmellsAsync(CodeSmellDetectorInput input)
        {
            var codeLines = input.CodeWithLineNumbers.Split(Environment.NewLine, StringSplitOptions.None);
            var feedbacks = new List<string>();
            foreach (var rule in _validationRules)
            {
                for (int i = 0; i < codeLines.Length; i++)
                {
                    var line = codeLines[i];
                    if (rule.Pattern.IsMatch(line))
                    {
                        feedbacks.Add($"Line [{i + 1}]: Detected code smell - {rule.ErrorMessage}");
                    }
                }
            }
            var output = new CodeSmellDetectorOutput
            {
                Valid = feedbacks.Count == 0,
                Feedbacks = [.. feedbacks]
            };
            return await Task.FromResult(output);
        }


        public async Task<CodeSmellDetectorOutput> ExecuteAsync(CodeSmellDetectorInput input, CancellationToken cancellationToken = default)
        {
            return await DetectCodeSmellsAsync(input);
        }


        private class ValidationRule
        {
            public Regex Pattern { get; set; } = null!;
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
}
