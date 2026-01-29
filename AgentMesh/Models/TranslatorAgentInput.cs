namespace AgentMesh.Models
{
    public class TranslatorAgentInput
    {
        public string TargetLanguage { get; set; } = string.Empty;
        public string UserRequest { get; set; } = string.Empty;
        public string RequestContext { get; set; } = string.Empty;
    }
}
