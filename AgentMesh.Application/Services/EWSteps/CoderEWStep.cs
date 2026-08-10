using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CoderEWStep(
        CoderAgent coderAgent,
        BusinessRequirementsParameter businessRequirementsParameter,
        TechnicalSpecificationParameter technicalSpecificationParameter,
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
            var agentOutput = await coderAgent.ExecuteAsync([businessRequirementsParameter, 
                technicalSpecificationParameter, 
                knowledgeBaseAPIDocumentsContentParameter], cancellationToken);

            generatedCodeParameter.ParameterValue = agentOutput.Result;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
