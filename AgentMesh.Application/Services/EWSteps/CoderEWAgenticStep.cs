using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CoderEWAgenticStep(
        CoderAgent coderAgent,
        BusinessRequirementsParameter businessRequirementsParameter,
        TechnicalSpecificationParameter technicalSpecificationParameter,
        KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter,
        RequestDateTimeParameter requestDateTimeParameter,
        GeneratedCodeParameter generatedCodeParameter) : IEWAgenticStep
    {
        public string Name => "Coder";

        public string? AgentName => "Coder";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;


        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await coderAgent.ExecuteAsync([
                requestDateTimeParameter,
                businessRequirementsParameter,
                technicalSpecificationParameter,
                knowledgeBaseAPIDocumentsContentParameter], cancellationToken);

            generatedCodeParameter.ParameterValue = agentOutput.Result;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
