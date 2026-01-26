using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Application.Workflows;
using AgentMesh.Models;
using AgentMesh.Services;
using Polly;
using System.ClientModel;

namespace AgentMesh.Workflows
{
    public class CodeModeWorkflow : IWorkflow
    {
        private readonly IBusinessRequirementsCreatorAgent _businessRequirementsCreatorAgent;
        private readonly ICoderAgent _coderAgent;
        private readonly ICodeStaticAnalyzer _codeStaticAnalyzer;
        private readonly ICodeFixerAgent _codeFixerAgent;
        private readonly IResultsPresenterAgent _resultsPresenterAgent;
        private readonly IExecutor<JSSandboxInput, JSSandboxOutput> _jsSandboxExecutor;
        private readonly IContextManagerAgent _contextManagerAgent;
        private readonly ITranslatorAgent _translatorAgent;
        private readonly IRouterAgent _routerAgent;
        private readonly IPersonalAssistantAgent _personalAssistantAgent;

        public CodeModeWorkflow(
            IBusinessRequirementsCreatorAgent businessRequirementsCreatorAgent,
            ICoderAgent coderAgent,
            ICodeStaticAnalyzer codeStaticAnalyzer,
            ICodeFixerAgent codeFixerAgent,
            IResultsPresenterAgent resultsPresenterAgent,
            IExecutor<JSSandboxInput, JSSandboxOutput> jsSandboxExecutor,
            IContextManagerAgent contextManagerAgent,
            ITranslatorAgent translatorAgent,
            IRouterAgent routerAgent,
            IPersonalAssistantAgent personalAssistantAgent)
        {
            _businessRequirementsCreatorAgent = businessRequirementsCreatorAgent;
            _coderAgent = coderAgent;
            _codeStaticAnalyzer = codeStaticAnalyzer;
            _codeFixerAgent = codeFixerAgent;
            _resultsPresenterAgent = resultsPresenterAgent;
            _jsSandboxExecutor = jsSandboxExecutor;
            _contextManagerAgent = contextManagerAgent;
            _translatorAgent = translatorAgent;
            _routerAgent = routerAgent;
            _personalAssistantAgent = personalAssistantAgent;
        }

        public async Task<WorkflowResult> ExecuteAsync(string userInput)
        {
            var state = new CodeModeWorkflowState(userInput);

            while (!state.IsCompleted)
            {
                await ExecuteNextStepAsync(state);
            }

            return new WorkflowResult
            {
                Response = state.FinalAnswer!,
                InputTokenUsage = state.InputTokenUsage,
                OutputTokenUsage = state.OutputTokenUsage
            };
        }

        private async Task ExecuteNextStepAsync(CodeModeWorkflowState state)
        {
            switch (state.GetNextStep())
            {
                case CodeModeWorkflowState.RunStep.ContextManager:
                    var contextManagerInput = state.GetInput<ContextManagerAgentInput>();

                    var contextManagerOutput = await ExecuteWithRetryAsync(
                        () => _contextManagerAgent.ExecuteAsync(contextManagerInput),
                        ContextManagerAgentConfiguration.AgentName);

                    state.UpdateState(contextManagerOutput);
                    break;

                case CodeModeWorkflowState.RunStep.Translator:
                    var translatorInput = state.GetInput<TranslatorAgentInput>();

                    var translatorOutput = await ExecuteWithRetryAsync(
                        () => _translatorAgent.ExecuteAsync(translatorInput),
                        TranslatorAgentConfiguration.AgentName);

                    state.UpdateState(translatorOutput);
                    break;

                case CodeModeWorkflowState.RunStep.Router:
                    var routerInput = state.GetInput<RouterAgentInput>();

                    var routerOutput = await ExecuteWithRetryAsync(
                        () => _routerAgent.ExecuteAsync(routerInput),
                        RouterAgentConfiguration.AgentName);

                    state.UpdateState(routerOutput);
                    break;

                case CodeModeWorkflowState.RunStep.BusinessRequirements:
                    var businessRequirementsInput = state.GetInput<BusinessRequirementsCreatorAgentInput>();

                    var brcOutput = await ExecuteWithRetryAsync(
                        () => _businessRequirementsCreatorAgent.ExecuteAsync(businessRequirementsInput),
                        BusinessRequirementsCreatorAgentConfiguration.AgentName);

                    state.UpdateState(brcOutput);
                    break;

                case CodeModeWorkflowState.RunStep.Coder:
                    var coderInput = state.GetInput<CoderAgentInput>();

                    var coderAgentOutput = await ExecuteWithRetryAsync(
                        () => _coderAgent.ExecuteAsync(coderInput),
                        CoderAgentConfiguration.AgentName);

                    state.UpdateState(coderAgentOutput);

                    var codeWithLineNumbers = GetSourceCodeWithLineNumbers(coderAgentOutput.CodeToRun);
                    state.SetLastCodeWithLineNumbers(codeWithLineNumbers);
                    break;

                case CodeModeWorkflowState.RunStep.CodeSmellChecker:
                    var staticAnalyzerInput = state.GetInput<CodeStaticAnalyzerInput>();

                    var staticAnalyzerOutput = await ExecuteWithRetryAsync(
                        () => _codeStaticAnalyzer.ExecuteAsync(staticAnalyzerInput),
                        CodeStaticAnalyzerConfiguration.AgentName);

                    state.UpdateState(staticAnalyzerOutput);
                    break;

                case CodeModeWorkflowState.RunStep.CodeFixer:
                    var codeFixerInput = state.GetInput<CodeFixerAgentInput>();

                    var codeFixerOutput = await ExecuteWithRetryAsync(
                        () => _codeFixerAgent.ExecuteAsync(codeFixerInput),
                        CodeFixerAgentConfiguration.AgentName);

                    state.UpdateState(codeFixerOutput);

                    var fixedCodeWithLineNumbers = GetSourceCodeWithLineNumbers(codeFixerOutput.FixedCode);
                    state.SetLastCodeWithLineNumbers(fixedCodeWithLineNumbers);
                    break;

                case CodeModeWorkflowState.RunStep.Sandbox:
                    var sandboxInput = new JSSandboxInput
                    {
                        Code = state.GeneratedCode ?? string.Empty
                    };

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

                case CodeModeWorkflowState.RunStep.Presenter:
                    var resultsPresenterInput = state.GetInput<ResultsPresenterAgentInput>();

                    var resultsPresenterOutput = await ExecuteWithRetryAsync(
                        () => _resultsPresenterAgent.ExecuteAsync(resultsPresenterInput),
                        ResultsPresenterAgentConfiguration.AgentName);

                    state.UpdateState(resultsPresenterOutput);
                    break;

                case CodeModeWorkflowState.RunStep.PersonalAssistant:
                    var personalAssistantInput = state.GetInput<PersonalAssistantAgentInput>();

                    var personalAssistantOutput = await ExecuteWithRetryAsync(
                        () => _personalAssistantAgent.ExecuteAsync(personalAssistantInput),
                        PersonalAssistantAgentConfiguration.AgentName);

                    state.UpdateState(personalAssistantOutput);
                    var contextManagerState = await _contextManagerAgent.GetState();
                    contextManagerState.ChatHistory.Add(new AgentMessage
                    {
                        Role = AgentMessageRole.Assistant,
                        Content = state.FinalAnswer!
                    });
                    await _contextManagerAgent.SetState(contextManagerState);
                    break;

                case CodeModeWorkflowState.RunStep.Completed:
                    break;
            }
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
                        // Silent retry for workflow execution
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
    }
}
