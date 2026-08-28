using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Knowledge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentMesh.Infrastructure.LightRag.Services
{
    public class LightRagKnowledgeService : IKnowledgeService
    {
        public async Task<KnowledgeQueryResult> QueryKnowledgeAsync(KnowledgeQuery query)
        {
            throw new NotImplementedException();
        }
    }
}
