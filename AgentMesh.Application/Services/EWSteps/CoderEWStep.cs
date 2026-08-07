using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Coder;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CoderEWStep(
        CoderAgent coderAgent,
        BusinessRequirementsParameter businessRequirementsParameter,
        TechnicalSpecificationParameter technicalSpecificationParameter,
        SelectedAPIsFileLocationsParameter selectedAPIsFileLocationsParameter,
        KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter,
        GeneratedCodeParameter generatedCodeParameter) : IEWStep
    {
        public string Name => "Coder";

        public bool IsAgentic => true;

        public string? AgentName => CoderAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly CoderAgent _coderAgent = coderAgent;
        private readonly BusinessRequirementsParameter _businessRequirementsParameter = businessRequirementsParameter;
        private readonly TechnicalSpecificationParameter _technicalSpecificationParameter = technicalSpecificationParameter;
        private readonly SelectedAPIsFileLocationsParameter _selectedAPIsFileLocationsParameter = selectedAPIsFileLocationsParameter;
        private readonly KnowledgeBaseAPIDocumentsContentParameter _knowledgeBaseAPIDocumentsContentParameter = knowledgeBaseAPIDocumentsContentParameter;
        private readonly GeneratedCodeParameter _generatedCodeParameter = generatedCodeParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var docsToPass = (_knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? []).Select(doc => new KnowledgeBaseGetDocsOutputItem
            {
                File = doc.File,
                Content = doc.Content
            });

            var agentInput = new CoderAgentInput
            {
                BusinessRequirements = _businessRequirementsParameter.ParameterValue ?? "(No business requirements)",
                TechnicalSpecification = _technicalSpecificationParameter.ParameterValue ?? "(No technical specification)",
                SelectedAPIsFileLocations = _selectedAPIsFileLocationsParameter.ParameterValue ?? [],
                KnowledgeBaseAPIDocumentsContent = docsToPass
            };

            var agentOutput = await _coderAgent.ExecuteAsync(agentInput, cancellationToken);

            _generatedCodeParameter.ParameterValue = agentOutput.CodeToRun;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
