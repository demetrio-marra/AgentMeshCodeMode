using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class JSSandboxExecutor : IExecutor<JSSandboxInput, JSSandboxOutput>
    {
        private readonly IJSSandbox _jsSandbox;
        private readonly UserConfiguration _userConfiguration;

        public JSSandboxExecutor(IJSSandbox jSSandbox, UserConfiguration userConfiguration)
        {
            _jsSandbox = jSSandbox;
            _userConfiguration = userConfiguration;
        }


        public async Task<JSSandboxOutput> ExecuteAsync(JSSandboxInput input, CancellationToken cancellationToken = default)
        {
            var ret = await _jsSandbox.RunCode(_userConfiguration.AgentId, input.Code);

            return new JSSandboxOutput
            {
                Result = ret
            };
        }
    }
}
