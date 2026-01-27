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
        private readonly CoderAgentConfiguration _coderConfiguration;
        private readonly CodeStaticAnalyzerConfiguration _codeStaticAnalyzerConfiguration;
        private readonly CodeFixerAgentConfiguration _codeFixerConfiguration;
        private readonly ResultsPresenterAgentConfiguration _resultsPresenterConfiguration;
        private readonly ContextManagerAgentConfiguration _contextManagerConfiguration;
        private readonly TranslatorAgentConfiguration _translatorConfiguration;
        private readonly ContextAggregatorAgentConfiguration _contextAggregatorConfiguration;
        private readonly RouterAgentConfiguration _routerConfiguration;
        private readonly PersonalAssistantAgentConfiguration _personalAssistantConfiguration;
        private readonly LLMsConfiguration _llmsConfiguration;

        public UserConsoleInputService(
            IWorkflow workflow,
            BusinessRequirementsCreatorAgentConfiguration businessRequirementsCreatorConfiguration,
            CoderAgentConfiguration coderConfiguration,
            CodeStaticAnalyzerConfiguration codeStaticAnalyzerConfiguration,
            CodeFixerAgentConfiguration codeFixerConfiguration,
            ResultsPresenterAgentConfiguration resultsPresenterConfiguration,
            ContextManagerAgentConfiguration contextManagerConfiguration,
            TranslatorAgentConfiguration translatorConfiguration,
            ContextAggregatorAgentConfiguration contextAggregatorConfiguration,
            RouterAgentConfiguration routerConfiguration,
            PersonalAssistantAgentConfiguration personalAssistantConfiguration,
            LLMsConfiguration llmsConfiguration)
        {
            _workflow = workflow;
            _businessRequirementsCreatorConfiguration = businessRequirementsCreatorConfiguration;
            _coderConfiguration = coderConfiguration;
            _codeStaticAnalyzerConfiguration = codeStaticAnalyzerConfiguration;
            _codeFixerConfiguration = codeFixerConfiguration;
            _resultsPresenterConfiguration = resultsPresenterConfiguration;
            _contextManagerConfiguration = contextManagerConfiguration;
            _translatorConfiguration = translatorConfiguration;
            _contextAggregatorConfiguration = contextAggregatorConfiguration;
            _routerConfiguration = routerConfiguration;
            _personalAssistantConfiguration = personalAssistantConfiguration;
            _llmsConfiguration = llmsConfiguration;
        }

        public async Task Run()
        {
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

                var result = await _workflow.ExecuteAsync(question!);

                ConsoleHelper.WriteLineWithColor("\nResponse for user:\n" + result.Response, ConsoleColor.Green);

                var agentInputCosts = new Dictionary<string, decimal>
                {
                    { ContextManagerAgentConfiguration.AgentName, _llmsConfiguration[_contextManagerConfiguration.LLM].CostPerMillionInputTokens },
                    { TranslatorAgentConfiguration.AgentName, _llmsConfiguration[_translatorConfiguration.LLM].CostPerMillionInputTokens },
                    { ContextAggregatorAgentConfiguration.AgentName, _llmsConfiguration[_contextAggregatorConfiguration.LLM].CostPerMillionInputTokens },
                    { RouterAgentConfiguration.AgentName, _llmsConfiguration[_routerConfiguration.LLM].CostPerMillionInputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _llmsConfiguration[_businessRequirementsCreatorConfiguration.LLM].CostPerMillionInputTokens },
                    { CoderAgentConfiguration.AgentName, _llmsConfiguration[_coderConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _llmsConfiguration[_codeStaticAnalyzerConfiguration.LLM].CostPerMillionInputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _llmsConfiguration[_codeFixerConfiguration.LLM].CostPerMillionInputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _llmsConfiguration[_resultsPresenterConfiguration.LLM].CostPerMillionInputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _llmsConfiguration[_personalAssistantConfiguration.LLM].CostPerMillionInputTokens }
                };

                var agentOutputCosts = new Dictionary<string, decimal>
                {
                    { ContextManagerAgentConfiguration.AgentName, _llmsConfiguration[_contextManagerConfiguration.LLM].CostPerMillionOutputTokens },
                    { TranslatorAgentConfiguration.AgentName, _llmsConfiguration[_translatorConfiguration.LLM].CostPerMillionOutputTokens },
                    { ContextAggregatorAgentConfiguration.AgentName, _llmsConfiguration[_contextAggregatorConfiguration.LLM].CostPerMillionOutputTokens },
                    { RouterAgentConfiguration.AgentName, _llmsConfiguration[_routerConfiguration.LLM].CostPerMillionOutputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _llmsConfiguration[_businessRequirementsCreatorConfiguration.LLM].CostPerMillionOutputTokens },
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
            ConsoleHelper.PrintAgentConfiguration("Context Manager", ContextManagerAgentConfiguration.AgentName, _contextManagerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Translator", TranslatorAgentConfiguration.AgentName, _translatorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Context Aggregator", ContextAggregatorAgentConfiguration.AgentName, _contextAggregatorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Router", RouterAgentConfiguration.AgentName, _routerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Business Requirements Creator", BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Coder", CoderAgentConfiguration.AgentName, _coderConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeStaticAnalyzer", CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeFixer", CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Results Presenter", ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Personal Assistant", PersonalAssistantAgentConfiguration.AgentName, _personalAssistantConfiguration);
            Console.WriteLine();
        }
    }
}
