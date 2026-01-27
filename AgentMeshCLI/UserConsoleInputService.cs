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
            PersonalAssistantAgentConfiguration personalAssistantConfiguration)
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
                    { ContextManagerAgentConfiguration.AgentName, _contextManagerConfiguration.CostPerMillionInputTokens },
                    { TranslatorAgentConfiguration.AgentName, _translatorConfiguration.CostPerMillionInputTokens },
                    { ContextAggregatorAgentConfiguration.AgentName, _contextAggregatorConfiguration.CostPerMillionInputTokens },
                    { RouterAgentConfiguration.AgentName, _routerConfiguration.CostPerMillionInputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration.CostPerMillionInputTokens },
                    { CoderAgentConfiguration.AgentName, _coderConfiguration.CostPerMillionInputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration.CostPerMillionInputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration.CostPerMillionInputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration.CostPerMillionInputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _personalAssistantConfiguration.CostPerMillionInputTokens }
                };

                var agentOutputCosts = new Dictionary<string, decimal>
                {
                    { ContextManagerAgentConfiguration.AgentName, _contextManagerConfiguration.CostPerMillionOutputTokens },
                    { TranslatorAgentConfiguration.AgentName, _translatorConfiguration.CostPerMillionOutputTokens },
                    { ContextAggregatorAgentConfiguration.AgentName, _contextAggregatorConfiguration.CostPerMillionOutputTokens },
                    { RouterAgentConfiguration.AgentName, _routerConfiguration.CostPerMillionOutputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration.CostPerMillionOutputTokens },
                    { CoderAgentConfiguration.AgentName, _coderConfiguration.CostPerMillionOutputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration.CostPerMillionOutputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration.CostPerMillionOutputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration.CostPerMillionOutputTokens },
                    { PersonalAssistantAgentConfiguration.AgentName, _personalAssistantConfiguration.CostPerMillionOutputTokens }
                };

                ConsoleHelper.PrintTokenUsageSummary(result.InputTokenUsage, result.OutputTokenUsage, agentInputCosts, agentOutputCosts);
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
