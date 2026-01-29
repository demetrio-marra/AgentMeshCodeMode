using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class BusinessRequirementsCreatorAgent : IBusinessRequirementsCreatorAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<BusinessRequirementsCreatorAgent> _logger;
        private readonly string _apiDocumentation;

        public BusinessRequirementsCreatorAgent(
            [FromKeyedServices(BusinessRequirementsCreatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessRequirementsCreatorAgentConfiguration configuration,
            ILogger<BusinessRequirementsCreatorAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _apiDocumentation = configuration.ApiDocumentation;
        }

        public async Task<BusinessRequirementsCreatorAgentOutput> ExecuteAsync(
            BusinessRequirementsCreatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing BusinessRequirementsCreatorAgent.");
            _logger.LogDebug("BusinessRequirementsCreatorAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var inputMessages = new List<AgentMessage>();
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"API Documentation: {_apiDocumentation}" });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = input.UserQuestionText });

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response was empty");
                    throw new EmptyAgentResponseException();
                }

                return new BusinessRequirementsCreatorAgentOutput
                {
                    BusinessRequirements = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, BusinessRequirementsCreatorAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "BusinessRequirementsCreatorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("BusinessRequirementsCreatorAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
