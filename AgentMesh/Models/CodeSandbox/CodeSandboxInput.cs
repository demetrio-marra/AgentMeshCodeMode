namespace AgentMesh.Models.CodeSandbox
{
    public class CodeSandboxInput
    {
        public string Code { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Code", Code }
            };
        }
    }
}
