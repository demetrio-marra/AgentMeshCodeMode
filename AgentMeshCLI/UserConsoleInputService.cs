using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Models;
using AgentMesh.Models.Workflows;

namespace AgentMesh
{
    internal class UserConsoleInputService
    {
        private readonly IWorkflow _workflow;
        private readonly IWorkflowProgressNotifier _workflowProgressNotifier;
        private readonly BusinessRequirementsCreatorAgentConfiguration _businessRequirementsCreatorConfiguration;
        private readonly BusinessAdvisorAgentConfiguration _businessAdvisorConfiguration;
        private readonly CoderAgentConfiguration _coderConfiguration;
        private readonly CodeStaticAnalyzerConfiguration _codeStaticAnalyzerConfiguration;
        private readonly CodeFixerAgentConfiguration _codeFixerConfiguration;
        private readonly ResultsPresenterAgentConfiguration _resultsPresenterConfiguration;
        private readonly IntentExtractorAgentConfiguration _intentExtractorConfiguration;
        private readonly PersonalAssistantAgentConfiguration _personalAssistantConfiguration;
        private readonly LLMsConfiguration _llmsConfiguration;
        private readonly ConversationSummarizerAgentConfiguration _conversationSummarizerConfiguration;
        private readonly ContextAnalyzerAgentConfiguration _contextAnalyzerConfiguration;
        private readonly SearchQueriesConciliatorAgentConfiguration _searchQueriesConciliatorConfiguration;
        private readonly SESJSSandboxConfiguration _sesJsSandboxConfiguration;
        private readonly UserConfiguration _userConfiguration;
        private readonly DocumentationAgentConfiguration _documentationAgentConfiguration;
        private readonly IConversationSummarizerAgent _conversationSummarizerAgent;

        public UserConsoleInputService(
            IWorkflow workflow,
            IWorkflowProgressNotifier workflowProgressNotifier,
            BusinessRequirementsCreatorAgentConfiguration businessRequirementsCreatorConfiguration,
            BusinessAdvisorAgentConfiguration businessAdvisorConfiguration,
            CoderAgentConfiguration coderConfiguration,
            CodeStaticAnalyzerConfiguration codeStaticAnalyzerConfiguration,
            CodeFixerAgentConfiguration codeFixerConfiguration,
            ResultsPresenterAgentConfiguration resultsPresenterConfiguration,
            IntentExtractorAgentConfiguration intentExtractorConfiguration,
            PersonalAssistantAgentConfiguration personalAssistantConfiguration,
            LLMsConfiguration llmsConfiguration,
            ConversationSummarizerAgentConfiguration conversationSummarizerConfiguration,
            ContextAnalyzerAgentConfiguration contextAnalyzerConfiguration,
            SearchQueriesConciliatorAgentConfiguration searchQueriesConciliatorConfiguration,
            SESJSSandboxConfiguration sESJSSandboxConfiguration,
            UserConfiguration userConfiguration,
            DocumentationAgentConfiguration documentationAgentConfiguration,
            IConversationSummarizerAgent conversationSummarizerAgent)
        {
            _workflow = workflow;
            _workflowProgressNotifier = workflowProgressNotifier;
            _businessRequirementsCreatorConfiguration = businessRequirementsCreatorConfiguration;
            _businessAdvisorConfiguration = businessAdvisorConfiguration;
            _coderConfiguration = coderConfiguration;
            _codeStaticAnalyzerConfiguration = codeStaticAnalyzerConfiguration;
            _codeFixerConfiguration = codeFixerConfiguration;
            _resultsPresenterConfiguration = resultsPresenterConfiguration;
            _intentExtractorConfiguration = intentExtractorConfiguration;
            _personalAssistantConfiguration = personalAssistantConfiguration;
            _llmsConfiguration = llmsConfiguration;
            _conversationSummarizerConfiguration = conversationSummarizerConfiguration;
            _contextAnalyzerConfiguration = contextAnalyzerConfiguration;
            _searchQueriesConciliatorConfiguration = searchQueriesConciliatorConfiguration;
            _sesJsSandboxConfiguration = sESJSSandboxConfiguration;
            _userConfiguration = userConfiguration;
            _documentationAgentConfiguration = documentationAgentConfiguration;
            _conversationSummarizerAgent = conversationSummarizerAgent;
        }

        public async Task Run()
        {
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
                var result = await _workflow.ExecuteAsync(question!, conversationContext.Conversation.ToList());

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

                // Passiamo l'intera conversazione al context analyzer agent.
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
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _llmsConfiguration[_businessRequirementsCreatorConfiguration.LLM].CostPerMillionInputTokens },
                    { BusinessAdvisorAgentConfiguration.AgentName, _llmsConfiguration[_businessAdvisorConfiguration.LLM].CostPerMillionInputTokens },
                    { CoderAgentConfiguration.AgentName, _llmsConfiguration[_coderConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _llmsConfiguration[_codeStaticAnalyzerConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _llmsConfiguration[_codeFixerConfiguration.LLM].CostPerMillionInputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _llmsConfiguration[_resultsPresenterConfiguration.LLM].CostPerMillionInputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionInputTokens },
                    { ConversationSummarizerAgent.AgentName, _llmsConfiguration[_conversationSummarizerConfiguration.LLM].CostPerMillionInputTokens },
                    { ContextAnalyzerAgent.AgentName, _llmsConfiguration[_contextAnalyzerConfiguration.LLM].CostPerMillionInputTokens },
                    { DocumentationAgent.AgentName, _llmsConfiguration[_documentationAgentConfiguration.LLM].CostPerMillionInputTokens },
                    { SearchQueriesConciliatorAgentConfiguration.AgentName, _llmsConfiguration[_searchQueriesConciliatorConfiguration.LLM].CostPerMillionInputTokens }
                };

                var agentOutputCosts = new Dictionary<string, decimal>
                {
                    { IntentExtractorAgentConfiguration.AgentName, _llmsConfiguration[_intentExtractorConfiguration.LLM].CostPerMillionOutputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _llmsConfiguration[_businessRequirementsCreatorConfiguration.LLM].CostPerMillionOutputTokens },
                    { BusinessAdvisorAgentConfiguration.AgentName, _llmsConfiguration[_businessAdvisorConfiguration.LLM].CostPerMillionOutputTokens },
                    { CoderAgentConfiguration.AgentName, _llmsConfiguration[_coderConfiguration.LLM].CostPerMillionOutputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _llmsConfiguration[_codeStaticAnalyzerConfiguration.LLM].CostPerMillionOutputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _llmsConfiguration[_codeFixerConfiguration.LLM].CostPerMillionOutputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _llmsConfiguration[_resultsPresenterConfiguration.LLM].CostPerMillionOutputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionOutputTokens },
                    { ConversationSummarizerAgent.AgentName, _llmsConfiguration[_conversationSummarizerConfiguration.LLM].CostPerMillionOutputTokens },
                    { ContextAnalyzerAgent.AgentName, _llmsConfiguration[_contextAnalyzerConfiguration.LLM].CostPerMillionOutputTokens },
                    { DocumentationAgent.AgentName, _llmsConfiguration[_documentationAgentConfiguration.LLM].CostPerMillionOutputTokens },
                    { SearchQueriesConciliatorAgentConfiguration.AgentName, _llmsConfiguration[_searchQueriesConciliatorConfiguration.LLM].CostPerMillionOutputTokens }
                };

                ConsoleHelper.WriteLineWithColor($"\n\nConversation status: Count of messages {conversationContext.Conversation.Count()}. Count of tokens: {conversationContext.TokensCount}\n", ConsoleColor.Gray);

                if (conversationContext.TokensCount >= _conversationSummarizerConfiguration.SummaryTokenThreshold)
                {
                    var currentCountOfMessages = conversationContext.Conversation.Count();

                    ConsoleHelper.WriteLineWithColor($"Conversation tokens exceeded configured threshold ({_conversationSummarizerConfiguration.SummaryTokenThreshold}). Summarizing conversation...", ConsoleColor.White);

                    var summarizerInput = new ConversationSummarizerAgentInput {
                        Conversation = conversationContext.Conversation,
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

                    conversationContext.Conversation = summarizationResult.NewConversation;
                    conversationContext.TokensCount = 0; // non fa niente se non è preciso, tanto lo ricalcoliamo al prossimo giro

                    var afterCountOfMessages = conversationContext.Conversation.Count();

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
                    
                    result.UsageStatistics.Add(summarizationTokenUsageEntry);
                }

                ConsoleHelper.PrintTokenUsageSummary(result.UsageStatistics, agentInputCosts, agentOutputCosts);
            }
        }

        private void PrintConfigurations()
        {
            Console.WriteLine("Sandbox Url: " + _sesJsSandboxConfiguration.SandboxServiceURL + ", SandboxName: " + _sesJsSandboxConfiguration.SandboxName + ", AgentId: " + _userConfiguration.AgentId);
            Console.WriteLine("Agent configurations:");
            ConsoleHelper.PrintAgentConfiguration("Intent Extractor", IntentExtractorAgentConfiguration.AgentName, _intentExtractorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Business Requirements Creator", BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Business Advisor", BusinessAdvisorAgentConfiguration.AgentName, _businessAdvisorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Coder", CoderAgentConfiguration.AgentName, _coderConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeStaticAnalyzer", CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeFixer", CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Results Presenter", ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Personal Assistant", PersonalAssistantAgentConfiguration.AgentName, _personalAssistantConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Conversation Summarizer", ConversationSummarizerAgent.AgentName, _conversationSummarizerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Context Analyzer", ContextAnalyzerAgent.AgentName, _contextAnalyzerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Documentation", DocumentationAgent.AgentName, _documentationAgentConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Search Queries Conciliator", SearchQueriesConciliatorAgentConfiguration.AgentName, _searchQueriesConciliatorConfiguration);
            Console.WriteLine();
        }
    }
}
