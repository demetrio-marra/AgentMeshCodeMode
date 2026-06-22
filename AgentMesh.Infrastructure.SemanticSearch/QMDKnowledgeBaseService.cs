using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;

namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class QMDKnowledgeBaseService : IKnowledgeBaseService
    {
        private const string KEYWORDS_SEARCH_TYPE = "lex";
        private const string SEMANTIC_SEARCH_TYPE = "vec";

        private readonly QMDHttpProxy _httpProxy;

        public QMDKnowledgeBaseService(QMDHttpProxy httpProxy)
        {
            _httpProxy = httpProxy;
        }


        public async Task<IDictionary<string, string?>> GetKnowledgeBaseEntriesContentAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.MultiGet.MultiGetToolRequest
            {
                Pattern = string.Join(", ", fileNames)
            };

            var ret = await _httpProxy.MultiGetAsync(query, cancellationToken);

            return ret.Files.Where(f => f != null).Select(kv => new KeyValuePair<string, string?>(kv.Uri!, kv.Text)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }


        public async Task<string> GetKnowledgeBaseEntryContentAsync(string id, CancellationToken cancellationToken = default)
        {
            var ret = await _httpProxy.GetAsync(new DTOs.Get.GetToolRequest { File = id }, cancellationToken);
            return ret.Text;
        }


        public async Task<IEnumerable<KnowledgeBaseQueryResult>> KeywordsSearch(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, bool rerank = false, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.Query.QueryToolRequest
            {
                 Searches = searchTerms.Select(term => new DTOs.Query.QuerySubQuery { Type = KEYWORDS_SEARCH_TYPE, Query = term }).ToList(),
                 Collections = collections?.ToList(),
                 Rerank = rerank
            };

            var ret = await _httpProxy.QueryAsync(query, cancellationToken);

            return ret.Results.Select(r => new KnowledgeBaseQueryResult
            {
                Id = r.DocId!,
                Title = r.Title!,
                Summary = r.Snippet,
                File = r.File,
                Relevance = r.Score
            });
        }


        public async Task<IEnumerable<KnowledgeBaseQueryResult>> SemanticSearchAsync(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, bool rerank = true, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.Query.QueryToolRequest
            {
                Searches = searchTerms.Select(term => new DTOs.Query.QuerySubQuery { Type = SEMANTIC_SEARCH_TYPE, Query = term }).ToList(),
                Collections = collections?.ToList(),
                Rerank = rerank
            };

            var ret = await _httpProxy.QueryAsync(query, cancellationToken);

            return ret.Results.Select(r => new KnowledgeBaseQueryResult
            {
                Id = r.DocId!,
                Title = r.Title!,
                Summary = r.Snippet,
                File = r.File,
                Relevance = r.Score
            });
        }


        public async Task<IEnumerable<KnowledgeBaseQueryResult>> FindAsync(IEnumerable<string> searchTerms, IEnumerable<string>? collections = null, CancellationToken cancellationToken = default)
        {
            var searches = searchTerms.Select(term => new DTOs.Query.QuerySubQuery { Type = SEMANTIC_SEARCH_TYPE, Query = term }).ToList();
            searches.AddRange(searchTerms.Select(term => new DTOs.Query.QuerySubQuery { Type = KEYWORDS_SEARCH_TYPE, Query = term }));

            var query = new DTOs.Query.QueryToolRequest
            {
                Searches = searches,
                Collections = collections?.ToList(),
                Rerank = true
            };

            var ret = await _httpProxy.QueryAsync(query, cancellationToken);

            return ret.Results.Select(r => new KnowledgeBaseQueryResult
            {
                Id = r.DocId!,
                Title = r.Title!,
                Summary = r.Snippet,
                File = r.File,
                Relevance = r.Score
            });
        }
    }
}
