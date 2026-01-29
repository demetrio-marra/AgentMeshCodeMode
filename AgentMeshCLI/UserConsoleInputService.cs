using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Helpers;
using AgentMesh.Models;
using AgentMesh.Services;

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
            LLMsConfiguration llmsConfiguration)
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
        }

        public async Task Run()
        {
            PrintConfigurations();

            var conversation = new List<ContextMessage>();

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
                var result = await _workflow.ExecuteAsync(question!, conversation);
                var answerDateTime = DateTime.UtcNow;

                conversation.Add(new ContextMessage
                {
                    Role = ContextMessageRole.User,
                    Date = questionDateTime,
                    Text = question!
                });
                conversation.Add(new ContextMessage
                {
                    Role = ContextMessageRole.Assistant,
                    Date = answerDateTime,
                    Text = result.Response
                });

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
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionInputTokens }
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
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionOutputTokens }
                };

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
            Console.WriteLine();
        }
    }
}
