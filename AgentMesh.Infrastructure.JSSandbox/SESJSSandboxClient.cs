using System.Net;
using System.Net.Http.Json;
using AgentMesh.Infrastructure.JSSandbox.Models;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Contracts;
using AgentMesh.Models.CodeSandbox;

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

        public async Task<CodeSandboxOutput> RunCode(string agentId, string code)
        {
            var request = new CodeExecutionRequestDTO 
            { 
                CodeToRun = code,
                UserAgentId = agentId
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/Execution/{Uri.EscapeDataString(_sandboxName)}", request);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<SandboxErrorResponseDTO>();

                throw new CodeSandboxCallException(
                    errorResponse?.ErrorType ?? "Unknown",
                    errorResponse?.Error ?? "An unknown error occurred.");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CodeSandboxExecutionDTO>();

            if (result is null)
                throw new InvalidOperationException("Received null response from sandbox service.");

            if (result.IsError)
                throw new InvalidOperationException($"Sandbox execution error: {result.ExecutionResult}");

            return new CodeSandboxOutput
            {
                ExecutionId = result.Id,
                Result = result.ExecutionResult
            };
        }
    }
}
