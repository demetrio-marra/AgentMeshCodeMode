using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class IntentExtractorAgent : AgentBase<string>, IIntentExtractorAgent
    {
        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<IntentExtractorAgent> _logger;

        public IntentExtractorAgent(
            [FromKeyedServices(IntentExtractorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            IntentExtractorAgentConfiguration configuration,
            ILogger<IntentExtractorAgent> logger) : base(logger, IntentExtractorAgentConfiguration.AgentName, openAIClient)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<IntentExtractorAgentOutput> ExecuteAsync(
            IntentExtractorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userMessage = MessageSerializationUtils.SerializeConversationHistory(input.ContextMessages, input.UserLastRequest);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = userMessage }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            var ret = new IntentExtractorAgentOutput
            {
                UserIntent = result.Result,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount,
                TokenCount = result.TotalTokenCount
            };

            return ret;
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}
