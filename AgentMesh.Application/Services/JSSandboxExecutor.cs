using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class JSSandboxExecutor : IJSSandboxExecutor
    {
        private readonly IJSSandbox _jsSandbox;
        private readonly UserConfiguration _userConfiguration;
        private readonly ILogger<JSSandboxExecutor> _logger;

        public JSSandboxExecutor(IJSSandbox jSSandbox, UserConfiguration userConfiguration, ILogger<JSSandboxExecutor> logger)
        {
            _jsSandbox = jSSandbox;
            _userConfiguration = userConfiguration;
            _logger = logger;
        }


        public async Task<JSSandboxOutput> ExecuteAsync(JSSandboxInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing JSSandboxExecutor.");
            _logger.LogDebug("JSSandboxExecutor Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            var ret = await _jsSandbox.RunCode(_userConfiguration.AgentId, input.Code);

            stopwatch.Stop();

            var output = new JSSandboxOutput
            {
                Result = ret
            };

            _logger.LogDebug("JSSandboxExecutor completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("JSSandboxExecutor Output: {Output}", System.Text.Json.JsonSerializer.Serialize(output));

            return output;
        }
    }
}
