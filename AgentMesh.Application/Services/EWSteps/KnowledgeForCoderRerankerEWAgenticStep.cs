using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.Rerank;
using AgentMesh.Models;
using AgentMesh.Services;
using System.Text;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeForCoderRerankerEWAgenticStep(
        IRerankerService rerankerService,
        KnowledgeQueryForCoderResultParameter knowledgeQueryForCoderResultParameter,
        KnowledgeQueryForCoderParameter knowledgeQueryForCoderParameter) : IEWAgenticStep
    {
        public string Name => "Knowledge For Coder Reranker";

        public string? AgentName => "KnowledgeForCoderReranker";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(KnowledgeQueryForCoderParameter),
            typeof(KnowledgeQueryForCoderResultParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(KnowledgeContentForCoderParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var initialKnowledgeQueryResult = knowledgeQueryForCoderResultParameter.ValueAs(Values[typeof(KnowledgeQueryForCoderResultParameter)]) ?? new KnowledgeQueryResult();
            var contents = initialKnowledgeQueryResult.Contents.ToList();

            var entitiesByContentId = initialKnowledgeQueryResult.Entities
                .Where(entity => !string.IsNullOrWhiteSpace(entity.ContentItem?.Id))
                .GroupBy(entity => entity.ContentItem.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            var relationsByContentId = initialKnowledgeQueryResult.Relations
                .Where(relation => !string.IsNullOrWhiteSpace(relation.ContentItem?.Id))
                .GroupBy(relation => relation.ContentItem.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

            var rearrangedKnowledgeQueryResults = contents
                .Select(content =>
                {
                    var builder = new StringBuilder();
                    builder.AppendLine($"ContentId: {content.Id}");
                    builder.AppendLine($"Source: {content.Source}");
                    builder.AppendLine();

                    builder.AppendLine("Entities:");
                    if (entitiesByContentId.TryGetValue(content.Id, out var contentEntities) && contentEntities.Count > 0)
                    {
                        foreach (var entity in contentEntities)
                        {
                            builder.AppendLine($"- Entity: {entity.Entity}; Type: {entity.Type}; Description: {entity.Description}");
                        }
                    }
                    else
                    {
                        builder.AppendLine("- (none)");
                    }

                    builder.AppendLine();
                    builder.AppendLine("Relations:");
                    if (relationsByContentId.TryGetValue(content.Id, out var contentRelations) && contentRelations.Count > 0)
                    {
                        foreach (var relation in contentRelations)
                        {
                            builder.AppendLine($"- {relation.Description}");
                        }
                    }
                    else
                    {
                        builder.AppendLine("- (none)");
                    }

                    builder.AppendLine();
                    builder.AppendLine("Content: ");
                    builder.AppendLine(content.Content);

                    return builder.ToString();
                })
                .ToList();

            var queryText = knowledgeQueryForCoderParameter.ValueAs(Values[typeof(KnowledgeQueryForCoderParameter)])?.QueryText ?? string.Empty;

            var rerankerInputRequest = new RerankInputQuery
            {
                TopN = 10,
                Query = queryText,
                CandidateDocuments = rearrangedKnowledgeQueryResults
            };

            var rerankResult = await rerankerService.RerankAsync(rerankerInputRequest, cancellationToken);

            var selectedContentIds = rerankResult.RerankedDocuments
                .Select(resultItem => resultItem.DocumentIndex)
                .Where(index => index >= 0 && index < contents.Count)
                .Select(index => contents[index].Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rerankedContents = contents
                .Where(content => selectedContentIds.Contains(content.Id))
                .ToList();

            return new EWAgenticStepExecutionResult
            {
                InputTokens = rerankResult.PromptTokens,
                OutputTokens = rerankResult.CompletionTokens,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(KnowledgeContentForCoderParameter), rerankedContents }
                }
            };
        }
    }
}
