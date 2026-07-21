using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.Coder;
using AgentMesh.Models.KnowledgeBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace AgentMesh.Application.Services
{
    public partial class CoderAgent([FromKeyedServices(CoderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                      Resilience resilience,
                      ILogger<CoderAgent> logger) : AgentBase<string>(logger, CoderAgentConfiguration.AgentName, openAIClient, resilience)
    {
        private readonly Regex JavascriptCodeRegex = JavascriptCodeRegexCompiled();

        private readonly ILogger<CoderAgent> _logger = logger;

        public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input, CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.System, Content = $"Business Requirements:\n{input.BusinessRequirements}" }
            };

            if (!string.IsNullOrWhiteSpace(input.TechnicalSpecification))
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"Technical Specification:\n{input.TechnicalSpecification}"
                });
            }

            var knowledgeBaseDocumentsContent = FormatKnowledgeBaseDocumentsContent(input.KnowledgeBaseAPIDocumentsContent);
            if (!string.IsNullOrWhiteSpace(knowledgeBaseDocumentsContent))
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"Knowledge Base API Documents:\n{knowledgeBaseDocumentsContent}"
                });
            }

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

        private static string FormatKnowledgeBaseDocumentsContent(IEnumerable<KnowledgeBaseGetDocsOutputItem> documents)
        {
            return string.Join("\n\n", documents.Select(d => $"- File: {d.File ?? "(No file)"}\n  Content: {d.Content}"));
        }

        [GeneratedRegex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "it-IT")]
        private static partial Regex JavascriptCodeRegexCompiled();
    }
}
