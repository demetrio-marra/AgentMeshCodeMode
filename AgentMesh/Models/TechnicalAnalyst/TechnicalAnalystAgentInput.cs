using System.Collections.Generic;

namespace AgentMesh.Models.TechnicalAnalyst
{
    public class TechnicalAnalystAgentInput
    {
        public string Intent { get; set; } = string.Empty;
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public Dictionary<string, IEnumerable<string>> Entities { get; set; } = new();
        public IEnumerable<string> UserPreferences { get; set; } = [];
        public IEnumerable<string> AgentMemories { get; set; } = [];
        public string KnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string LanguageOfKnowledgeBase { get; set; } = string.Empty;

        /// <summary>
        /// Pre-fetched API documentation produced by <c>ISemanticSearchExecutor</c>.
        /// Empty string when no relevant documentation was found.
        /// </summary>
        public string ApiDocumentation { get; set; } = string.Empty;
    }
}
