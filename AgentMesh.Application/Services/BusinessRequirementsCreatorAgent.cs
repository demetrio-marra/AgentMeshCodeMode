using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class BusinessRequirementsCreatorAgent : IBusinessRequirementsCreatorAgent
    {
        private static readonly string AgentRole = "BusinessRequirementsCreator";

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<BusinessRequirementsCreatorAgent> _logger;
        private readonly ISemanticSearchService _semanticSearchService;

        public BusinessRequirementsCreatorAgent(
            [FromKeyedServices(BusinessRequirementsCreatorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            BusinessRequirementsCreatorAgentConfiguration configuration,
            ILogger<BusinessRequirementsCreatorAgent> logger,
            ISemanticSearchService semanticSearchService)
        {
            _openAIClient = openAIClient;
            _logger = logger;
            _semanticSearchService = semanticSearchService;
        }

        public async Task<BusinessRequirementsCreatorAgentOutput> ExecuteAsync(
            BusinessRequirementsCreatorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing BusinessRequirementsCreatorAgent.");
            _logger.LogDebug("BusinessRequirementsCreatorAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            IEnumerable<SemanticSearchResult> similarDocs = [];
            if (input.ActionableRequirements != null && input.ActionableRequirements.Any())
            {
                similarDocs = await _semanticSearchService.SearchByActionableRequirements(input.ActionableRequirements,
                    AgentRole,
                    cancellationToken);
            }

            var userMessage = input.EnrichedUserRequest;

            var inputMessages = new List<AgentMessage>();
            if (similarDocs.Any())
            {
                var apiDocumentation = string.Join("\n\n", similarDocs.Select(d => d.FoundInformation));
                inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"API Documentation: {apiDocumentation}" });
            }
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." });
            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = userMessage });

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

                try
                {
                    var responseDTO = JsonSerializer.Deserialize<BusinessRequirementsResponseDTO>(responseText);

                    if (responseDTO == null)
                    {
                        _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", responseText);
                        throw new BadStructuredResponseException(responseText, "The model's response could not be deserialized into the expected format.");
                    }

                    if (string.IsNullOrWhiteSpace(responseDTO.BusinessRequirements))
                    {
                        _logger.LogWarning("The model's response contains empty business requirements. Response text: {ResponseText}", responseText);
                        throw new BadStructuredResponseException(responseText, "The model's response contains empty business requirements.");
                    }

                    if (string.IsNullOrWhiteSpace(responseDTO.BusinessRequirements))
                    {
                        _logger.LogWarning("The model's response is missing the 'businessRequirements' field or it is empty. Response text: {ResponseText}", responseText);
                        throw new BadStructuredResponseException(responseText, "The model's response is missing the 'businessRequirements' field or it is empty.");
                    }

                    return new BusinessRequirementsCreatorAgentOutput
                    {
                        BusinessRequirements = responseDTO.BusinessRequirements,
                        MentionedApis = responseDTO.MentionedApis,
                        TokenCount = response.TotalTokenCount,
                        InputTokenCount = response.InputTokenCount,
                        OutputTokenCount = response.OutputTokenCount
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", responseText);
                    throw new BadStructuredResponseException(responseText, "Failed to parse the model's response.", ex);
                } 

            }, BusinessRequirementsCreatorAgentConfiguration.AgentName, _logger); // polly ends here!

            stopwatch.Stop();
            _logger.LogDebug(
                "BusinessRequirementsCreatorAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds,
                result.TokenCount);

            _logger.LogDebug("BusinessRequirementsCreatorAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }


        private class BusinessRequirementsResponseDTO
        {
            [JsonPropertyName("businessRequirements")]
            public string BusinessRequirements { get; set; } = string.Empty;

            [JsonPropertyName("mentionedApis")]
            public IEnumerable<string> MentionedApis { get; set; } = Enumerable.Empty<string>();
        }
    }
}
