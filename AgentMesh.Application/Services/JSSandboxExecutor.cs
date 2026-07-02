using AgentMesh.Application.Contracts;
using AgentMesh.Models.CodeSandbox;
using AgentMesh.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class JSSandboxExecutor(IJSSandbox jSSandbox, UserConfiguration userConfiguration, ILogger<JSSandboxExecutor> logger) : IJSSandboxExecutor
    {
        private readonly IJSSandbox _jsSandbox = jSSandbox;
        private readonly UserConfiguration _userConfiguration = userConfiguration;
        private readonly ILogger<JSSandboxExecutor> _logger = logger;

        public async Task<CodeSandboxOutput> ExecuteAsync(CodeSandboxInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing JSSandboxExecutor.");
            _logger.LogDebug("JSSandboxExecutor Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var stopwatch = Stopwatch.StartNew();

            var executionResult = await _jsSandbox.RunCode(_userConfiguration.AgentId, input.Code);

            stopwatch.Stop();

            _logger.LogDebug("JSSandboxExecutor completed in {ElapsedMilliseconds}ms.", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("JSSandboxExecutor Output: {Output}", System.Text.Json.JsonSerializer.Serialize(executionResult));

            return executionResult;
        }
    }
}
