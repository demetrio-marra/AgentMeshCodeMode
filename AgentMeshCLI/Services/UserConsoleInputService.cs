using AgentMesh.Application.Services;
using AgentMesh.Application.Configuration;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.RelevantFactsEvaluator;
using AgentMesh.Models.Workflows;
using AgentMesh.Models.ChatMessages;
using AgentMesh.Application.Models.ConversationSummarization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AgentMesh.Application.Models.CostsAnalysis;

namespace AgentMesh.Services
{
    internal class UserConsoleInputService(
        ConversationContext conversationContext,
        IWorkflowProgressNotifier workflowProgressNotifier,
        IServiceProvider serviceProvider,
        FunctionalAnalystAgentConfiguration functionalAnalystConfiguration,
        TechnicalAnalystAgentConfiguration technicalAnalystConfiguration,
        CoderAgentConfiguration coderConfiguration,
        CodeFixerAgentConfiguration codeFixerConfiguration,
        DomainExpertAgentConfiguration domainExpertConfiguration,
        PersonalAssistantAgentConfiguration personalAssistantConfiguration,
        LLMsConfiguration llmsConfiguration,
        ConversationSummarizerAgentConfiguration conversationSummarizerConfiguration,
        SESJSSandboxConfiguration sESJSSandboxConfiguration,
        UserConfiguration userConfiguration,
        DocumentationAgentConfiguration documentationAgentConfiguration,
        RelevantFactsEvaluatorAgentConfiguration relevantFactsEvaluatorConfiguration,
        ConversationSummarizerAgent conversationSummarizerAgent,
        RelevantFactsEvaluatorAgent relevantFactsEvaluatorAgent,
        RequestAnalyzerAgentConfiguration requestAnalyzerAgentConfiguration,
        CodeModeWorkflowConfiguration workflowConfiguration,
        EmbeddingServiceConfiguration embeddingServiceConfiguration,
        AgentMemoryExecutor agentMemorySaver,
        RequestCanonicalizationAgentConfiguration requestCanonicalizationAgentConfiguration,
        KnowledgeBaseQueryExpanderAgentConfiguration knowledgeBaseQueryExpanderAgentConfiguration,
        RerankerAgentConfiguration rerankerAgentConfiguration,
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
                var currentConversation = conversationContext.Conversation.ToList();

                var executionScope = serviceProvider.CreateScope();
                var pipeline = executionScope.ServiceProvider.GetRequiredService<EWPipeline>();

                var result = await pipeline.ExecuteAsync(question!, currentConversation);
                var usageStatistics = result.Steps.ToList();

                var answerDateTime = DateTime.UtcNow;

                currentConversation.Add(new ContextMessage
                {
                    Role = ContextMessageRole.User,
                    Date = questionDateTime,
                    Text = question!,
                });
                currentConversation.Add(new ContextMessage
                {
                    Role = ContextMessageRole.Assistant,
                    Date = answerDateTime,
                    Text = result.ResponseForUser,
                });

                conversationContext.TokensCount = result.ContextSizeInTokens;
                conversationContext.Conversation = currentConversation;

                ConsoleHelper.WriteLineWithColor("\nResponse for user:", ConsoleColor.Gray);
                ConsoleHelper.WriteLineWithColor(result.ResponseForUser, ConsoleColor.Cyan);

                var agentInputCosts = new Dictionary<string, decimal>
                {
                    { FunctionalAnalystAgentConfiguration.AgentName, llmsConfiguration[functionalAnalystConfiguration.LLM].CostPerMillionInputTokens },
                    { TechnicalAnalystAgentConfiguration.AgentName, llmsConfiguration[technicalAnalystConfiguration.LLM].CostPerMillionInputTokens },
                    { CoderAgentConfiguration.AgentName, llmsConfiguration[coderConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeFixerAgentConfiguration.AgentName,    llmsConfiguration[codeFixerConfiguration.LLM].CostPerMillionInputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, llmsConfiguration[personalAssistantConfiguration.LLM].CostPerMillionInputTokens },
                    { ConversationSummarizerAgent.AgentName, llmsConfiguration[conversationSummarizerConfiguration.LLM].CostPerMillionInputTokens },
                    { DocumentationAgent.AgentName, llmsConfiguration[documentationAgentConfiguration.LLM].CostPerMillionInputTokens },
                    { RelevantFactsEvaluatorAgentConfiguration.AgentName, llmsConfiguration[relevantFactsEvaluatorConfiguration.LLM].CostPerMillionInputTokens },
                    { RequestAnalyzerAgent.AgentName, llmsConfiguration[requestAnalyzerAgentConfiguration.LLM].CostPerMillionInputTokens },
                    { RequestCanonicalizationAgentConfiguration.AgentName, llmsConfiguration[requestCanonicalizationAgentConfiguration.LLM].CostPerMillionInputTokens },
                    { KnowledgeBaseQueryExpanderAgentConfiguration.AgentName, llmsConfiguration[knowledgeBaseQueryExpanderAgentConfiguration.LLM].CostPerMillionInputTokens },
                    { RerankerAgentConfiguration.AgentName, llmsConfiguration[rerankerAgentConfiguration.LLM].CostPerMillionInputTokens },
                    { "Embedding Service", embeddingServiceConfiguration.CostPerMillionTokens }
                };

                if (workflowConfiguration.EnableDomainExpert)
                {
                    agentInputCosts.Add(DomainExpertAgentConfiguration.AgentName, llmsConfiguration[domainExpertConfiguration.LLM].CostPerMillionInputTokens);
                }

                var agentOutputCosts = new Dictionary<string, decimal>
                {
                    { FunctionalAnalystAgentConfiguration.AgentName, llmsConfiguration[functionalAnalystConfiguration.LLM].CostPerMillionOutputTokens },
                    { TechnicalAnalystAgentConfiguration.AgentName, llmsConfiguration[technicalAnalystConfiguration.LLM].CostPerMillionOutputTokens },
                    { CoderAgentConfiguration.AgentName, llmsConfiguration[coderConfiguration.LLM].CostPerMillionOutputTokens },
                    { CodeFixerAgentConfiguration.AgentName, llmsConfiguration[codeFixerConfiguration.LLM].CostPerMillionOutputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, llmsConfiguration[personalAssistantConfiguration.LLM].CostPerMillionOutputTokens },
                    { ConversationSummarizerAgent.AgentName, llmsConfiguration[conversationSummarizerConfiguration.LLM].CostPerMillionOutputTokens },
                    { DocumentationAgent.AgentName, llmsConfiguration[documentationAgentConfiguration.LLM].CostPerMillionOutputTokens },
                    { RelevantFactsEvaluatorAgentConfiguration.AgentName, llmsConfiguration[relevantFactsEvaluatorConfiguration.LLM].CostPerMillionOutputTokens },
                    { RequestAnalyzerAgent.AgentName, llmsConfiguration[requestAnalyzerAgentConfiguration.LLM].CostPerMillionOutputTokens },
                    { RequestCanonicalizationAgentConfiguration.AgentName, llmsConfiguration[requestCanonicalizationAgentConfiguration.LLM].CostPerMillionOutputTokens },
                    { KnowledgeBaseQueryExpanderAgentConfiguration.AgentName, llmsConfiguration[knowledgeBaseQueryExpanderAgentConfiguration.LLM].CostPerMillionOutputTokens },
                    { RerankerAgentConfiguration.AgentName, llmsConfiguration[rerankerAgentConfiguration.LLM].CostPerMillionOutputTokens }
                };

                if (workflowConfiguration.EnableDomainExpert)
                {
                    agentOutputCosts.Add(DomainExpertAgentConfiguration.AgentName, llmsConfiguration[domainExpertConfiguration.LLM].CostPerMillionOutputTokens);
                }

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
                IsInputStep: false,
                IsOutputStep: false,
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
                AgentName: RelevantFactsEvaluatorAgentConfiguration.AgentName,
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
                IsInputStep: false,
                IsOutputStep: false,
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
                IsInputStep: false,
                IsOutputStep: false,
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
            ConsoleHelper.PrintAgentConfiguration("Request Analyzer", RequestAnalyzerAgent.AgentName, requestAnalyzerAgentConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Knowledge Base Query Expander", KnowledgeBaseQueryExpanderAgentConfiguration.AgentName, knowledgeBaseQueryExpanderAgentConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Reranker", RerankerAgentConfiguration.AgentName, rerankerAgentConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Functional Analyst", FunctionalAnalystAgentConfiguration.AgentName, functionalAnalystConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Technical Analyst", TechnicalAnalystAgentConfiguration.AgentName, technicalAnalystConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Coder", CoderAgentConfiguration.AgentName, coderConfiguration);

            if (workflowConfiguration.EnableCodeCorrection)
            {
                ConsoleHelper.PrintAgentConfiguration("Code Fixer", CodeFixerAgentConfiguration.AgentName, codeFixerConfiguration);
            }

            if (workflowConfiguration.EnableDomainExpert)
            {
                ConsoleHelper.PrintAgentConfiguration("Domain Expert", DomainExpertAgentConfiguration.AgentName, domainExpertConfiguration);
            }

            ConsoleHelper.PrintAgentConfiguration("Personal Assistant", PersonalAssistantAgentConfiguration.AgentName, personalAssistantConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Conversation Summarizer", ConversationSummarizerAgent.AgentName, conversationSummarizerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Documentation Manager", DocumentationAgent.AgentName, documentationAgentConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Relevant Facts Evaluator", RelevantFactsEvaluatorAgentConfiguration.AgentName, relevantFactsEvaluatorConfiguration);
            Console.WriteLine();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Run(stoppingToken);
        }
    }
}

