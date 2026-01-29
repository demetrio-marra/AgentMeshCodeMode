using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class BusinessAdvisorAgent : IBusinessAdvisorAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<BusinessAdvisorAgent> _logger;
        private readonly string _apiDocumentation;

        public BusinessAdvisorAgent(
            [FromKeyedServices(BusinessAdvisorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessAdvisorAgentConfiguration configuration,
            ILogger<BusinessAdvisorAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _apiDocumentation = configuration.ApiDocumentation;
        }

        public async Task<BusinessAdvisorAgentOutput> ExecuteAsync(
            BusinessAdvisorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing BusinessAdvisorAgent.");
            _logger.LogDebug("BusinessAdvisorAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var userMessage = UserMessageBuilder.BuildUserMessageString(input.RequestContext, input.UserRequest);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"API Documentation: {_apiDocumentation}" },
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = userMessage }
            };

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                return new BusinessAdvisorAgentOutput
                {
                    Content = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, BusinessAdvisorAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug(
                "BusinessAdvisorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("BusinessAdvisorAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
