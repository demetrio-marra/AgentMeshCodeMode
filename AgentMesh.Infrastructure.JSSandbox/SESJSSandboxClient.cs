using AgentMesh.Application.Services;
using System.Net.Http.Json;
using AgentMesh.Infrastructure.JSSandbox.Models;

namespace AgentMesh.Infrastructure.JSSandbox
{
    public class SESJSSandboxClient : IJSSandbox
    {
        private readonly string _sandboxName;
        private readonly HttpClient _httpClient;

        public SESJSSandboxClient(SESJSSandboxConfiguration configuration)
        {
            _sandboxName = configuration.SandboxName;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(configuration.SandboxServiceURL)
            };
        }

        public async Task<string> RunCode(string agentId, string code)
        {
            var request = new CodeExecutionRequestDTO 
            { 
                CodeToRun = code,
                UserAgentId = agentId
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/Execution/{Uri.EscapeDataString(_sandboxName)}", request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CodeSandboxExecutionDTO>();

            if (result is null)
                throw new InvalidOperationException("Received null response from sandbox service.");

            if (result.IsError)
                throw new InvalidOperationException($"Sandbox execution error: {result.ExecutionResult}");

            return result.ExecutionResult;
        }
    }
}
