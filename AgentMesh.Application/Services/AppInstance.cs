using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Models.Costs;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Application.Services
{
    /// <summary>
    /// This class is the API layer for the application. It is responsible for managing the conversation context and processing user requests. It uses dependency injection to access the necessary services and maintains the state of the conversation.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="conversationContext"></param>
    public class AppInstance(IServiceProvider serviceProvider,
        ConversationContext conversationContext,
        IEnumerable<AgentFlatConfigurationRecord> agentsConfigurations,
        ConversationSummarizerAgentConfiguration conversationSummarizerConfiguration)
    {
        public int CountOfMessages { get => conversationContext.Conversation.Count(); }
        public int CountOfTokensInContext { get => conversationContext.TokensCount; }

        public async Task InitConversation()
        {
            conversationContext.TokensCount = 0;
            conversationContext.Conversation = [];

            await Task.CompletedTask;
        }


        public async Task<WorkflowResult> ProcessRequest(string message, CancellationToken cancellationToken)
        {
            var requestDatetime = DateTime.UtcNow;

            conversationContext.Conversation = conversationContext.Conversation.Append(new()
            {
                Role = ContextMessageRole.User,
                Date = requestDatetime,
                Text = message,
            });

            var executionScope = serviceProvider.CreateScope();
            var pipeline = executionScope.ServiceProvider.GetRequiredService<EWPipeline>();

            var stepsStats = await pipeline.ExecuteAsync(cancellationToken);
            var usageStatistics = stepsStats.ToList();

            var parameters = executionScope.ServiceProvider.GetRequiredService<IEnumerable<IEWParameter>>();

            var answerDateTime = DateTime.UtcNow;
            var answerText = parameters.First(p => p.IsResponseForUserParameter)?.GetDisplayValue() ?? string.Empty;

            conversationContext.Conversation = conversationContext.Conversation.Append(new()
            {
                Role = ContextMessageRole.Assistant,
                Date = answerDateTime,
                Text = answerText,
            });

            var fistAgenticStepStatistics = stepsStats.FirstOrDefault(s => s.IsFirstAgenticStep);
            var lastAgenticStepStatistics = stepsStats.LastOrDefault(s => s.IsLastAgenticStep);
            

            var inputTokens = fistAgenticStepStatistics.InputTokens ?? 0;
            var outputTokens = lastAgenticStepStatistics.OutputTokens ?? 0;

            conversationContext.TokensCount = inputTokens + outputTokens;

            bool summarizerHasRun = false;

            int? countOfMessagesBeforeSummarization = null;
            int? countOfTokensBeforeSummarization = null;

            if (conversationContext.TokensCount >= conversationSummarizerConfiguration.SummaryTokenThreshold)
            {
                countOfMessagesBeforeSummarization = conversationContext.Conversation.Count();
                countOfTokensBeforeSummarization = conversationContext.TokensCount;

                var memoryConversation = conversationContext.Conversation.ToList();
                var summarizerConversation = conversationContext.Conversation.ToList();

                var initSummarizationStep = executionScope.ServiceProvider.GetRequiredService<InitSummarizationEWCodeStep>();
                await initSummarizationStep.ExecuteAsync(cancellationToken);

                var summarizationStep = executionScope.ServiceProvider.GetRequiredService<ConversationSummarizerEWAgenticStep>();
                var relevantFactsEvaluatorStep = executionScope.ServiceProvider.GetRequiredService<RelevantFactsEvaluatorEWAgenticStep>();

                var summarizerTask = summarizationStep.ExecuteAsync(cancellationToken);
                var relevantFactsEvaluatorTask = relevantFactsEvaluatorStep.ExecuteAsync(cancellationToken);

                await Task.WhenAll(relevantFactsEvaluatorTask, summarizerTask);

                var saveToMemoryStep = executionScope.ServiceProvider.GetRequiredService<AgentMemorySaverServiceEWCodeStep>();
                await saveToMemoryStep.ExecuteAsync(cancellationToken);

                // TODO: wrap tasks in a pipeline to get the usage statistics for the summarization steps and add them to the main usage statistics list

                summarizerHasRun = true;
            }

            var agentsCosts = CalculateExecutionCosts(usageStatistics);

            return new WorkflowResult
            {
                Message = answerText,
                MainPipelineStepsData = usageStatistics,
                ContextSummarizerHasRun = summarizerHasRun,
                AgentsCostData = agentsCosts,
                CountOfMessages = conversationContext.Conversation.Count(),
                CountOfTokens = conversationContext.TokensCount,
                CountOfMessagesBeforeSummarization = countOfMessagesBeforeSummarization,
                CountOfTokensBeforeSummarization = countOfTokensBeforeSummarization
            };
        }


        private List<AgentExecutionCost> CalculateExecutionCosts(IEnumerable<EWStepStatisticsRecord> stepStatistics)
        {
            var costs = new List<AgentExecutionCost>();
            var agenticSteps = stepStatistics.Where(s => s.IsAgentic && !string.IsNullOrWhiteSpace(s.AgentName))
                .ToList();

            var agentsConfigurationsDict = agentsConfigurations.ToDictionary(a => a.AgentUniqueRole, a => a);

            foreach (var step in agenticSteps)
            {
                if (agentsConfigurationsDict.TryGetValue(step.AgentName!, out var agentConfig))
                {
                    var agentCost = new AgentExecutionCost(
                        AgentName: step.AgentName!,
                        CostPerMillionInputTokens: agentConfig.LLMClassCostPerMillionInputTokens,
                        CostPerMillionOutputTokens: agentConfig.LLMClassCostPerMillionOutputTokens,
                        ConsumedInputTokens: step.InputTokens ?? 0,
                        ConsumedOutputTokens: step.OutputTokens ?? 0
                    );
                    costs.Add(agentCost);
                }
            }
            return costs;
        }


        public Task GetLastExecutionDetails()
        {
            throw new NotImplementedException();
        }

        public Task GetLastExecutionDetailByParameter()
        {
            throw new NotImplementedException();
        }

        public Task GetLastExecutionDetailByStep()
        {
            throw new NotImplementedException();
        }
    }
}
