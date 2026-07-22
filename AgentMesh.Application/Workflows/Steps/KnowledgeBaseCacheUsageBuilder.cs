using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Application.Contracts;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.QueriesCache;

namespace AgentMesh.Application.Workflows.Steps;

public static class KnowledgeBaseCacheUsageBuilder
{
    public static async Task<AgentTokenUsageEntry?> BuildKnowledgeBaseCacheTokenUsageAsync(
        bool enableCacheService,
        IReadOnlyCollection<KnowledgeBaseQueryInputItem> queries,
        IEnumerable<KnowledgeBaseQueryResultItem> queryResults,
        IQueriesCacheService queriesCacheService)
    {
        var resultsList = queryResults.ToList();
        if (!enableCacheService || !resultsList.Any())
        {
            return null;
        }

        var cacheableQueries = queries
            .Where(entry => entry.SearchType != KnowledgeBaseQuerySearchType.Keyword)
            .ToList();

        if (!cacheableQueries.Any())
        {
            return null;
        }

        var cacheItems = new List<KnowledgeBaseQueriesCacheItem>();
        foreach (var query in cacheableQueries)
        {
            foreach (var result in resultsList)
            {
                cacheItems.Add(new KnowledgeBaseQueriesCacheItem
                {
                    FoundQuery = query.Query,
                    FoundQueryType = query.SearchType.ToString(),
                    DocumentId = result.Id,
                    DocumentFile = result.File,
                    DocumentTitle = result.Title,
                    DocumentSummary = result.Summary
                });
            }
        }

        var cacheUpdateResult = await queriesCacheService.SetKnowledgeBaseCachedItemsAsync(cacheItems);

        return new AgentTokenUsageEntry
        {
            AgentName = "Query Cache Updater Service (Knowledge)",
            InputTokens = cacheUpdateResult.TotalTokens,
            OutputTokens = 0
        };
    }
}

