using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Helpers;
using AgentMesh.Models;
using AgentMesh.Services;
using Polly;
using System.ClientModel;

namespace AgentMesh
{
    internal class RootService
    {
        private readonly BusinessRequirementsCreatorAgent _businessRequirementsCreatorAgent;
        private readonly BusinessRequirementsCreatorAgentConfiguration _businessRequirementsCreatorConfiguration;
        private readonly CoderAgent _coderAgent;
        private readonly CoderAgentConfiguration _coderConfiguration;
        private readonly CodeStaticAnalyzer _codeStaticAnalyzer;
        private readonly CodeStaticAnalyzerConfiguration _codeStaticAnalyzerConfiguration;
        private readonly CodeFixerAgent _codeFixerAgent;
        private readonly CodeFixerAgentConfiguration _codeFixerConfiguration;
        private readonly ResultsPresenterAgent _resultsPresenterAgent;
        private readonly ResultsPresenterAgentConfiguration _resultsPresenterConfiguration;
        private readonly IExecutor<JSSandboxInput, JSSandboxOutput> _jsSandboxExecutor;
        private readonly ContextManagerAgent _contextManagerAgent;
        private readonly ContextManagerAgentConfiguration _contextManagerConfiguration;
        private readonly RouterAgent _routerAgent;
        private readonly RouterAgentConfiguration _routerConfiguration;
        private readonly UserConfiguration _userConfiguration;

        public RootService(
                           BusinessRequirementsCreatorAgent businessRequirementsCreatorAgent,
                           BusinessRequirementsCreatorAgentConfiguration businessRequirementsCreatorConfiguration,
                           CoderAgent coderAgent,
                           CoderAgentConfiguration coderConfiguration,
                           CodeStaticAnalyzer codeStaticAnalyzer,
                           CodeStaticAnalyzerConfiguration codeStaticAnalyzerConfiguration,
                           CodeFixerAgent codeFixerAgent,
                           CodeFixerAgentConfiguration codeFixerConfiguration,
                           ResultsPresenterAgent resultsPresenterAgent,
                           ResultsPresenterAgentConfiguration resultsPresenterConfiguration,
                           IExecutor<JSSandboxInput, JSSandboxOutput> jsSandboxExecutor,
                           ContextManagerAgent contextManagerAgent,
                           ContextManagerAgentConfiguration contextManagerConfiguration,
                           RouterAgent routerAgent,
                           RouterAgentConfiguration routerConfiguration,
                           UserConfiguration userConfiguration)
        {
            _businessRequirementsCreatorAgent = businessRequirementsCreatorAgent;
            _businessRequirementsCreatorConfiguration = businessRequirementsCreatorConfiguration;
            _coderAgent = coderAgent;
            _coderConfiguration = coderConfiguration;
            _codeStaticAnalyzer = codeStaticAnalyzer;
            _codeStaticAnalyzerConfiguration = codeStaticAnalyzerConfiguration;
            _codeFixerAgent = codeFixerAgent;
            _codeFixerConfiguration = codeFixerConfiguration;
            _resultsPresenterAgent = resultsPresenterAgent;
            _resultsPresenterConfiguration = resultsPresenterConfiguration;
            _jsSandboxExecutor = jsSandboxExecutor;
            _contextManagerAgent = contextManagerAgent;
            _contextManagerConfiguration = contextManagerConfiguration;
            _routerAgent = routerAgent;
            _routerConfiguration = routerConfiguration;
            _userConfiguration = userConfiguration;
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

                var state = new RootServiceState(question);

                while (!state.IsCompleted)
                {
                    await ExecuteNextStepAsync(state);
                }

                ConsoleHelper.WriteLineWithColor("\nResponse for user:\n" + state.FinalAnswer!, ConsoleColor.Green);

                var agentInputCosts = new Dictionary<string, decimal>
                {
                    { ContextManagerAgentConfiguration.AgentName, _contextManagerConfiguration.CostPerMillionInputTokens },
                    { RouterAgentConfiguration.AgentName, _routerConfiguration.CostPerMillionInputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration.CostPerMillionInputTokens },
                    { CoderAgentConfiguration.AgentName, _coderConfiguration.CostPerMillionInputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration.CostPerMillionInputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration.CostPerMillionInputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration.CostPerMillionInputTokens }
                };

                var agentOutputCosts = new Dictionary<string, decimal>
                {
                    { ContextManagerAgentConfiguration.AgentName, _contextManagerConfiguration.CostPerMillionOutputTokens },
                    { RouterAgentConfiguration.AgentName, _routerConfiguration.CostPerMillionOutputTokens },
                    { BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration.CostPerMillionOutputTokens },
                    { CoderAgentConfiguration.AgentName, _coderConfiguration.CostPerMillionOutputTokens },
                    { CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration.CostPerMillionOutputTokens },
                    { CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration.CostPerMillionOutputTokens },
                    { ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration.CostPerMillionOutputTokens }
                };

                ConsoleHelper.PrintTokenUsageSummary(state.InputTokenUsage, state.OutputTokenUsage, agentInputCosts, agentOutputCosts);
            }
        }

        

        private async Task ExecuteNextStepAsync(RootServiceState state)
        {
            switch (state.GetNextStep())
            {
                case RootServiceState.RunStep.ContextManager:
                    var contextManagerInput = state.GetInput<ContextManagerAgentInput>();

                    ConsoleHelper.WriteLineWithColor("\nContextManager is enriching the user message with context...", ConsoleColor.DarkYellow);
                    ConsoleHelper.WriteLineWithColor(contextManagerInput.UserSentenceText, ConsoleColor.Cyan);

                    var contextManagerOutput = await ExecuteWithRetryAsync(
                        () => _contextManagerAgent.ExecuteAsync(contextManagerInput),
                        ContextManagerAgentConfiguration.AgentName);

                    state.UpdateState(contextManagerOutput);

                    ConsoleHelper.WriteLineWithColor($"  Tokens consumed: {contextManagerOutput.TokenCount}", ConsoleColor.Magenta);
                    ConsoleHelper.WriteLineWithColor($"  Enriched message: {contextManagerOutput.ContextEnrichedUserSentenceText}", ConsoleColor.Green);
                    break;

                case RootServiceState.RunStep.Router:
                    var routerInput = state.GetInput<RouterAgentInput>();

                    ConsoleHelper.WriteLineWithColor("\nRouter is determining the recipient for the message...", ConsoleColor.DarkYellow);
                    ConsoleHelper.WriteLineWithColor(routerInput.Message, ConsoleColor.Cyan);

                    var routerOutput = await ExecuteWithRetryAsync(
                        () => _routerAgent.ExecuteAsync(routerInput),
                        RouterAgentConfiguration.AgentName);

                    state.UpdateState(routerOutput);

                    ConsoleHelper.WriteLineWithColor($"  Tokens consumed: {routerOutput.TokenCount}", ConsoleColor.Magenta);
                    ConsoleHelper.WriteLineWithColor($"  Recipient: {routerOutput.Recipient}", ConsoleColor.Green);
                    break;

                case RootServiceState.RunStep.BusinessRequirements:

                    var businessRequirementsInput = state.GetInput<BusinessRequirementsCreatorAgentInput>();

                    ConsoleHelper.WriteLineWithColor("\nBusiness Requirements Creator is processing the user question...", ConsoleColor.DarkYellow);
                    ConsoleHelper.WriteLineWithColor(businessRequirementsInput.UserQuestionText, ConsoleColor.Cyan);

                    var brcOutput = await ExecuteWithRetryAsync(
                        () => _businessRequirementsCreatorAgent.ExecuteAsync(businessRequirementsInput),
                        BusinessRequirementsCreatorAgentConfiguration.AgentName);

                    state.UpdateState(brcOutput);

                    ConsoleHelper.WriteLineWithColor($"  Tokens consumed: {brcOutput.TokenCount}", ConsoleColor.Magenta);

                    if (!brcOutput.EngageCoderAgent)
                    {
                        await CompleteWithPresenterAsync(state);
                    }
                    break;

                case RootServiceState.RunStep.Coder:
                    var coderInput = state.GetInput<CoderAgentInput>();

                    ConsoleHelper.WriteLineWithColor("\nCoder Agent is generating code based on the following business requirements:", ConsoleColor.DarkYellow);
                    ConsoleHelper.WriteLineWithColor(coderInput.BusinessRequirements, ConsoleColor.Cyan);

                    var coderAgentOutput = await ExecuteWithRetryAsync(
                        () => _coderAgent.ExecuteAsync(coderInput),
                        CoderAgentConfiguration.AgentName);

                    state.UpdateState(coderAgentOutput);

                    ConsoleHelper.WriteLineWithColor($"  Tokens consumed: {coderAgentOutput.TokenCount}", ConsoleColor.Magenta);

                    var codeWithLineNumbers = GetSourceCodeWithLineNumbers(coderAgentOutput.CodeToRun);
                    state.SetLastCodeWithLineNumbers(codeWithLineNumbers);
                    break;

                case RootServiceState.RunStep.CodeSmellChecker:
                    var staticAnalyzerInput = state.GetInput<CodeStaticAnalyzerInput>();

                    ConsoleHelper.WriteLineWithColor("\nCodeStaticAnalyzer is reviewing the generated code for potential issues:", ConsoleColor.DarkYellow);
                    ConsoleHelper.WriteLineWithColor(state.LastCodeWithLineNumbers ?? string.Empty, ConsoleColor.Cyan);

                    var staticAnalyzerOutput = await ExecuteWithRetryAsync(
                        () => _codeStaticAnalyzer.ExecuteAsync(staticAnalyzerInput),
                        CodeStaticAnalyzerConfiguration.AgentName);

                    state.UpdateState(staticAnalyzerOutput);

                    ConsoleHelper.WriteLineWithColor($"  Tokens consumed: {staticAnalyzerOutput.TokenCount}", ConsoleColor.Magenta);
                    break;

                case RootServiceState.RunStep.CodeFixer:
                    var codeFixerInput = state.GetInput<CodeFixerAgentInput>();

                    ConsoleHelper.WriteLineWithColor("\nCodeFixer Agent is fixing the following issues:", ConsoleColor.DarkYellow);
                    ConsoleHelper.WriteLineWithColor("- " + string.Join("\n- ", codeFixerInput.Issues), ConsoleColor.Cyan);

                    var codeFixerOutput = await ExecuteWithRetryAsync(
                        () => _codeFixerAgent.ExecuteAsync(codeFixerInput),
                        CodeFixerAgentConfiguration.AgentName);

                    state.UpdateState(codeFixerOutput);

                    ConsoleHelper.WriteLineWithColor($"  Tokens consumed: {codeFixerOutput.TokenCount}", ConsoleColor.Magenta);

                    var fixedCodeWithLineNumbers = GetSourceCodeWithLineNumbers(codeFixerOutput.FixedCode);
                    state.SetLastCodeWithLineNumbers(fixedCodeWithLineNumbers);
                    break;

                case RootServiceState.RunStep.Sandbox:
                    var sandboxInput = new JSSandboxInput
                    {
                        AgentId = _userConfiguration.AgentId,
                        Code = state.GeneratedCode ?? string.Empty
                    };

                    ConsoleHelper.WriteLineWithColor("\nCoder agent generated this code to be ran by the Sandbox:", ConsoleColor.DarkYellow);
                    ConsoleHelper.WriteLineWithColor(state.LastCodeWithLineNumbers ?? string.Empty, ConsoleColor.Cyan);

                    try
                    {
                        var executionOutput = await _jsSandboxExecutor.ExecuteAsync(sandboxInput);
                        state.UpdateState(executionOutput);
                    }
                    catch (Exception ex)
                    {
                        state.SetSandboxError(ex.Message);
                    }
                    break;

                case RootServiceState.RunStep.Presenter:

                    await CompleteWithPresenterAsync(state);
                    break;

                case RootServiceState.RunStep.Completed:
                    break;
            }
        }

        private async Task CompleteWithPresenterAsync(RootServiceState state)
        {
            var resultsPresenterInput = state.GetInput<ResultsPresenterAgentInput>();
            ConsoleHelper.WriteLineWithColor("\nResultsPresenter Agent is preparing the final answer for the user.", ConsoleColor.DarkYellow);
            ConsoleHelper.WriteLineWithColor(resultsPresenterInput.UserQuestionText, ConsoleColor.Cyan);
            ConsoleHelper.WriteLineWithColor(resultsPresenterInput.ExecutionResult, ConsoleColor.Cyan);

            var resultsPresenterOutput = await ExecuteWithRetryAsync(
                () => _resultsPresenterAgent.ExecuteAsync(resultsPresenterInput),
                ResultsPresenterAgentConfiguration.AgentName);

            state.UpdateState(resultsPresenterOutput);

            ConsoleHelper.WriteLineWithColor($"  Tokens consumed: {resultsPresenterOutput.TokenCount}", ConsoleColor.Magenta);
        }

        private Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string agentName)
        {
            var policy = Policy
                .Handle<BadStructuredResponseException>()
                .Or<ClientResultException>(ex => ex.Message.Contains("Tool choice is none, but model called a tool"))
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: _ => TimeSpan.FromSeconds(5),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        ConsoleHelper.WriteLineWithColor($"Retry {retryCount} for {agentName} after {timeSpan.TotalSeconds} seconds due to bad structured response: {exception.Message}", ConsoleColor.Yellow);
                    });

            return policy.ExecuteAsync(action);
        }


        private static string GetSourceCodeWithLineNumbers(string sourceCode, bool useSpaceFiller = true)
        {
            var fillerChar = ' ';
            if (!useSpaceFiller)
            {
                fillerChar = '0';
            }
            var codeLines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var maxLineNumberWidth = codeLines.Length.ToString().Length;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < codeLines.Length; i++)
            {
                sb.AppendLine($"[{(i + 1).ToString().PadLeft(maxLineNumberWidth, fillerChar)}] {codeLines[i]}");
            }
            return sb.ToString();
        }


        private void PrintConfigurations()
        {
            Console.WriteLine("Agent configurations:");
            ConsoleHelper.PrintAgentConfiguration("Context Manager", ContextManagerAgentConfiguration.AgentName, _contextManagerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Router", RouterAgentConfiguration.AgentName, _routerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Business Requirements Creator", BusinessRequirementsCreatorAgentConfiguration.AgentName, _businessRequirementsCreatorConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Coder", CoderAgentConfiguration.AgentName, _coderConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeStaticAnalyzer", CodeStaticAnalyzerConfiguration.AgentName, _codeStaticAnalyzerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("CodeFixer", CodeFixerAgentConfiguration.AgentName, _codeFixerConfiguration);
            ConsoleHelper.PrintAgentConfiguration("Results Presenter", ResultsPresenterAgentConfiguration.AgentName, _resultsPresenterConfiguration);
            Console.WriteLine();
        }
    }
}
