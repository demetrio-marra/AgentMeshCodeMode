using AgentMesh.Application.Services;

namespace AgentMesh.Infrastructure.JSSandbox
{
    public class SESJSSandboxClient : IJSSandbox
    {
        private readonly SESJSSandboxConfiguration _configuration;

        public SESJSSandboxClient(SESJSSandboxConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> RunCode(string agentId, string code)
        {
            throw new NotImplementedException();
        }
    }
}
