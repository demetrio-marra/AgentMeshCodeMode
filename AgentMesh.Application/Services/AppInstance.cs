using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Models.Costs;
using AgentMesh.Application.Models.Workflows;
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
        ConversationSummarizationConfiguration conversationSummarizerConfiguration)
    {
        public int CountOfMessages { get => conversationContext.Conversation.Count(); }
        public int CountOfTokensInContext { get => conversationContext.TokensCount; }
        public decimal CumulatedCost { get; private set; }

        public async Task InitConversation()
        {
            conversationContext.TokensCount = 0;
            conversationContext.Conversation = [];
            CumulatedCost = 0;

            await Task.CompletedTask;
        }

        public async Task<WorkflowResult> ProcessRequest(string message, CancellationToken cancellationToken)
        {
            var requestDatetime = DateTime.UtcNow;

            var executionScope = serviceProvider.CreateScope();

            var pipeline = executionScope.ServiceProvider.GetRequiredService<IChatRequestPipeline>();
            pipeline.InitialChatHistory = conversationContext.Conversation.ToList();
            pipeline.UserLastRequest = message;

            var stepsStats = await pipeline.ExecuteAsync(cancellationToken);
            var usageStatistics = stepsStats.ToList();

            var answerDateTime = DateTime.UtcNow;
            var answerText = pipeline.FinalResponse;

            conversationContext.Conversation = conversationContext.Conversation.Append(new()
            {
                Role = ContextMessageRole.User,
                Date = requestDatetime,
                Text = message,
            });
            conversationContext.Conversation = conversationContext.Conversation.Append(new()
            {
                Role = ContextMessageRole.Assistant,
                Date = answerDateTime,
                Text = answerText,
            });

            var inputTokens = stepsStats.Where(s => s.CountInputTokensAsContextTokens).Sum(s => s.InputTokens ?? 0);
            var outputTokens = stepsStats.Where(s => s.CountOutputTokensAsContextTokens).Sum(s => s.OutputTokens ?? 0);

            conversationContext.TokensCount = inputTokens + outputTokens;

            bool summarizerHasRun = false;

            int? countOfMessagesBeforeSummarization = null;
            int? countOfTokensBeforeSummarization = null;

            if (conversationContext.RequiresSummarization)
            {
                var cntBefore = conversationContext.Conversation.Count();
                var cntTokensBefore = conversationContext.TokensCount;

                var countOfMessagesToPreserve = cntBefore < conversationSummarizerConfiguration.NumMessageToPreseve ? 
                    cntBefore : conversationSummarizerConfiguration.NumMessageToPreseve;

                var countOfMessagesToIncludeInSummarization = cntBefore - countOfMessagesToPreserve;

                if (countOfMessagesToIncludeInSummarization > 0)
                {
                    var messagesToSummarize = conversationContext.Conversation.Take(countOfMessagesToIncludeInSummarization).ToList();

                    var summarizationPipeline = executionScope.ServiceProvider.GetRequiredService<ISummarizationPipeline>();
                    summarizationPipeline.ChatMessagesToSummarize = messagesToSummarize;
                    summarizationPipeline.SummarizationLanguage = conversationSummarizerConfiguration.SummarizeLanguage;

                    var summarizationStepStats = await summarizationPipeline.ExecuteAsync(cancellationToken);
                    usageStatistics.AddRange(summarizationStepStats.ToList());

                    var summarizationContentParameter = summarizationPipeline.SummarizedContent;
                    var summarizationDatetimeParameter = summarizationPipeline.SummarizedContentDatetime;

                    var summaryMessage = new ContextMessage
                    {
                        Role = ContextMessageRole.Assistant,
                        Date = summarizationDatetimeParameter,
                        Text = summarizationContentParameter
                    };

                    // Rimuoviamo i messaggi che sono stati riassunti e li sostituiamo con il messaggio di riepilogo
                    var messagesToKeep = conversationContext.Conversation.Skip(countOfMessagesToIncludeInSummarization).ToList();
                    conversationContext.Conversation = messagesToKeep.Prepend(summaryMessage).ToList();

                    // Numero simbolico.
                    // Per avere reale accuratezza dovremmo aggiungere il conteggio dei token a ciascun messaggio nel chatcontext
                    // in modo da poterlo sommare al numero di token del messaggio di riepilogo. Per ora, impostiamo un numero simbolico.
                    // Ad ogni modo, il corretto numero di token sarà ristabilito alla successiva richiesta
                    conversationContext.TokensCount = 100;

                    countOfMessagesBeforeSummarization = cntBefore;
                    countOfTokensBeforeSummarization = cntTokensBefore;
                    summarizerHasRun = true;
                }
            }

            var agentsCosts = CalculateExecutionCosts(usageStatistics);
            CumulatedCost += agentsCosts.Sum(c => c.InputCost + c.OutputCost);

            return new WorkflowResult
            {
                Message = answerText,
                MainPipelineStepsData = usageStatistics,
                ContextSummarizerHasRun = summarizerHasRun,
                AgentsCostData = agentsCosts,
                CountOfMessages = conversationContext.Conversation.Count(),
                CountOfTokens = conversationContext.TokensCount,
                CountOfMessagesBeforeSummarization = countOfMessagesBeforeSummarization,
                CountOfTokensBeforeSummarization = countOfTokensBeforeSummarization,
                CumulatedCost = CumulatedCost
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
