using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.PersonalAssistant;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class PersonalAssistantAgent : AgentBase<string>, IPersonalAssistantAgent
    {
        public PersonalAssistantAgent(
            [FromKeyedServices(PersonalAssistantAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            ILogger<PersonalAssistantAgent> logger) : base(logger, PersonalAssistantAgentConfiguration.AgentName, openAIClient)
        {
        }

        public async Task<PersonalAssistantAgentOutput> ExecuteAsync(
            PersonalAssistantAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Respond in {input.OutputLanguage}." },
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Respond about this data:\n" + input.Data },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new PersonalAssistantAgentOutput
            {
                Response = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}
