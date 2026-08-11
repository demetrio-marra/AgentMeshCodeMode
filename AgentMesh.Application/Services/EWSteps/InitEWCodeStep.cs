using AgentMesh.Application.Models.CostsAnalysis;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class InitEWCodeStep(
        ConversationContext conversationContext,
        UserLastRequestParameter userLastRequestParameter,
        InitialContextMessagesParameter initialContextMessagesParameter) : IEWCodeStep
    {
        public string Name => "Init";

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!conversationContext.Conversation.Any())
            {
                throw new ArgumentException("Conversation context is empty. Cannot initialize EW code step.");
            }

            IEnumerable<ContextMessage> ic = [];
            var userLastRequest = string.Empty;

            ic = [.. conversationContext.Conversation.Take(conversationContext.Conversation.Count() - 1)];
            userLastRequest = conversationContext.Conversation.Where(m => m.Role == ContextMessageRole.User)
                .Last().Text;

            initialContextMessagesParameter.ParameterValue = [.. ic];
            userLastRequestParameter.ParameterValue = userLastRequest;

            await Task.CompletedTask;
        }
    }
}
