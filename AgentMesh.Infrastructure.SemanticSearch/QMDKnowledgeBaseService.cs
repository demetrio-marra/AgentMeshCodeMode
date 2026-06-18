using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;

namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class QMDKnowledgeBaseService : IKnowledgeBaseService
    {
        private const string KeywordsSearchType = "lex";
        private const string SemanticSearchType = "vec";

        private readonly QMDHttpProxy _httpProxy;

        public QMDKnowledgeBaseService(QMDHttpProxy httpProxy)
        {
            _httpProxy = httpProxy;
        }

        public async Task<IDictionary<string, string?>> GetKnowledgeBaseEntriesContentAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.MultiGet.MultiGetToolRequest
            {
                Pattern = string.Join(",", ids)
            };
            var ret = await _httpProxy.MultiGetAsync(query, cancellationToken);
            return ret.Files.Where(f => f != null).Select(kv => new KeyValuePair<string, string?>(kv.File!, kv.Content)).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task<string> GetKnowledgeBaseEntryContentAsync(string id, CancellationToken cancellationToken = default)
        {
            var ret = await _httpProxy.GetAsync(new DTOs.Get.GetToolRequest { File = id }, cancellationToken);
            return ret.Content;
        }

        public async Task<IEnumerable<KnowledgeBaseKeywordsQueryResult>> KeywordsSearch(IEnumerable<string> searchTerms, CancellationToken cancellationToken = default)
        {
            var query = new DTOs.Query.QueryToolRequest
            {
                 Searches = searchTerms.Select(term => new DTOs.Query.QuerySubQuery { Type = KeywordsSearchType, Query = term }).ToList()
            };
            var ret = await _httpProxy.QueryAsync(query, cancellationToken);
            return ret.Results.Select(r => new KnowledgeBaseKeywordsQueryResult
            {
                Id = r.DocId!,
                Title = r.Title!,
                Summary = r.Snippet,
                File = r.File
            });
        }

        public async Task<IEnumerable<KnowledgeBaseKeywordsQueryResult>> SemanticSearchAsync(IEnumerable<string> searchTerms, bool rerank = false, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
