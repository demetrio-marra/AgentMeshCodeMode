using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Models;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh
{
    internal class UserConsoleInputService(
        IWorkflow workflow,
        IWorkflowProgressNotifier workflowProgressNotifier,
        DomainExpertAgentConfiguration domainExpertConfiguration,
        CoderAgentConfiguration coderConfiguration,
        CodeFixerAgentConfiguration codeFixerConfiguration,
        ResultsPresenterAgentConfiguration resultsPresenterConfiguration,
        IntentExtractorAgentConfiguration intentExtractorConfiguration,
        PersonalAssistantAgentConfiguration personalAssistantConfiguration,
        LLMsConfiguration llmsConfiguration,
        ConversationSummarizerAgentConfiguration conversationSummarizerConfiguration,
        SESJSSandboxConfiguration sESJSSandboxConfiguration,
        UserConfiguration userConfiguration,
        DocumentationAgentConfiguration documentationAgentConfiguration,
        IConversationSummarizerAgent conversationSummarizerAgent,
        CodeModeWorkflowConfiguration workflowConfiguration,
        EmbeddingServiceConfiguration embeddingServiceConfiguration,
        IAgentMemorySaverExecutor agentMemorySaver)
    {
        private readonly IWorkflow _workflow = workflow;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier = workflowProgressNotifier;
        private readonly DomainExpertAgentConfiguration _domainExpertConfiguration = domainExpertConfiguration;
        private readonly CoderAgentConfiguration _coderConfiguration = coderConfiguration;
        private readonly CodeFixerAgentConfiguration _codeFixerConfiguration = codeFixerConfiguration;
        private readonly ResultsPresenterAgentConfiguration _resultsPresenterConfiguration = resultsPresenterConfiguration;
        private readonly IntentExtractorAgentConfiguration _intentExtractorConfiguration = intentExtractorConfiguration;
        private readonly PersonalAssistantAgentConfiguration _personalAssistantConfiguration = personalAssistantConfiguration;
        private readonly LLMsConfiguration _llmsConfiguration = llmsConfiguration;
        private readonly ConversationSummarizerAgentConfiguration _conversationSummarizerConfiguration = conversationSummarizerConfiguration;
        private readonly SESJSSandboxConfiguration _sesJsSandboxConfiguration = sESJSSandboxConfiguration;
        private readonly UserConfiguration _userConfiguration = userConfiguration;
        private readonly DocumentationAgentConfiguration _documentationAgentConfiguration = documentationAgentConfiguration;
        private readonly IConversationSummarizerAgent _conversationSummarizerAgent = conversationSummarizerAgent;
        private readonly CodeModeWorkflowConfiguration _workflowConfiguration = workflowConfiguration;
        private readonly EmbeddingServiceConfiguration _embeddingServiceConfiguration = embeddingServiceConfiguration;
        private readonly IAgentMemorySaverExecutor _agentMemorySaver = agentMemorySaver;

        public async Task Run()
        {
            Console.WriteLine("Welcome to AgentMesh! This is a console application that allows you to interact with the AgentMesh system.\n");

            PrintConfigurations();

            var conversationContext = new ConversationContext();

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
                var result = await _workflow.ExecuteAsync(question!, [.. conversationContext.Conversation]);

                var inputMessageTokens = result.UsageStatistics
                    .Where(e => e.IsAgentic && e.TokensUsage?.AgentName == _workflow.GetIngressExecutorName())
                    .Sum(e => e.TokensUsage?.InputTokens ?? 0);

                var outputMessageTokens = result.UsageStatistics
                    .Where(e => e.IsAgentic && e.TokensUsage?.AgentName == _workflow.GetEgressExecutorName())
                    .Sum(e => e.TokensUsage?.OutputTokens ?? 0);

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
                    Text = result.Response,
                });

                // Passiamo l'intera conversazione al workflow.
                // Quindi i token totali, non devono essere sommati ogni volta,
                // ma semplicemente aggiornati con i token dell'ultima interazione,
                // ai quali aggiungeremo quello di output dell'ultima risposta.
                // In questo modo avremo sempre il conteggio totale dei token in conversazione
                // senza però il conteggio dei token dell'ultimo messaggio di input.
                // Potremmo migliorarlo includendo anche l'ultimo messaggio nel context, prima di inviarlo
                conversationContext.TokensCount = inputMessageTokens + outputMessageTokens;
                conversationContext.Conversation = currentConversation;

                ConsoleHelper.WriteLineWithColor("\nResponse for user:", ConsoleColor.White);
                ConsoleHelper.WriteLineWithColor(result.Response, ConsoleColor.Green);

                var agentInputCosts = new Dictionary<string, decimal>
                {
                    { IntentExtractorAgentConfiguration.AgentName, _llmsConfiguration[_intentExtractorConfiguration.LLM].CostPerMillionInputTokens },
                    { DomainExpertAgentConfiguration.AgentName, _llmsConfiguration[_domainExpertConfiguration.LLM].CostPerMillionInputTokens },
                    { CoderAgentConfiguration.AgentName, _llmsConfiguration[_coderConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _llmsConfiguration[_codeFixerConfiguration.LLM].CostPerMillionInputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _llmsConfiguration[_resultsPresenterConfiguration.LLM].CostPerMillionInputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionInputTokens },
                    { ConversationSummarizerAgent.AgentName, _llmsConfiguration[_conversationSummarizerConfiguration.LLM].CostPerMillionInputTokens },
                    { DocumentationAgent.AgentName, _llmsConfiguration[_documentationAgentConfiguration.LLM].CostPerMillionInputTokens },
                    { "Embedding Service", _embeddingServiceConfiguration.CostPerMillionTokens }
                };

                var agentOutputCosts = new Dictionary<string, decimal>
                {
                    { IntentExtractorAgentConfiguration.AgentName, _llmsConfiguration[_intentExtractorConfiguration.LLM].CostPerMillionOutputTokens },
                    { DomainExpertAgentConfiguration.AgentName, _llmsConfiguration[_domainExpertConfiguration.LLM].CostPerMillionOutputTokens },
                    { CoderAgentConfiguration.AgentName, _llmsConfiguration[_coderConfiguration.LLM].CostPerMillionOutputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _llmsConfiguration[_codeFixerConfiguration.LLM].CostPerMillionOutputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _llmsConfiguration[_resultsPresenterConfiguration.LLM].CostPerMillionOutputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionOutputTokens },
                    { ConversationSummarizerAgent.AgentName, _llmsConfiguration[_conversationSummarizerConfiguration.LLM].CostPerMillionOutputTokens },
                    { DocumentationAgent.AgentName, _llmsConfiguration[_documentationAgentConfiguration.LLM].CostPerMillionOutputTokens }
                };

                ConsoleHelper.WriteLineWithColor($"\n\nConversation status: Count of messages {conversationContext.Conversation.Count()}. Count of tokens: {conversationContext.TokensCount}\n", ConsoleColor.Gray);

                if (conversationContext.TokensCount >= _conversationSummarizerConfiguration.SummaryTokenThreshold)
                {
                    ConsoleHelper.WriteLineWithColor($"Conversation tokens exceeded configured threshold ({_conversationSummarizerConfiguration.SummaryTokenThreshold}). Summarizing conversation...", ConsoleColor.White);

                    var memoryConversation = conversationContext.Conversation.ToList();
                    var summarizerConversation = conversationContext.Conversation.ToList();

                    var memorySaverTask = SaveConversationToAgentMemory(memoryConversation);
                    var summarizerTask = SummarizeChatContextTask(summarizerConversation);

                    await Task.WhenAll(memorySaverTask, summarizerTask);

                    var memorySaverUsage = await memorySaverTask;
                    var summarizerResult = await summarizerTask;

                    conversationContext.Conversation = summarizerResult.NewConversation;
                    conversationContext.TokensCount = 0; // non fa niente se non è preciso, tanto lo ricalcoliamo al prossimo giro

                    result.UsageStatistics.Add(memorySaverUsage);
                    result.UsageStatistics.Add(summarizerResult.Usage);
                }

                ConsoleHelper.PrintTokenUsageSummary(result.UsageStatistics, agentInputCosts, agentOutputCosts);
            }
        }

        private async Task<WorkflowStepUsageEntry> SaveConversationToAgentMemory(List<ContextMessage> conversation)
        {
            var usage = new WorkflowStepUsageEntry
            {
                StepName = "Agent Memory Saver",
                IsAgentic = false
            };

            if (!_workflowConfiguration.EnableMemoryService || !conversation.Any())
            {
                return usage;
            }

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Agent Memory Saver", new Dictionary<string, string>
            {
                { "Conversation", $"<omitted for brevity>. Total: {conversation.Count}" }
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await _agentMemorySaver.ExecuteAsync(new AgentMemorySaverConversationInput
            {
                ConversationHistory = conversation
            });

            stopwatch.Stop();

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Agent Memory Saver", new Dictionary<string, string>
            {
                { "SavedMessagesCount", conversation.Count.ToString() }
            });

            usage.Elapsed = stopwatch.Elapsed;
            return usage;
        }

        private async Task<(WorkflowStepUsageEntry Usage, IEnumerable<ContextMessage> NewConversation)> SummarizeChatContextTask(List<ContextMessage> conversation)
        {
            var currentCountOfMessages = conversation.Count;


            var summarizerInput = new ConversationSummarizerAgentInput
            {
                Conversation = conversation,
                CountOfMessagesToKeep = _conversationSummarizerConfiguration.NumMessageToPreseve,
                SummaryLanguage = _conversationSummarizerConfiguration.SummarizeLanguage
            };

            await _workflowProgressNotifier.NotifyWorkflowStepStart("Conversation Summarizer Agent", new Dictionary<string, string>
            {
                { "Conversation", $"<omitted for brevity>. Total: {summarizerInput.Conversation.Count()}" },
                { "CountOfMessagesToKeep", summarizerInput.CountOfMessagesToKeep.ToString() },
                { "SummaryLanguage", summarizerInput.SummaryLanguage ?? string.Empty }
            });

            var summarizationStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var summarizationResult = await _conversationSummarizerAgent.ExecuteAsync(summarizerInput);
            summarizationStopwatch.Stop();

            await _workflowProgressNotifier.NotifyWorkflowStepEnd("Conversation Summarizer Agent", new Dictionary<string, string>
            {
                { "Conversation", $"<omitted for brevity>. Total: {summarizationResult.NewConversation.Count()}" },
                { "Summary", summarizationResult.Summary.ToString() }
            });

            var afterCountOfMessages = summarizationResult.NewConversation.Count();

            var summarizationTokenUsageEntry = new WorkflowStepUsageEntry
            {
                StepName = "Conversation Summarizer Agent",
                Elapsed = summarizationStopwatch.Elapsed,
                IsAgentic = true,
                TokensUsage = new AgentTokenUsageEntry
                {
                    AgentName = ConversationSummarizerAgent.AgentName,
                    InputTokens = summarizationResult.InputTokenCount,
                    OutputTokens = summarizationResult.OutputTokenCount
                }
            };

            return (summarizationTokenUsageEntry, summarizationResult.NewConversation);
        }


        private void PrintConfigurations()
        {
            Console.WriteLine("Sandbox Url: " + _sesJsSandboxConfiguration.SandboxServiceURL + ", SandboxName: " + _sesJsSandboxConfiguration.SandboxName + ", AgentId: " + _userConfiguration.AgentId);
            Console.WriteLine("Agent configurations:");
            ConsoleHelper.PrintAgentConfiguration("Intent Extractor", IntentExtractorAgentConfiguration.AgentName, _intentExtractorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Domain Expert", DomainExpertAgentConfiguration.AgentName, _domainExpertConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Coder", CoderAgentConfiguration.AgentName, _coderConfiguration);

            if (_workflowConfiguration.EnableCodeCorrection)
            {
                ConsoleHelper.PrintAgentConfiguration("Code Fixer", CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration);
            }

            ConsoleHelper.PrintAgentConfiguration("Results Presenter", ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Personal Assistant", PersonalAssistantAgentConfiguration.AgentName, _personalAssistantConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Conversation Summarizer", ConversationSummarizerAgent.AgentName, _conversationSummarizerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Documentation", DocumentationAgent.AgentName, _documentationAgentConfiguration);
            Console.WriteLine();
        }
    }
}

