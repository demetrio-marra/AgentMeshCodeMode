using AgentMesh.Application.Models.ChatClient;
using AgentMesh.Models;

namespace AgentMesh.Application.Models.PersonalAssistant
{
    public class PersonalAssistantAgentOutput : IAgentOutput
    {
        public string? OpeningSentence { get; set; }
        public string? ClosingSentence { get; set; }
        public string? ConvenienceErrorSentence { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Opening sentence", OpeningSentence ?? string.Empty },
                { "Closing sentence", ClosingSentence ?? string.Empty },
                { "Convenience error sentence", ConvenienceErrorSentence ?? string.Empty }
            };
        }
    }
}
