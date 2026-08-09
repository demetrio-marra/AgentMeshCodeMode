using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentMesh.Models.SemanticSearch
{
    public class SemanticSearchResult
    {
        public string FoundInformation { get; set; } = string.Empty;
        public float Relevance { get; set; }
    }
}
