using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Models.PersonalAssistant;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class PersonalAssistantAgent(
        [FromKeyedServices(PersonalAssistantAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<PersonalAssistantAgent> logger) : AgentBase<string>(logger, PersonalAssistantAgentConfiguration.AgentName, openAIClient, resilience), IPersonalAssistantAgent
    {
        public async Task<PersonalAssistantAgentOutput> ExecuteAsync(
            PersonalAssistantAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.System, Content = $"Respond in {input.LanguageOfTheUser}." },
                new() { Role = AgentMessageRole.System, Content = $"Respond about this data:\n" + input.Data },
                new() { Role = AgentMessageRole.User, Content = input.EnrichedUserRequest }
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
