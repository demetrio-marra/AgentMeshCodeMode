using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Coder;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
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

        private readonly CoderAgent coderAgent = coderAgent;
        private readonly BusinessRequirementsParameter businessRequirementsParameter = businessRequirementsParameter;
        private readonly TechnicalSpecificationParameter technicalSpecificationParameter = technicalSpecificationParameter;
        private readonly SelectedAPIsFileLocationsParameter selectedAPIsFileLocationsParameter = selectedAPIsFileLocationsParameter;
        private readonly KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter = knowledgeBaseAPIDocumentsContentParameter;
        private readonly GeneratedCodeParameter generatedCodeParameter = generatedCodeParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var docsToPass = (this.knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? []).Select(doc => new KnowledgeBaseGetDocsOutputItem
            {
                File = doc.File,
                Content = doc.Content
            });

            var agentInput = new CoderAgentInput
            {
                BusinessRequirements = this.businessRequirementsParameter.ParameterValue ?? "(No business requirements)",
                TechnicalSpecification = this.technicalSpecificationParameter.ParameterValue ?? "(No technical specification)",
                SelectedAPIsFileLocations = this.selectedAPIsFileLocationsParameter.ParameterValue ?? [],
                KnowledgeBaseAPIDocumentsContent = docsToPass
            };

            var agentOutput = await this.coderAgent.ExecuteAsync(agentInput, cancellationToken);

            this.generatedCodeParameter.ParameterValue = agentOutput.CodeToRun;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
