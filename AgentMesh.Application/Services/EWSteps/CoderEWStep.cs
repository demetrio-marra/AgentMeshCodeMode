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

        public string? AgentName => "Coder";

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;


        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var docsToPass = (knowledgeBaseAPIDocumentsContentParameter.ParameterValue ?? []).Select(doc => new KnowledgeBaseGetDocsOutputItem
            {
                File = doc.File,
                Content = doc.Content
            });

            var agentInput = new CoderAgentInput
            {
                BusinessRequirements = businessRequirementsParameter.ParameterValue ?? "(No business requirements)",
                TechnicalSpecification = technicalSpecificationParameter.ParameterValue ?? "(No technical specification)",
                SelectedAPIsFileLocations = selectedAPIsFileLocationsParameter.ParameterValue ?? [],
                KnowledgeBaseAPIDocumentsContent = docsToPass
            };

            var agentOutput = await coderAgent.ExecuteAsync(agentInput, cancellationToken);

            generatedCodeParameter.ParameterValue = agentOutput.CodeToRun;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
