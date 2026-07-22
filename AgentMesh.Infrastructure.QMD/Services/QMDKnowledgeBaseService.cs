using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Infrastructure.QMD.Services
{
    public class QMDKnowledgeBaseService(QMDHttpProxy httpProxy) : IKnowledgeBaseService
    {
        private const int MAX_QUERIES = 10;

        private const string KEYWORDS_SEARCH_TYPE = "lex";
        private const string SEMANTIC_SEARCH_TYPE = "vec";
        private const string HYPOTHETICAL_SEARCH_TYPE = "hyde";

        private readonly QMDHttpProxy _httpProxy = httpProxy;

        async Task<IEnumerable<KnowledgeBaseDocumentContent>> IKnowledgeBaseService.GetKnowledgeBaseEntriesContentAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken)
        {
            var query = new DTOs.MultiGet.MultiGetToolRequest
            {
                Pattern = string.Join(", ", fileNames)
            };

            var ret = await _httpProxy.MultiGetAsync(query, cancellationToken);

            return [.. ret.Files.Where(f => f != null).Select(kv => new KnowledgeBaseDocumentContent { File = kv.Uri, Content = kv.Text ?? string.Empty })];
        }


        public async Task<KnowledgeBaseQueryResult> KeywordsSearch(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, bool rerank = false, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.Query.QueryToolRequest
            {
                Searches = [.. searchTerms.Take(MAX_QUERIES).Select(term => new DTOs.Query.QuerySubQuery { Type = KEYWORDS_SEARCH_TYPE, Query = term })],
                Collections = collections?.ToList(),
                Rerank = rerank
            };

            var ret = await _httpProxy.QueryAsync(query, cancellationToken);

            return new KnowledgeBaseQueryResult
            {
                Results = [.. ret.Results.Select(r => new KnowledgeBaseQueryResultItem
                {
                    Id = r.DocId!,
                    Title = r.Title!,
                    Summary = r.Snippet,
                    File = r.File,
                    Relevance = r.Score
                })]
            };
        }


        public async Task<KnowledgeBaseQueryResult> SemanticSearchAsync(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, bool rerank = true, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.Query.QueryToolRequest
            {
                Searches = [.. searchTerms.Take(MAX_QUERIES).Select(term => new DTOs.Query.QuerySubQuery { Type = SEMANTIC_SEARCH_TYPE, Query = term })],
                Collections = collections?.ToList(),
                Rerank = rerank
            };

            var ret = await _httpProxy.QueryAsync(query, cancellationToken);

            return new KnowledgeBaseQueryResult
            {
                Results = [.. ret.Results.Select(r => new KnowledgeBaseQueryResultItem
                {
                    Id = r.DocId!,
                    Title = r.Title!,
                    Summary = r.Snippet,
                    File = r.File,
                    Relevance = r.Score
                })]
            };
        }


        public async Task<KnowledgeBaseQueryResult> FindAsync(KnowledgeBaseQueryInput query, bool rerank = true, CancellationToken cancellationToken = default)
        {
            var dbQuery = new DTOs.Query.QueryToolRequest
            {
                Searches = [.. query.Queries.Take(MAX_QUERIES).Select(q => new DTOs.Query.QuerySubQuery
                {
                    Type = q.SearchType == KnowledgeBaseQuerySearchType.Keyword ? KEYWORDS_SEARCH_TYPE : q.SearchType == KnowledgeBaseQuerySearchType.Semantic ? SEMANTIC_SEARCH_TYPE : HYPOTHETICAL_SEARCH_TYPE,
                    Query = q.Query
                })],
                Collections = [.. query.Collections],
                Rerank = rerank,
                Intent = query.UserIntent
            };

            var ret = await _httpProxy.QueryAsync(dbQuery, cancellationToken);

            return new KnowledgeBaseQueryResult
            {
                Results = [.. ret.Results.Select(r => new KnowledgeBaseQueryResultItem
                {
                    Id = r.DocId!,
                    Title = r.Title!,
                    Summary = r.Snippet,
                    File = r.File,
                    Relevance = r.Score
                })]
            };
        }

    }
}
