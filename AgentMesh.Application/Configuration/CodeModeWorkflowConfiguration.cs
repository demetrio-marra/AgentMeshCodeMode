namespace AgentMesh.Application.Configuration
{
    public class CodeModeWorkflowConfiguration
    {
        public const string SectionName = "CodeModeWorkflow";

        public bool EnableCacheService { get; set; } = true;
        public bool EnableMemoryService { get; set; } = true;
        public bool EnableCodeCorrection { get; set; } = true;
        public bool EnableDomainExpert { get; set; } = true;
        public bool RerankOnHybridSearch { get; set; } = true;
        public IEnumerable<string> ApplicationDomainList { get; set; } = [];
        public string LanguageOfKnowledgeBase { get; set; } = string.Empty;
    }
}
