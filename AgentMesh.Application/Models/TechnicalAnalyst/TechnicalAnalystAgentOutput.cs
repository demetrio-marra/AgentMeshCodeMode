using AgentMesh.Models;
using AgentMesh.Utils;

namespace AgentMesh.Application.Models.TechnicalAnalyst
{
    public class TechnicalAnalystAgentOutput : IAgentOutput
    {
        public string TechnicalSpecification { get; set; } = string.Empty;
        public required bool RequestRejected { get; set; }
        public string? ReasonOfRejection { get; set; }
        public IEnumerable<string> SelectedAPIsFileLocations { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Technical specification", TechnicalSpecification },
                { "Request rejected", RequestRejected.ToString() },
                { "Reason of rejection", ReasonOfRejection ?? string.Empty },
                { "Selected apis file locations", SelectedAPIsFileLocations.Any() ? ListsFormatter.ToBulletList(SelectedAPIsFileLocations) : "(No selected API files)" }
            };
        }
    }
}
