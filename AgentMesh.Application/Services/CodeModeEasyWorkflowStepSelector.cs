using AgentMesh.Application.Services.Workflows.Steps;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentMesh.Application.Services
{
    public class CodeModeEasyWorkflowStepSelector : IEasyWorkflowStepSelector
    {
        private readonly AgentMemoryServiceWorkflowStep _agentMemoryServiceWorkflowStep;
        private readonly AgentMemoryQueryExpanderWorkflowStep _agentMemoryQueryExpanderWorkflowStep;
        private readonly DomainsKnowledgeBaseServiceSearchWorkflowStep _domainsKnowledgeBaseServiceSearchWorkflowStep;
        private readonly DomainsKnowledgeBaseDocumentsExtractorWorkflowStep _domainsKnowledgeBaseDocumentsExtractorWorkflowStep;
        private readonly DocumentationWorkflowStep _documentationWorkflowStep;
        private readonly FunctionalAnalystWorkflowStep _functionalAnalystWorkflowStep;
        private readonly APIsKnowledgeBaseServiceSearchWorkflowStep _apisKnowledgeBaseServiceSearchWorkflowStep;
        private readonly APIKnowledgeBaseDocumentsExtractorWorkflowStep _apiKnowledgeBaseDocumentsExtractorWorkflowStep;
        private readonly TechnicalAnalystWorkflowStep _technicalAnalystWorkflowStep;
        private readonly CoderWorkflowStep _coderWorkflowStep;
        private readonly JSSandboxWorkflowStep _jsSandboxWorkflowStep;
        private readonly CodeExecutionFailuresDetectorWorkflowStep _codeExecutionFailuresDetectorWorkflowStep;
        private readonly CodeFixerForRuntimeErrorsWorkflowStep _codeFixerForRuntimeErrorsWorkflowStep;
        private readonly DomainExpertWorkflowStep _domainExpertWorkflowStep;
        private readonly RequestAnalyzerWorkflowStep _requestAnalyzerWorkflowStep;
        private readonly KnowledgeBaseQueryExpanderWorkflowStep _knowledgeBaseQueryExpanderWorkflowStep;
        private readonly RequestCanonicalizationWorkflowStep _requestCanonicalizationWorkflowStep;
        private readonly RerankerWorkflowStep _rerankerWorkflowStep;

        public CodeModeEasyWorkflowStepSelector(AgentMemoryServiceWorkflowStep agentMemoryServiceWorkflowStep,
            AgentMemoryQueryExpanderWorkflowStep agentMemoryQueryExpanderWorkflowStep,
            DomainsKnowledgeBaseServiceSearchWorkflowStep domainsKnowledgeBaseServiceSearchWorkflowStep,
            DomainsKnowledgeBaseDocumentsExtractorWorkflowStep domainsKnowledgeBaseDocumentsExtractorWorkflowStep,
            DocumentationWorkflowStep documentationWorkflowStep,
            FunctionalAnalystWorkflowStep functionalAnalystWorkflowStep,
            APIsKnowledgeBaseServiceSearchWorkflowStep apisKnowledgeBaseServiceSearchWorkflowStep,
            APIKnowledgeBaseDocumentsExtractorWorkflowStep apiKnowledgeBaseDocumentsExtractorWorkflowStep,
            TechnicalAnalystWorkflowStep technicalAnalystWorkflowStep,
            CoderWorkflowStep coderWorkflowStep,
            JSSandboxWorkflowStep jsSandboxWorkflowStep,
            CodeExecutionFailuresDetectorWorkflowStep codeExecutionFailuresDetectorWorkflowStep,
            CodeFixerForRuntimeErrorsWorkflowStep codeFixerForRuntimeErrorsWorkflowStep,
            DomainExpertWorkflowStep domainExpertWorkflowStep,
            RequestAnalyzerWorkflowStep requestAnalyzerWorkflowStep,
            KnowledgeBaseQueryExpanderWorkflowStep knowledgeBaseQueryExpanderWorkflowStep,
            RequestCanonicalizationWorkflowStep requestCanonicalizationWorkflowStep,
            RerankerWorkflowStep rerankerWorkflowStep)
        {
            _agentMemoryServiceWorkflowStep = agentMemoryServiceWorkflowStep;
            _agentMemoryQueryExpanderWorkflowStep = agentMemoryQueryExpanderWorkflowStep;
            _domainsKnowledgeBaseServiceSearchWorkflowStep = domainsKnowledgeBaseServiceSearchWorkflowStep;
            _domainsKnowledgeBaseDocumentsExtractorWorkflowStep = domainsKnowledgeBaseDocumentsExtractorWorkflowStep;
            _documentationWorkflowStep = documentationWorkflowStep;
            _functionalAnalystWorkflowStep = functionalAnalystWorkflowStep;
            _apisKnowledgeBaseServiceSearchWorkflowStep = apisKnowledgeBaseServiceSearchWorkflowStep;
            _apiKnowledgeBaseDocumentsExtractorWorkflowStep = apiKnowledgeBaseDocumentsExtractorWorkflowStep;
            _technicalAnalystWorkflowStep = technicalAnalystWorkflowStep;
            _coderWorkflowStep = coderWorkflowStep;
            _jsSandboxWorkflowStep = jsSandboxWorkflowStep;
            _codeExecutionFailuresDetectorWorkflowStep = codeExecutionFailuresDetectorWorkflowStep;
            _codeFixerForRuntimeErrorsWorkflowStep = codeFixerForRuntimeErrorsWorkflowStep;
            _domainExpertWorkflowStep = domainExpertWorkflowStep;
            _requestAnalyzerWorkflowStep = requestAnalyzerWorkflowStep;
            _knowledgeBaseQueryExpanderWorkflowStep = knowledgeBaseQueryExpanderWorkflowStep;
            _requestCanonicalizationWorkflowStep = requestCanonicalizationWorkflowStep;
            _rerankerWorkflowStep = rerankerWorkflowStep;
        }

        public IEnumerable<IEasyWorkflowStep> NextStepsToRun(IEnumerable<ParameterRecord> parameters)
        {
            throw new NotImplementedException();
        }
    }
}
