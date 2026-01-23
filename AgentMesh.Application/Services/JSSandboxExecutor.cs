using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class JSSandboxExecutor : IExecutor<JSSandboxInput, JSSandboxOutput>
    {
        private readonly IJSSandbox _jsSandbox;

        public JSSandboxExecutor(IJSSandbox jSSandbox)
        {
            _jsSandbox = jSSandbox;
        }


        public async Task<JSSandboxOutput> ExecuteAsync(JSSandboxInput input, CancellationToken cancellationToken = default)
        {
            var ret = await _jsSandbox.RunCode(input.AgentId, input.Code);

            return new JSSandboxOutput
            {
                Result = ret
            };
        }
    }
}
