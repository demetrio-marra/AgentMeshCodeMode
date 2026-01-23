using AgentMesh.Application.Services;
using AgentMesh.Models;

namespace AgentMesh.Application.Models
{
    public class RootServiceState
    {
        public enum RunStep
        {
            ContextManager,
            Router,
            BusinessRequirements,
            Coder,
            CodeSmellChecker,
            CodeFixer,
            Sandbox,
            Presenter,
            Completed
        }

        public RootServiceState(string userQuestion)
        {
            UserQuestion = userQuestion;
            TokenUsage = new Dictionary<string, int>();
            InputTokenUsage = new Dictionary<string, int>();
            OutputTokenUsage = new Dictionary<string, int>();
        }

        // Pure state properties
        public string UserQuestion { get; }
        public string? ContextManagerResponse { get; private set; }
        public string? RouterRecipient { get; private set; }
        public string? BusinessRequirements { get; private set; }
        public bool ShouldEngageCoder { get; private set; }
        public string? OutputForUserFromBusinessAnalyst { get; private set; }
        public string? GeneratedCode { get; private set; }
        public string? LastCodeWithLineNumbers { get; private set; }
        public List<string> CodeIssues { get; private set; } = new();
        public bool IsCodeValid { get; private set; }
        public int CodeFixerIterationCount { get; private set; }
        public bool HasBeenCheckedAfterFix { get; private set; }
        public string? SandboxResult { get; private set; }
        public string? SandboxError { get; private set; }
        public string? FinalAnswer { get; private set; }
        public Dictionary<string, int> TokenUsage { get; private set; }
        public Dictionary<string, int> InputTokenUsage { get; private set; }
        public Dictionary<string, int> OutputTokenUsage { get; private set; }

        // Workflow state flags
        public bool HasContextManagerResponse => !string.IsNullOrWhiteSpace(ContextManagerResponse);
        public bool HasRouterRecipient => !string.IsNullOrWhiteSpace(RouterRecipient);
        public bool HasBusinessRequirements => !string.IsNullOrWhiteSpace(BusinessRequirements) || !string.IsNullOrWhiteSpace(OutputForUserFromBusinessAnalyst);
        public bool HasGeneratedCode => !string.IsNullOrWhiteSpace(GeneratedCode);
        public bool HasInitialCodeCheck => IsCodeValid || CodeIssues.Any();
        public bool NeedsCodeFixer => !IsCodeValid && CodeIssues.Any() && CodeFixerIterationCount < 2;
        public bool HasSandboxResult => !string.IsNullOrWhiteSpace(SandboxResult) || !string.IsNullOrWhiteSpace(SandboxError);
        public bool IsCompleted => !string.IsNullOrWhiteSpace(FinalAnswer);

        public TInput GetInput<TInput>() where TInput : class, new()
        {
            return typeof(TInput).Name switch
            {
                nameof(ContextManagerAgentInput) => new ContextManagerAgentInput
                {
                    UserSentenceText = UserQuestion
                } as TInput,

                nameof(RouterAgentInput) => new RouterAgentInput
                {
                    Message = ContextManagerResponse!
                } as TInput,

                nameof(BusinessRequirementsCreatorAgentInput) => new BusinessRequirementsCreatorAgentInput
                {
                    UserQuestionText = UserQuestion
                } as TInput,

                nameof(CoderAgentInput) => new CoderAgentInput
                {
                    BusinessRequirements = BusinessRequirements ?? string.Empty
                } as TInput,

                nameof(CodeStaticAnalyzerInput) => new CodeStaticAnalyzerInput
                {
                    CodeToFix = LastCodeWithLineNumbers ?? string.Empty
                } as TInput,

                nameof(CodeFixerAgentInput) => new CodeFixerAgentInput
                {
                    CodeToFix = LastCodeWithLineNumbers ?? string.Empty,
                    Issues = CodeIssues
                } as TInput,

                nameof(ResultsPresenterAgentInput) => new ResultsPresenterAgentInput
                {
                    UserQuestionText = UserQuestion,
                    ExecutionResult = GetExecutionResult()
                } as TInput,

                _ => throw new NotSupportedException($"Input type {typeof(TInput).Name} is not supported")
            } ?? throw new InvalidOperationException($"Failed to create input for {typeof(TInput).Name}");
        }

        public void UpdateState<TOutput>(TOutput output) where TOutput : class
        {
            switch (output)
            {
                case ContextManagerAgentOutput cmOutput:
                    ContextManagerResponse = cmOutput.ContextEnrichedUserSentenceText;
                    AddTokenUsage(ContextManagerAgentConfiguration.AgentName, cmOutput.TokenCount, cmOutput.InputTokenCount, cmOutput.OutputTokenCount);
                    break;

                case RouterAgentOutput routerOutput:
                    RouterRecipient = routerOutput.Recipient;
                    AddTokenUsage(RouterAgentConfiguration.AgentName, routerOutput.TokenCount, routerOutput.InputTokenCount, routerOutput.OutputTokenCount);
                    break;

                case BusinessRequirementsCreatorAgentOutput brcOutput:
                    ShouldEngageCoder = brcOutput.EngageCoderAgent;
                    if (brcOutput.EngageCoderAgent)
                    {
                        BusinessRequirements = brcOutput.BusinessRequirements;
                    }
                    else
                    {
                        OutputForUserFromBusinessAnalyst = brcOutput.AnswerToUserText ?? string.Empty;
                    }
                    AddTokenUsage(BusinessRequirementsCreatorAgentConfiguration.AgentName, brcOutput.TokenCount, brcOutput.InputTokenCount, brcOutput.OutputTokenCount);
                    break;

                case CoderAgentOutput coderOutput:
                    GeneratedCode = coderOutput.CodeToRun;
                    // Reset code smell check state when new code is generated
                    IsCodeValid = false;
                    CodeIssues.Clear();
                    HasBeenCheckedAfterFix = false;
                    AddTokenUsage(CoderAgentConfiguration.AgentName, coderOutput.TokenCount, coderOutput.InputTokenCount, coderOutput.OutputTokenCount);
                    break;

                case CodeStaticAnalyzerOutput staticAnalyzerOutput:
                    IsCodeValid = !staticAnalyzerOutput.Violations.Any();
                    if (!IsCodeValid)
                    {
                        CodeIssues = staticAnalyzerOutput.Violations.ToList();
                    }
                    else
                    {
                        CodeIssues.Clear();
                    }
                    HasBeenCheckedAfterFix = true;
                    AddTokenUsage(CodeStaticAnalyzerConfiguration.AgentName, staticAnalyzerOutput.TokenCount, staticAnalyzerOutput.InputTokenCount, staticAnalyzerOutput.OutputTokenCount);
                    break;

                case CodeFixerAgentOutput codeFixerOutput:
                    GeneratedCode = codeFixerOutput.FixedCode;
                    CodeFixerIterationCount++;
                    IsCodeValid = false;
                    CodeIssues.Clear();
                    HasBeenCheckedAfterFix = false;
                    AddTokenUsage(CodeFixerAgentConfiguration.AgentName, codeFixerOutput.TokenCount, codeFixerOutput.InputTokenCount, codeFixerOutput.OutputTokenCount);
                    break;

                case JSSandboxOutput sandboxOutput:
                    SandboxResult = sandboxOutput.Result;
                    SandboxError = null;
                    break;

                case ResultsPresenterAgentOutput presenterOutput:
                    FinalAnswer = presenterOutput.Answer;
                    AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, presenterOutput.TokenCount, presenterOutput.InputTokenCount, presenterOutput.OutputTokenCount);
                    break;

                default:
                    throw new NotSupportedException($"Output type {typeof(TOutput).Name} is not supported");
            }
        }

        private void AddTokenUsage(string agentName, int tokenCount, int inputTokenCount, int outputTokenCount)
        {
            if (TokenUsage.ContainsKey(agentName))
            {
                TokenUsage[agentName] += tokenCount;
                InputTokenUsage[agentName] += inputTokenCount;
                OutputTokenUsage[agentName] += outputTokenCount;
            }
            else
            {
                TokenUsage[agentName] = tokenCount;
                InputTokenUsage[agentName] = inputTokenCount;
                OutputTokenUsage[agentName] = outputTokenCount;
            }
        }

        public void SetSandboxError(string error)
        {
            SandboxError = error;
            SandboxResult = null;
        }

        public void SetLastCodeWithLineNumbers(string codeWithLineNumbers)
        {
            LastCodeWithLineNumbers = codeWithLineNumbers;
        }

        public void SetFinalAnswer(string answer)
        {
            FinalAnswer = answer;
        }

        private string GetExecutionResult()
        {
            if (!string.IsNullOrWhiteSpace(OutputForUserFromBusinessAnalyst))
            {
                return OutputForUserFromBusinessAnalyst!;
            }

            if (!string.IsNullOrWhiteSpace(SandboxError))
            {
                return SandboxError!;
            }

            // return if sandbox result is available
            if (HasSandboxResult)
            {
                return SandboxResult ?? string.Empty;
            }

            return UserQuestion;
        }

        public RunStep GetNextStep()
        {
            if (IsCompleted)
            {
                return RunStep.Completed;
            }

            if (!HasContextManagerResponse)
            {
                return RunStep.ContextManager;
            }

            if (!HasRouterRecipient)
            {
                return RunStep.Router;
            }

            // Check router's decision
            if (RouterRecipient?.Equals("Personal Assistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                return RunStep.Presenter;
            }

            if (RouterRecipient?.Equals("Business Analyst", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (!HasBusinessRequirements)
                {
                    return RunStep.BusinessRequirements;
                }

                if (!ShouldEngageCoder)
                {
                    return RunStep.Presenter;
                }

                if (!HasGeneratedCode)
                {
                    return RunStep.Coder;
                }

                if (!HasInitialCodeCheck)
                {
                    return RunStep.CodeSmellChecker;
                }

                if (NeedsCodeFixer)
                {
                    return RunStep.CodeFixer;
                }

                if (CodeFixerIterationCount > 0 && !HasBeenCheckedAfterFix)
                {
                    return RunStep.CodeSmellChecker;
                }

                if (!HasSandboxResult)
                {
                    return RunStep.Sandbox;
                }

                return RunStep.Presenter;
            }

            // Default to Presenter if recipient is not recognized
            return RunStep.Presenter;
        }
    }
}
