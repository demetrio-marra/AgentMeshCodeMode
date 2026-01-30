using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Helpers;
using AgentMesh.Models;

namespace AgentMesh
{
    internal class UserConsoleInputService
    {
        private readonly IWorkflow _workflow;
        private readonly BusinessRequirementsCreatorAgentConfiguration _businessRequirementsCreatorConfiguration;
        private readonly BusinessAdvisorAgentConfiguration _businessAdvisorConfiguration;
        private readonly CoderAgentConfiguration _coderConfiguration;
        private readonly CodeStaticAnalyzerConfiguration _codeStaticAnalyzerConfiguration;
        private readonly CodeFixerAgentConfiguration _codeFixerConfiguration;
        private readonly ResultsPresenterAgentConfiguration _resultsPresenterConfiguration;
        private readonly ContextAnalyzerAgentConfiguration _contextAnalyzerConfiguration;
        private readonly TranslatorAgentConfiguration _translatorConfiguration;
        private readonly RouterAgentConfiguration _routerConfiguration;
        private readonly PersonalAssistantAgentConfiguration _personalAssistantConfiguration;
        private readonly LLMsConfiguration _llmsConfiguration;
        private readonly ConversationSummarizerAgentConfiguration _conversationSummarizerConfiguration;
        private readonly IConversationSummarizerAgent _conversationSummarizerAgent;

        public UserConsoleInputService(
            IWorkflow workflow,
            BusinessRequirementsCreatorAgentConfiguration businessRequirementsCreatorConfiguration,
            BusinessAdvisorAgentConfiguration businessAdvisorConfiguration,
            CoderAgentConfiguration coderConfiguration,
            CodeStaticAnalyzerConfiguration codeStaticAnalyzerConfiguration,
            CodeFixerAgentConfiguration codeFixerConfiguration,
            ResultsPresenterAgentConfiguration resultsPresenterConfiguration,
            ContextAnalyzerAgentConfiguration contextAnalyzerConfiguration,
            TranslatorAgentConfiguration translatorConfiguration,
            RouterAgentConfiguration routerConfiguration,
            PersonalAssistantAgentConfiguration personalAssistantConfiguration,
            LLMsConfiguration llmsConfiguration,
            ConversationSummarizerAgentConfiguration conversationSummarizerConfiguration,
            IConversationSummarizerAgent conversationSummarizerAgent)
        {
            _workflow = workflow;
            _businessRequirementsCreatorConfiguration = businessRequirementsCreatorConfiguration;
            _businessAdvisorConfiguration = businessAdvisorConfiguration;
            _coderConfiguration = coderConfiguration;
            _codeStaticAnalyzerConfiguration = codeStaticAnalyzerConfiguration;
            _codeFixerConfiguration = codeFixerConfiguration;
            _resultsPresenterConfiguration = resultsPresenterConfiguration;
            _contextAnalyzerConfiguration = contextAnalyzerConfiguration;
            _translatorConfiguration = translatorConfiguration;
            _routerConfiguration = routerConfiguration;
            _personalAssistantConfiguration = personalAssistantConfiguration;
            _llmsConfiguration = llmsConfiguration;
            _conversationSummarizerConfiguration = conversationSummarizerConfiguration;
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

                var inputMessageTokens = result.TokenUsageEntries
                    .Where(e => e.AgentName == _workflow.GetIngressAgentName())
                    .Sum(e => e.InputTokens);

                var outputMessageTokens = result.TokenUsageEntries
                    .Where(e => e.AgentName == _workflow.GetEgressAgentName())
                    .Sum(e => e.OutputTokens);

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

                ConsoleHelper.WriteLineWithColor("\nResponse for user:\n" + result.Response, ConsoleColor.Green);

                var agentInputCosts = new Dictionary<string, decimal>
                {
                    { ContextAnalyzerAgentConfiguration.AgentName, _llmsConfiguration[_contextAnalyzerConfiguration.LLM].CostPerMillionInputTokens },
                    { TranslatorAgentConfiguration.AgentName, _llmsConfiguration[_translatorConfiguration.LLM].CostPerMillionInputTokens },
                    { RouterAgentConfiguration.AgentName, _llmsConfiguration[_routerConfiguration.LLM].CostPerMillionInputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _llmsConfiguration[_businessRequirementsCreatorConfiguration.LLM].CostPerMillionInputTokens },
                    { BusinessAdvisorAgentConfiguration.AgentName, _llmsConfiguration[_businessAdvisorConfiguration.LLM].CostPerMillionInputTokens },
                    { CoderAgentConfiguration.AgentName, _llmsConfiguration[_coderConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _llmsConfiguration[_codeStaticAnalyzerConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _llmsConfiguration[_codeFixerConfiguration.LLM].CostPerMillionInputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _llmsConfiguration[_resultsPresenterConfiguration.LLM].CostPerMillionInputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionInputTokens },
                    { ConversationSummarizerAgent.AgentName, _llmsConfiguration[_conversationSummarizerConfiguration.LLM].CostPerMillionInputTokens }
                };

                var agentOutputCosts = new Dictionary<string, decimal>
                {
                    { ContextAnalyzerAgentConfiguration.AgentName, _llmsConfiguration[_contextAnalyzerConfiguration.LLM].CostPerMillionOutputTokens },
                    { TranslatorAgentConfiguration.AgentName, _llmsConfiguration[_translatorConfiguration.LLM].CostPerMillionOutputTokens },
                    { RouterAgentConfiguration.AgentName, _llmsConfiguration[_routerConfiguration.LLM].CostPerMillionOutputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _llmsConfiguration[_businessRequirementsCreatorConfiguration.LLM].CostPerMillionOutputTokens },
                    { BusinessAdvisorAgentConfiguration.AgentName, _llmsConfiguration[_businessAdvisorConfiguration.LLM].CostPerMillionOutputTokens },
                    { CoderAgentConfiguration.AgentName, _llmsConfiguration[_coderConfiguration.LLM].CostPerMillionOutputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _llmsConfiguration[_codeStaticAnalyzerConfiguration.LLM].CostPerMillionOutputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _llmsConfiguration[_codeFixerConfiguration.LLM].CostPerMillionOutputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _llmsConfiguration[_resultsPresenterConfiguration.LLM].CostPerMillionOutputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionOutputTokens },
                    { ConversationSummarizerAgent.AgentName, _llmsConfiguration[_conversationSummarizerConfiguration.LLM].CostPerMillionOutputTokens }
                };

                Console.WriteLine($"\n\nTotal context tokens in conversation history: {conversationContext.TokensCount}\n");

                if (conversationContext.TokensCount >= _conversationSummarizerConfiguration.SummaryTokenThreshold)
                {
                    var currentCountOfMessages = conversationContext.Conversation.Count();

                    ConsoleHelper.WriteLineWithColor("Conversation tokens exceeded threshold. Summarizing conversation...", ConsoleColor.Yellow);
                    var summarizerInput = new ConversationSummarizerAgentInput {
                        Conversation = conversationContext.Conversation,
                        CountOfMessagesToKeep = _conversationSummarizerConfiguration.NumMessageToPreseve,
                        SummaryLanguage = _conversationSummarizerConfiguration.SummarizeLanguage
                    };
                    var summarizationResult = await _conversationSummarizerAgent.ExecuteAsync(summarizerInput);
                    conversationContext.Conversation = summarizationResult.NewConversation;
                    conversationContext.TokensCount = 0; // non fa niente se non è preciso, tanto lo ricalcoliamo al prossimo giro

                    var afterCountOfMessages = conversationContext.Conversation.Count();

                    ConsoleHelper.WriteLineWithColor($"Conversation summarized successfully. Messages count {currentCountOfMessages} -> {afterCountOfMessages}\n", ConsoleColor.Yellow);

                    var summarizationTokenUsageEntry = new AgentTokenUsageEntry
                    {
                        AgentName = ConversationSummarizerAgent.AgentName,
                        InputTokens = summarizationResult.InputTokenCount,
                        OutputTokens = summarizationResult.OutputTokenCount
                    };
                    
                    result.TokenUsageEntries.Add(summarizationTokenUsageEntry);
                }

                ConsoleHelper.PrintTokenUsageSummary(result.TokenUsageEntries, agentInputCosts, agentOutputCosts);
            }
        }

        private void PrintConfigurations()
        {
            Console.WriteLine("Agent configurations:");
            ConsoleHelper.PrintAgentConfiguration("Context Analyzer", ContextAnalyzerAgentConfiguration.AgentName, _contextAnalyzerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Translator", TranslatorAgentConfiguration.AgentName, _translatorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Router", RouterAgentConfiguration.AgentName, _routerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Business Requirements Creator", BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Business Advisor", BusinessAdvisorAgentConfiguration.AgentName, _businessAdvisorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Coder", CoderAgentConfiguration.AgentName, _coderConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeStaticAnalyzer", CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeFixer", CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Results Presenter", ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Personal Assistant", PersonalAssistantAgentConfiguration.AgentName, _personalAssistantConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Conversation Summarizer", ConversationSummarizerAgent.AgentName, _conversationSummarizerConfiguration);
            Console.WriteLine();
        }
    }
}
