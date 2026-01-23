using AgentMesh.Application.Services;
using AgentMesh.Models;

namespace AgentMesh.Application.Models
{
    public class RootServiceState
    {
        public enum RunStep
        {
            ContextManager,
            Translator,
            Router,
            BusinessRequirements,
            Coder,
            CodeSmellChecker,
            CodeFixer,
            Sandbox,
            Presenter,
            PersonalAssistant,
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
        public string? TranslatorResponse { get; private set; }
        public string? DetectedOriginalLanguage { get; private set; }
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
        public string? PresenterOutput { get; private set; }
        public string? FinalAnswer { get; private set; }
        public Dictionary<string, int> TokenUsage { get; private set; }
        public Dictionary<string, int> InputTokenUsage { get; private set; }
        public Dictionary<string, int> OutputTokenUsage { get; private set; }

        // Workflow state flags
        public bool HasContextManagerResponse => !string.IsNullOrWhiteSpace(ContextManagerResponse);
        public bool HasTranslatorResponse => !string.IsNullOrWhiteSpace(TranslatorResponse);
        public bool HasRouterRecipient => !string.IsNullOrWhiteSpace(RouterRecipient);
        public bool HasBusinessRequirements => !string.IsNullOrWhiteSpace(BusinessRequirements) || !string.IsNullOrWhiteSpace(OutputForUserFromBusinessAnalyst);
        public bool HasGeneratedCode => !string.IsNullOrWhiteSpace(GeneratedCode);
        public bool HasInitialCodeCheck => IsCodeValid || CodeIssues.Any();
        public bool NeedsCodeFixer => !IsCodeValid && CodeIssues.Any() && CodeFixerIterationCount < 2;
        public bool HasSandboxResult => !string.IsNullOrWhiteSpace(SandboxResult) || !string.IsNullOrWhiteSpace(SandboxError);
        public bool HasPresenterOutput => !string.IsNullOrWhiteSpace(PresenterOutput);
        public bool IsCompleted => !string.IsNullOrWhiteSpace(FinalAnswer);

        public TInput GetInput<TInput>() where TInput : class, new()
        {
            return typeof(TInput).Name switch
            {
                nameof(ContextManagerAgentInput) => new ContextManagerAgentInput
                {
                    UserSentenceText = UserQuestion
                } as TInput,

                nameof(TranslatorAgentInput) => new TranslatorAgentInput
                {
                    Sentence = ContextManagerResponse!,
                    TargetLanguage = "English"
                } as TInput,

                nameof(RouterAgentInput) => new RouterAgentInput
                {
                    Message = TranslatorResponse!
                } as TInput,

                nameof(BusinessRequirementsCreatorAgentInput) => new BusinessRequirementsCreatorAgentInput
                {
                    UserQuestionText = ContextManagerResponse
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
                    Content = GetExecutionResult()
                } as TInput,

                nameof(PersonalAssistantAgentInput) => new PersonalAssistantAgentInput
                {
                    Sentence = ContextManagerResponse ?? string.Empty,
                    Data = GetDataForPersonalAssistant(),
                    TargetLanguage = DetectedOriginalLanguage ?? "English"
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

                case TranslatorAgentOutput translatorOutput:
                    TranslatorResponse = translatorOutput.TranslatedSentence;
                    DetectedOriginalLanguage = translatorOutput.DetectedOriginalLanguage;
                    AddTokenUsage(TranslatorAgentConfiguration.AgentName, translatorOutput.TokenCount, translatorOutput.InputTokenCount, translatorOutput.OutputTokenCount);
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
                    PresenterOutput = presenterOutput.Content;
                    AddTokenUsage(ResultsPresenterAgentConfiguration.AgentName, presenterOutput.TokenCount, presenterOutput.InputTokenCount, presenterOutput.OutputTokenCount);
                    break;

                case PersonalAssistantAgentOutput personalAssistantOutput:
                    FinalAnswer = personalAssistantOutput.Response;
                    AddTokenUsage(PersonalAssistantAgentConfiguration.AgentName, personalAssistantOutput.TokenCount, personalAssistantOutput.InputTokenCount, personalAssistantOutput.OutputTokenCount);
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

            return string.Empty;
        }

        private string GetDataForPersonalAssistant()
        {
            // If Presenter has run, use its output
            if (!string.IsNullOrWhiteSpace(PresenterOutput))
            {
                return PresenterOutput!;
            }

            // Otherwise, use the direct execution result
            return GetExecutionResult();
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

            if (!HasTranslatorResponse)
            {
                return RunStep.Translator;
            }

            if (!HasRouterRecipient)
            {
                return RunStep.Router;
            }

            // Check router's decision
            if (RouterRecipient?.Equals("Personal Assistant", StringComparison.OrdinalIgnoreCase) == true)
            {
                return RunStep.PersonalAssistant;
            }

            if (RouterRecipient?.Equals("Business Analyst", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (!HasBusinessRequirements)
                {
                    return RunStep.BusinessRequirements;
                }

                // If Business Analyst doesn't engage Coder, go to PersonalAssistant
                if (!ShouldEngageCoder)
                {
                    return RunStep.PersonalAssistant;
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

                // After Sandbox (Coder path), go to Presenter
                if (!HasPresenterOutput)
                {
                    return RunStep.Presenter;
                }

                // After Presenter, go to PersonalAssistant
                return RunStep.PersonalAssistant;
            }

            // Default to PersonalAssistant if recipient is not recognized
            return RunStep.PersonalAssistant;
        }
    }
}
