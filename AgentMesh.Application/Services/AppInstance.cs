using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Models.ConversationSummarization;
using AgentMesh.Application.Models.Costs;
using AgentMesh.Application.Models.RelevantFactsEvaluator;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Application.Services.Executors;
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
        ConversationSummarizerAgentConfiguration conversationSummarizerConfiguration,
        CodeModeWorkflowConfiguration workflowConfiguration,
        ConversationSummarizerAgent conversationSummarizerAgent,
        RelevantFactsEvaluatorAgent relevantFactsEvaluatorAgent,
        AgentMemoryExecutor agentMemorySaverExecutor)
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

            var result = await pipeline.ExecuteAsync(cancellationToken);
            var usageStatistics = result.Steps.ToList();

            var answerDateTime = DateTime.UtcNow;

            conversationContext.Conversation = conversationContext.Conversation.Append(new()
            {
                Role = ContextMessageRole.Assistant,
                Date = answerDateTime,
                Text = result.ResponseForUser,
            });

            conversationContext.TokensCount = result.ContextSizeInTokens;
            bool summarizerHasRun = false;

            int? countOfMessagesBeforeSummarization = null;
            int? countOfTokensBeforeSummarization = null;

            if (conversationContext.TokensCount >= conversationSummarizerConfiguration.SummaryTokenThreshold)
            {
                countOfMessagesBeforeSummarization = conversationContext.Conversation.Count();
                countOfTokensBeforeSummarization = conversationContext.TokensCount;

                var memoryConversation = conversationContext.Conversation.ToList();
                var summarizerConversation = conversationContext.Conversation.ToList();

                var memorySaverTask = SaveConversationToAgentMemory(memoryConversation);
                var summarizerTask = SummarizeChatContextTask(summarizerConversation);

                await Task.WhenAll(memorySaverTask, summarizerTask);

                var memorySaverUsageEntries = await memorySaverTask;
                var summarizerResult = await summarizerTask;

                conversationContext.Conversation = summarizerResult.NewConversation;

                // Dopo la summarization il numero di token in conversazione, corrisponde esattamente al numero di token in output della summarization, perché la conversazione viene sostituita con la nuova conversazione sintetizzata.
                conversationContext.TokensCount = summarizerResult.Usage.OutputTokens ?? 0;

                usageStatistics.AddRange(memorySaverUsageEntries);
                usageStatistics.Add(summarizerResult.Usage);
                summarizerHasRun = true;
            }

            var agentsCosts = CalculateExecutionCosts(usageStatistics);

            return new WorkflowResult
            {
                Message = result.ResponseForUser,
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

        private async Task<List<EWStepStatisticsRecord>> SaveConversationToAgentMemory(List<ContextMessage> conversation)
        {
            var usageEntries = new List<EWStepStatisticsRecord>();

            if (!workflowConfiguration.EnableMemoryService || !conversation.Any())
            {
                return usageEntries;
            }

            var userConversation = conversation
                .Where(message => message.Role == ContextMessageRole.User)
                .Where(message => !string.IsNullOrWhiteSpace(message.Text))
                .ToList();

            if (!userConversation.Any())
            {
                return usageEntries;
            }

            var evaluatorStartTime = DateTime.UtcNow;
            var relevantMessagesResult = await relevantFactsEvaluatorAgent.ExecuteAsync(new RelevantFactsEvaluatorAgentInput
            {
                ConversationHistory = userConversation
            });
            var evaluatorEndTime = DateTime.UtcNow;

            var relevantConversation = BuildRelevantConversationForMemory(conversation, relevantMessagesResult.RelevantUserMessages);
            var relevantUserMessagesCount = relevantConversation.Count(message =>
                message.Role == ContextMessageRole.User &&
                !string.IsNullOrWhiteSpace(message.Text));

            var relevantFactsEvaluatorUsageEntry = new EWStepStatisticsRecord(
                StepName: "Relevant Facts Evaluator Agent",
                StartedOnUtc: evaluatorStartTime,
                CompletedOnUtc: evaluatorEndTime,
                IsFirstAgenticStep: false,
                IsLastAgenticStep: false,
                ParametersBefore:
                [
                    new EWDisplayParameterRecord("UserMessagesCount", userConversation.Count.ToString()),
                    new EWDisplayParameterRecord("RelevantUserMessages", "(Not evaluated yet)")
                ],
                ParametersAfter:
                [
                    new EWDisplayParameterRecord("UserMessagesCount", relevantUserMessagesCount.ToString()),
                    new EWDisplayParameterRecord("RelevantUserMessages", relevantUserMessagesCount > 0 ? "<omitted for brevity>" : "(No relevant user messages)")
                ],
                IsAgentic: true,
                AgentName: "RelevantFactsEvaluator",
                InputTokens: relevantMessagesResult.InputTokenCount,
                OutputTokens: relevantMessagesResult.OutputTokenCount
            );

            //await workflowProgressNotifier.NotifyWorkflowStepCompleted(relevantFactsEvaluatorUsageEntry.StepName, relevantFactsEvaluatorUsageEntry);
            usageEntries.Add(relevantFactsEvaluatorUsageEntry);

            if (relevantUserMessagesCount == 0)
            {
                return usageEntries;
            }

            var memorySaverStartTime = DateTime.UtcNow;

            await agentMemorySaverExecutor.SaveAsync(new AgentMemorySaverConversationInput
            {
                ConversationHistory = relevantConversation
            });

            var memorySaverEndTime = DateTime.UtcNow;

            var memorySaverUsageEntry = new EWStepStatisticsRecord(
                StepName: "Agent Memory Saver",
                StartedOnUtc: memorySaverStartTime,
                CompletedOnUtc: memorySaverEndTime,
                IsFirstAgenticStep: false,
                IsLastAgenticStep: false,
                ParametersBefore:
                [
                    new EWDisplayParameterRecord("SavedMessagesCount", "0")
                ],
                ParametersAfter:
                [
                    new EWDisplayParameterRecord("SavedMessagesCount", relevantUserMessagesCount.ToString())
                ],
                IsAgentic: false,
                AgentName: null,
                InputTokens: null,
                OutputTokens: null);

            //await workflowProgressNotifier.NotifyWorkflowStepCompleted(memorySaverUsageEntry.StepName, memorySaverUsageEntry);
            usageEntries.Add(memorySaverUsageEntry);

            return usageEntries;
        }

        private async Task<(EWStepStatisticsRecord Usage, IEnumerable<ContextMessage> NewConversation)> SummarizeChatContextTask(List<ContextMessage> conversation)
        {
            var summarizerInput = new ConversationSummarizerAgentInput
            {
                Conversation = conversation,
                CountOfMessagesToKeep = conversationSummarizerConfiguration.NumMessageToPreseve,
                SummaryLanguage = conversationSummarizerConfiguration.SummarizeLanguage
            };

            var summarizationStartTime = DateTime.UtcNow;
            var summarizationResult = await conversationSummarizerAgent.ExecuteAsync(summarizerInput);
            var summarizationEndTime = DateTime.UtcNow;

            var summarizationTokenUsageEntry = new EWStepStatisticsRecord(
                StepName: "Conversation Summarizer Agent",
                StartedOnUtc: summarizationStartTime,
                CompletedOnUtc: summarizationEndTime,
                IsFirstAgenticStep: false,
                IsLastAgenticStep: false,
                ParametersBefore:
                [
                    new EWDisplayParameterRecord("ConversationMessagesCount", summarizerInput.Conversation.Count().ToString()),
                    new EWDisplayParameterRecord("Summary", "(Not generated yet)")
                ],
                ParametersAfter:
                [
                    new EWDisplayParameterRecord("ConversationMessagesCount", summarizationResult.NewConversation.Count().ToString()),
                    new EWDisplayParameterRecord("Summary", summarizationResult.Summary.ToString())
                ],
                IsAgentic: true,
                AgentName: ConversationSummarizerAgent.AgentName,
                InputTokens: summarizationResult.InputTokenCount,
                OutputTokens: summarizationResult.OutputTokenCount
            );

            //await workflowProgressNotifier.NotifyWorkflowStepCompleted(summarizationTokenUsageEntry.StepName, summarizationTokenUsageEntry);

            return (summarizationTokenUsageEntry, summarizationResult.NewConversation);
        }


        private static List<ContextMessage> BuildRelevantConversationForMemory(IEnumerable<ContextMessage> conversation, IEnumerable<string> relevantUserMessages)
        {
            var normalizedRelevantMessages = relevantUserMessages
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Select(NormalizeMessageText)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return [.. conversation.Select(message => new ContextMessage
            {
                Role = message.Role,
                Date = message.Date,
                Text = message.Role == ContextMessageRole.User && normalizedRelevantMessages.Contains(NormalizeMessageText(message.Text))
                    ? message.Text
                    : string.Empty
            })];
        }

        private static string NormalizeMessageText(string? value)
            => string.Join(' ', (value ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));



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
