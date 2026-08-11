using AgentMesh.Application.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.RelevantFactsEvaluator;
using AgentMesh.Application.Models.ConversationSummarization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AgentMesh.Application.Models.CostsAnalysis;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Application.Services.Executors;
using System.Globalization;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Models;

namespace AgentMesh.Services
{
    internal class UserConsoleInputService(
        IServiceProvider serviceProvider,
        IEnumerable<AgentFlatConfigurationRecord> agentsConfigurations,
        ConversationContext conversationContext,
        IWorkflowProgressNotifier workflowProgressNotifier,
        ConversationSummarizerAgentConfiguration conversationSummarizerConfiguration,
        UserConfiguration userConfiguration,
        ConversationSummarizerAgent conversationSummarizerAgent,
        RelevantFactsEvaluatorAgent relevantFactsEvaluatorAgent,
        CodeModeWorkflowConfiguration workflowConfiguration,
        EmbeddingServiceConfiguration embeddingServiceConfiguration,
        AgentMemoryExecutor agentMemorySaver,
        SESJSSandboxConfiguration sesJSSandboxConfiguration) : BackgroundService
    {
        public async Task Run(CancellationToken cancellationToken)
        {
            Console.WriteLine("Welcome to AgentMesh! This is a console application that allows you to interact with the AgentMesh system.\n");

            PrintConfigurations();

            while (true)
            {
                Console.WriteLine("Enter your question or type 'exit':");
                Console.Write("> ");
                var question = Console.ReadLine();

                if (string.IsNullOrEmpty(question))
                {
                    Console.WriteLine("Please enter a valid question.");
                    continue;
                }

                if (string.Equals(question?.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                var questionDateTime = DateTime.UtcNow;

                conversationContext.Conversation = conversationContext.Conversation.Append(new()
                {
                    Role = ContextMessageRole.User,
                    Date = questionDateTime,
                    Text = question!,
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

                ConsoleHelper.WriteLineWithColor("\nResponse for user:", ConsoleColor.Gray);
                ConsoleHelper.WriteLineWithColor(result.ResponseForUser, ConsoleColor.Cyan);

                var agentInputCosts = new Dictionary<string, decimal>
                {
                    { "Embedding Service", embeddingServiceConfiguration.CostPerMillionTokens }
                };
                
                agentsConfigurations.Select(a => new { a.AgentUniqueRole, a.LLMClassCostPerMillionOutputTokens })
                    .ToList()
                    .ForEach(a => agentInputCosts.Add(a.AgentUniqueRole, a.LLMClassCostPerMillionOutputTokens));

                var agentOutputCosts = agentsConfigurations.Select(a => new { a.AgentUniqueRole, a.LLMClassCostPerMillionOutputTokens })
                    .ToList()
                    .ToDictionary(a => a.AgentUniqueRole, a => a.LLMClassCostPerMillionOutputTokens);

                ConsoleHelper.WriteLineWithColor($"\n\nConversation status: Count of messages {conversationContext.Conversation.Count()}. Count of tokens: {conversationContext.TokensCount}\n", ConsoleColor.Gray);

                if (conversationContext.TokensCount >= conversationSummarizerConfiguration.SummaryTokenThreshold)
                {
                    ConsoleHelper.WriteLineWithColor($"Conversation tokens exceeded configured threshold ({conversationSummarizerConfiguration.SummaryTokenThreshold}). Summarizing conversation...", ConsoleColor.White);

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
                }

                ConsoleHelper.PrintTokenUsageSummary(usageStatistics, agentInputCosts, agentOutputCosts);
            }
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

            await workflowProgressNotifier.NotifyWorkflowStepCompleted(relevantFactsEvaluatorUsageEntry.StepName, relevantFactsEvaluatorUsageEntry);
            usageEntries.Add(relevantFactsEvaluatorUsageEntry);

            if (relevantUserMessagesCount == 0)
            {
                return usageEntries;
            }

            var memorySaverStartTime = DateTime.UtcNow;

            await agentMemorySaver.SaveAsync(new AgentMemorySaverConversationInput
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

            await workflowProgressNotifier.NotifyWorkflowStepCompleted(memorySaverUsageEntry.StepName, memorySaverUsageEntry);
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

            await workflowProgressNotifier.NotifyWorkflowStepCompleted(summarizationTokenUsageEntry.StepName, summarizationTokenUsageEntry);

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

        private void PrintConfigurations()
        {
            Console.WriteLine("Sandbox Url: " + sesJSSandboxConfiguration.SandboxServiceURL + ", SandboxName: " + sesJSSandboxConfiguration.SandboxName + ", AgentId: " + userConfiguration.AgentId);
            Console.WriteLine("Agent configurations:");
            foreach(var agentConfig in agentsConfigurations)
            {
                ConsoleHelper.PrintAgentConfiguration(agentConfig.AgentUniqueRole, agentConfig.ProviderModelName, Convert.ToDouble(agentConfig.Temperature, CultureInfo.InvariantCulture));
            }
            Console.WriteLine();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Run(stoppingToken);
        }
    }
}

