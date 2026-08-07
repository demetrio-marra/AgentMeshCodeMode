using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class EWStepSelector : IEWStepSelector
    {
        private enum SelectorStage
        {
            Start,
            AfterRequestAnalyzer,
            AfterAgentMemoryQueryExpander,
            AfterAgentMemoryService,
            AfterKnowledgeBaseQueryExpander,
            AfterDomainsKnowledgeBaseServiceSearch,
            AfterReranker,
            AfterDomainsKnowledgeBaseDocumentsExtractor,
            AfterDocumentation,
            AfterFunctionalAnalystAndApisKnowledgeBaseSearch,
            AfterApiKnowledgeBaseDocumentsExtractor,
            AfterTechnicalAnalyst,
            AfterCoder,
            AfterInitialSandbox,
            CorrectionDetector,
            CorrectionFixer,
            CorrectionSandbox,
            AfterDomainExpert,
            Finalize,
            Completed
        }

        private readonly UserLastRequestParameter _userLastRequestParameter;
        private readonly IntentCategoryParameter _intentCategoryParameter;
        private readonly MissingValuesParameter _missingValuesParameter;
        private readonly PastMemoriesQueryParameter _pastMemoriesQueryParameter;
        private readonly DomainsKnowledgeBaseQueryParameter _domainsKnowledgeBaseQueryParameter;
        private readonly KnowledgeBaseQueryResultsParameter _knowledgeBaseQueryResultsParameter;
        private readonly FunctionalAnalystRejectedParameter _functionalAnalystRejectedParameter;
        private readonly TechnicalAnalystRejectedParameter _technicalAnalystRejectedParameter;
        private readonly APISKnowledgeBaseQueryResultsParameter _apisKnowledgeBaseQueryResultsParameter;
        private readonly CodeExecutionFailuresDetectorIterationCountParameter _codeExecutionFailuresDetectorIterationCountParameter;
        private readonly CodeExecutionAnalysisParameter _codeExecutionAnalysisParameter;
        private readonly CodeExecutionResultTypeParameter _codeExecutionResultTypeParameter;
        private readonly CodeModeWorkflowConfiguration _workflowConfiguration;
        private readonly RequestAnalyzerEWStep _requestAnalyzerEWStep;
        private readonly AgentMemoryQueryExpanderEWStep _agentMemoryQueryExpanderEWStep;
        private readonly AgentMemoryServiceEWStep _agentMemoryServiceEWStep;
        private readonly KnowledgeBaseQueryExpanderEWStep _knowledgeBaseQueryExpanderEWStep;
        private readonly DomainsKnowledgeBaseServiceSearchEWStep _domainsKnowledgeBaseServiceSearchEWStep;
        private readonly RerankerEWStep _rerankerEWStep;
        private readonly DomainsKnowledgeBaseDocumentsExtractorEWStep _domainsKnowledgeBaseDocumentsExtractorEWStep;
        private readonly DocumentationEWStep _documentationEWStep;
        private readonly FunctionalAnalystEWStep _functionalAnalystEWStep;
        private readonly APIsKnowledgeBaseServiceSearchEWStep _apisKnowledgeBaseServiceSearchEWStep;
        private readonly APIKnowledgeBaseDocumentsExtractorEWStep _apiKnowledgeBaseDocumentsExtractorEWStep;
        private readonly TechnicalAnalystEWStep _technicalAnalystEWStep;
        private readonly CoderEWStep _coderEWStep;
        private readonly JSSandboxEWStep _jsSandboxEWStep;
        private readonly CodeExecutionFailuresDetectorEWStep _codeExecutionFailuresDetectorEWStep;
        private readonly CodeFixerForRuntimeErrorsEWStep _codeFixerForRuntimeErrorsEWStep;
        private readonly DomainExpertEWStep _domainExpertEWStep;
        private readonly PersonalAssistantEWStep _personalAssistantEWStep;

        private SelectorStage _stage = SelectorStage.Start;
        private string? _currentRequest;

        public EWStepSelector(
            UserLastRequestParameter userLastRequestParameter,
            InitialContextMessagesParameter initialContextMessagesParameter,
            UserIntentParameter userIntentParameter,
            IntentCategoryParameter intentCategoryParameter,
            LanguageOfTheUserParameter languageOfTheUserParameter,
            ConversationTopicParameter conversationTopicParameter,
            UserPreferencesParameter userPreferencesParameter,
            UserProvidedDataParameter userProvidedDataParameter,
            UserRequestedActionsParameter userRequestedActionsParameter,
            MissingValuesParameter missingValuesParameter,
            KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter,
            PastMemoriesQueryParameter pastMemoriesQueryParameter,
            DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter,
            PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
            KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter,
            DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter,
            BusinessRequirementsParameter businessRequirementsParameter,
            FunctionalAnalystRejectedParameter functionalAnalystRejectedParameter,
            FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter,
            TechnicalSpecificationParameter technicalSpecificationParameter,
            TechnicalAnalystRejectedParameter technicalAnalystRejectedParameter,
            TechnicalAnalystRejectReasonsParameter technicalAnalystRejectReasonsParameter,
            ShouldEngageCoderParameter shouldEngageCoderParameter,
            APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter,
            SelectedAPIsFileLocationsParameter selectedAPIsFileLocationsParameter,
            DocumentationContentParameter documentationContentParameter,
            GeneratedCodeParameter generatedCodeParameter,
            LastCodeWithLineNumbersParameter lastCodeWithLineNumbersParameter,
            CodeExecutionFailuresDetectorIterationCountParameter codeExecutionFailuresDetectorIterationCountParameter,
            CodeExecutionAnalysisParameter codeExecutionAnalysisParameter,
            SandboxResultParameter sandboxResultParameter,
            SandboxExecutionIdParameter sandboxExecutionIdParameter,
            CodeExecutionResultTypeParameter codeExecutionResultTypeParameter,
            ExecutionErrorParameter executionErrorParameter,
            DomainExpertOutputParameter domainExpertOutputParameter,
            PersonalAssistantOpeningSentenceParameter personalAssistantOpeningSentenceParameter,
            PersonalAssistantClosingSentenceParameter personalAssistantClosingSentenceParameter,
            PersonalAssistantConvenienceErrorSentenceParameter personalAssistantConvenienceErrorSentenceParameter,
            FinalAnswerParameter finalAnswerParameter,
            CodeModeWorkflowConfiguration workflowConfiguration,
            RequestAnalyzerEWStep requestAnalyzerEWStep,
            AgentMemoryQueryExpanderEWStep agentMemoryQueryExpanderEWStep,
            AgentMemoryServiceEWStep agentMemoryServiceEWStep,
            KnowledgeBaseQueryExpanderEWStep knowledgeBaseQueryExpanderEWStep,
            DomainsKnowledgeBaseServiceSearchEWStep domainsKnowledgeBaseServiceSearchEWStep,
            RerankerEWStep rerankerEWStep,
            DomainsKnowledgeBaseDocumentsExtractorEWStep domainsKnowledgeBaseDocumentsExtractorEWStep,
            DocumentationEWStep documentationEWStep,
            FunctionalAnalystEWStep functionalAnalystEWStep,
            APIsKnowledgeBaseServiceSearchEWStep apisKnowledgeBaseServiceSearchEWStep,
            APIKnowledgeBaseDocumentsExtractorEWStep apiKnowledgeBaseDocumentsExtractorEWStep,
            TechnicalAnalystEWStep technicalAnalystEWStep,
            CoderEWStep coderEWStep,
            JSSandboxEWStep jsSandboxEWStep,
            CodeExecutionFailuresDetectorEWStep codeExecutionFailuresDetectorEWStep,
            CodeFixerForRuntimeErrorsEWStep codeFixerForRuntimeErrorsEWStep,
            DomainExpertEWStep domainExpertEWStep,
            PersonalAssistantEWStep personalAssistantEWStep)
        {
            _userLastRequestParameter = userLastRequestParameter;
            _intentCategoryParameter = intentCategoryParameter;
            _missingValuesParameter = missingValuesParameter;
            _pastMemoriesQueryParameter = pastMemoriesQueryParameter;
            _domainsKnowledgeBaseQueryParameter = domainsKnowledgeBaseQueryParameter;
            _knowledgeBaseQueryResultsParameter = knowledgeBaseQueryResultsParameter;
            _functionalAnalystRejectedParameter = functionalAnalystRejectedParameter;
            _technicalAnalystRejectedParameter = technicalAnalystRejectedParameter;
            _apisKnowledgeBaseQueryResultsParameter = apisKnowledgeBaseQueryResultsParameter;
            _codeExecutionFailuresDetectorIterationCountParameter = codeExecutionFailuresDetectorIterationCountParameter;
            _codeExecutionAnalysisParameter = codeExecutionAnalysisParameter;
            _codeExecutionResultTypeParameter = codeExecutionResultTypeParameter;
            _workflowConfiguration = workflowConfiguration;
            _requestAnalyzerEWStep = requestAnalyzerEWStep;
            _agentMemoryQueryExpanderEWStep = agentMemoryQueryExpanderEWStep;
            _agentMemoryServiceEWStep = agentMemoryServiceEWStep;
            _knowledgeBaseQueryExpanderEWStep = knowledgeBaseQueryExpanderEWStep;
            _domainsKnowledgeBaseServiceSearchEWStep = domainsKnowledgeBaseServiceSearchEWStep;
            _rerankerEWStep = rerankerEWStep;
            _domainsKnowledgeBaseDocumentsExtractorEWStep = domainsKnowledgeBaseDocumentsExtractorEWStep;
            _documentationEWStep = documentationEWStep;
            _functionalAnalystEWStep = functionalAnalystEWStep;
            _apisKnowledgeBaseServiceSearchEWStep = apisKnowledgeBaseServiceSearchEWStep;
            _apiKnowledgeBaseDocumentsExtractorEWStep = apiKnowledgeBaseDocumentsExtractorEWStep;
            _technicalAnalystEWStep = technicalAnalystEWStep;
            _coderEWStep = coderEWStep;
            _jsSandboxEWStep = jsSandboxEWStep;
            _codeExecutionFailuresDetectorEWStep = codeExecutionFailuresDetectorEWStep;
            _codeFixerForRuntimeErrorsEWStep = codeFixerForRuntimeErrorsEWStep;
            _domainExpertEWStep = domainExpertEWStep;
            _personalAssistantEWStep = personalAssistantEWStep;
        }

        public IEnumerable<IEWStep> NextStepsToRun()
        {
            if (_currentRequest != _userLastRequestParameter.ParameterValue)
            {
                _currentRequest = _userLastRequestParameter.ParameterValue;
                _stage = SelectorStage.Start;
            }

            return _stage switch
            {
                SelectorStage.Start => MoveTo(SelectorStage.AfterRequestAnalyzer, _requestAnalyzerEWStep),
                SelectorStage.AfterRequestAnalyzer => SelectAfterRequestAnalyzer(),
                SelectorStage.AfterAgentMemoryQueryExpander => SelectAfterAgentMemoryQueryExpander(),
                SelectorStage.AfterAgentMemoryService => SelectAfterAgentMemoryService(),
                SelectorStage.AfterKnowledgeBaseQueryExpander => SelectAfterKnowledgeBaseQueryExpander(),
                SelectorStage.AfterDomainsKnowledgeBaseServiceSearch => SelectAfterDomainsKnowledgeBaseServiceSearch(),
                SelectorStage.AfterReranker => SelectAfterReranker(),
                SelectorStage.AfterDomainsKnowledgeBaseDocumentsExtractor => SelectAfterDomainsKnowledgeBaseDocumentsExtractor(),
                SelectorStage.AfterDocumentation => MoveTo(SelectorStage.Finalize),
                SelectorStage.AfterFunctionalAnalystAndApisKnowledgeBaseSearch => SelectAfterFunctionalAnalystAndApisKnowledgeBaseSearch(),
                SelectorStage.AfterApiKnowledgeBaseDocumentsExtractor => MoveTo(SelectorStage.AfterTechnicalAnalyst, _technicalAnalystEWStep),
                SelectorStage.AfterTechnicalAnalyst => SelectAfterTechnicalAnalyst(),
                SelectorStage.AfterCoder => MoveTo(SelectorStage.AfterInitialSandbox, _jsSandboxEWStep),
                SelectorStage.AfterInitialSandbox => SelectAfterInitialSandbox(),
                SelectorStage.CorrectionDetector => SelectCorrectionDetector(),
                SelectorStage.CorrectionFixer => MoveTo(SelectorStage.CorrectionSandbox, _codeFixerForRuntimeErrorsEWStep),
                SelectorStage.CorrectionSandbox => SelectCorrectionSandbox(),
                SelectorStage.AfterDomainExpert => MoveTo(SelectorStage.Finalize),
                SelectorStage.Finalize => MoveTo(SelectorStage.Completed, _personalAssistantEWStep),
                _ => []
            };
        }

        private IEnumerable<IEWStep> SelectAfterRequestAnalyzer()
        {
            if ((_missingValuesParameter.ParameterValue ?? []).Any())
            {
                return MoveTo(SelectorStage.AfterAgentMemoryQueryExpander, _agentMemoryQueryExpanderEWStep);
            }

            return SelectAfterOptionalMemoryFlow();
        }

        private IEnumerable<IEWStep> SelectAfterAgentMemoryQueryExpander()
        {
            if ((_pastMemoriesQueryParameter.ParameterValue ?? []).Any())
            {
                return MoveTo(SelectorStage.AfterAgentMemoryService, _agentMemoryServiceEWStep);
            }

            return SelectAfterOptionalMemoryFlow();
        }

        private IEnumerable<IEWStep> SelectAfterAgentMemoryService() => SelectAfterOptionalMemoryFlow();

        private IEnumerable<IEWStep> SelectAfterOptionalMemoryFlow()
        {
            if ((_intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other) == UserIntentCategory.Other)
            {
                return MoveTo(SelectorStage.Finalize);
            }

            return MoveTo(SelectorStage.AfterKnowledgeBaseQueryExpander, _knowledgeBaseQueryExpanderEWStep);
        }

        private IEnumerable<IEWStep> SelectAfterKnowledgeBaseQueryExpander()
        {
            if ((_domainsKnowledgeBaseQueryParameter.ParameterValue ?? []).Any())
            {
                return MoveTo(SelectorStage.AfterDomainsKnowledgeBaseServiceSearch, _domainsKnowledgeBaseServiceSearchEWStep);
            }

            return SelectByIntentCategory();
        }

        private IEnumerable<IEWStep> SelectAfterDomainsKnowledgeBaseServiceSearch()
        {
            if ((_knowledgeBaseQueryResultsParameter.ParameterValue ?? []).Any())
            {
                return MoveTo(SelectorStage.AfterReranker, _rerankerEWStep);
            }

            return SelectByIntentCategory();
        }

        private IEnumerable<IEWStep> SelectAfterReranker()
        {
            if ((_knowledgeBaseQueryResultsParameter.ParameterValue ?? []).Any())
            {
                return MoveTo(SelectorStage.AfterDomainsKnowledgeBaseDocumentsExtractor, _domainsKnowledgeBaseDocumentsExtractorEWStep);
            }

            return SelectByIntentCategory();
        }

        private IEnumerable<IEWStep> SelectAfterDomainsKnowledgeBaseDocumentsExtractor() => SelectByIntentCategory();

        private IEnumerable<IEWStep> SelectByIntentCategory()
        {
            var intentCategory = _intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other;

            if (intentCategory == UserIntentCategory.Documentation)
            {
                return MoveTo(SelectorStage.AfterDocumentation, _documentationEWStep);
            }

            if (intentCategory == UserIntentCategory.TaskExecution)
            {
                _stage = SelectorStage.AfterFunctionalAnalystAndApisKnowledgeBaseSearch;
                return [_functionalAnalystEWStep, _apisKnowledgeBaseServiceSearchEWStep];
            }

            return MoveTo(SelectorStage.Finalize);
        }

        private IEnumerable<IEWStep> SelectAfterFunctionalAnalystAndApisKnowledgeBaseSearch()
        {
            if (_functionalAnalystRejectedParameter.ParameterValue == true)
            {
                return MoveTo(SelectorStage.Finalize);
            }

            if ((_apisKnowledgeBaseQueryResultsParameter.ParameterValue ?? []).Any())
            {
                return MoveTo(SelectorStage.AfterApiKnowledgeBaseDocumentsExtractor, _apiKnowledgeBaseDocumentsExtractorEWStep);
            }

            return MoveTo(SelectorStage.AfterTechnicalAnalyst, _technicalAnalystEWStep);
        }

        private IEnumerable<IEWStep> SelectAfterTechnicalAnalyst()
        {
            if (_technicalAnalystRejectedParameter.ParameterValue)
            {
                return MoveTo(SelectorStage.Finalize);
            }

            return MoveTo(SelectorStage.AfterCoder, _coderEWStep);
        }

        private IEnumerable<IEWStep> SelectAfterInitialSandbox()
        {
            return _codeExecutionResultTypeParameter.ParameterValue switch
            {
                SandboxResultType.CallError => MoveTo(SelectorStage.Finalize),
                SandboxResultType.ApplicationError or SandboxResultType.SyntaxError =>
                    _workflowConfiguration.EnableCodeCorrection
                        ? MoveTo(SelectorStage.CorrectionDetector, _codeExecutionFailuresDetectorEWStep)
                        : MoveTo(SelectorStage.Finalize),
                _ => _workflowConfiguration.EnableDomainExpert
                        ? MoveTo(SelectorStage.AfterDomainExpert, _domainExpertEWStep)
                        : MoveTo(SelectorStage.Finalize)
            };
        }

        private IEnumerable<IEWStep> SelectCorrectionDetector()
        {
            if (string.Equals(_codeExecutionAnalysisParameter.ParameterValue,
                    JavascriptCodeExecutionFailuresDetectorAgent.NO_ERROR,
                    StringComparison.OrdinalIgnoreCase))
            {
                return _workflowConfiguration.EnableDomainExpert
                    ? MoveTo(SelectorStage.AfterDomainExpert, _domainExpertEWStep)
                    : MoveTo(SelectorStage.Finalize);
            }

            return MoveTo(SelectorStage.CorrectionFixer, _codeFixerForRuntimeErrorsEWStep);
        }

        private IEnumerable<IEWStep> SelectCorrectionSandbox()
        {
            if (_codeExecutionResultTypeParameter.ParameterValue == SandboxResultType.CallError)
            {
                return _workflowConfiguration.EnableDomainExpert
                    ? MoveTo(SelectorStage.AfterDomainExpert, _domainExpertEWStep)
                    : MoveTo(SelectorStage.Finalize);
            }

            var detectorRuns = _codeExecutionFailuresDetectorIterationCountParameter.ParameterValue ?? 0;
            if ((_codeExecutionResultTypeParameter.ParameterValue == SandboxResultType.ApplicationError
                 || _codeExecutionResultTypeParameter.ParameterValue == SandboxResultType.SyntaxError)
                && detectorRuns < 2)
            {
                return MoveTo(SelectorStage.CorrectionDetector, _codeExecutionFailuresDetectorEWStep);
            }

            return _workflowConfiguration.EnableDomainExpert
                ? MoveTo(SelectorStage.AfterDomainExpert, _domainExpertEWStep)
                : MoveTo(SelectorStage.Finalize);
        }

        private IEnumerable<IEWStep> MoveTo(SelectorStage nextStage, IEWStep? step = null)
        {
            _stage = nextStage;
            return step != null ? [step] : [];
        }
    }
}
