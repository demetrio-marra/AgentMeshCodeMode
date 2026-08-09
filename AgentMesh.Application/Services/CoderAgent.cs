using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.Coder;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.KnowledgeBase;

namespace AgentMesh.Application.Services
{
    public sealed partial class CoderAgent(IOpenAIClientFactory openAIClientFactory,
                      Resilience resilience,
                      ILogger<CoderAgent> logger) : AgentBase<string>(logger,
                          "Coder", 
                          openAIClientFactory, 
                          resilience)
    {
        private readonly Regex JavascriptCodeRegex = JavascriptCodeRegexCompiled();

        private readonly ILogger<CoderAgent> _logger = logger;

        public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input, CancellationToken cancellationToken = default)
        {
            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}."
            };

            // Filter documents based on selected APIs
            var filteredDocuments = input.SelectedAPIsFileLocations.Any()
                ? input.KnowledgeBaseAPIDocumentsContent
                    .Where(doc => input.SelectedAPIsFileLocations.Contains(doc.File, StringComparer.OrdinalIgnoreCase))
                    .ToList()
                : [];

            if (filteredDocuments.Any())
            {
                systemMessages.Add($"Filtered knowledge base API documents:\n{FormatKnowledgeBaseDocumentsContent(filteredDocuments)}");
            }

            var userPayload = new
            {
                input.BusinessRequirements,
                input.TechnicalSpecification,
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
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

        private static string FormatKnowledgeBaseDocumentsContent(IEnumerable<KnowledgeBaseGetDocsOutputItem> documents)
        {
            return string.Join("\n\n", documents.Select(d => $"- File: {d.File ?? "(No file)"}\n  Content: {d.Content}"));
        }

        [GeneratedRegex(@"```\s*javascript\s*(?<code>(?:(?!```)[\s\S])*)\s*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "it-IT")]
        private static partial Regex JavascriptCodeRegexCompiled();
    }
}
