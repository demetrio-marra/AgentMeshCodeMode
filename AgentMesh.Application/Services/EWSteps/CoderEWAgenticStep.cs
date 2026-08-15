using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
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

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;


        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await coderAgent.ExecuteAsync([
                requestDateTimeParameter,
                businessRequirementsParameter,
                technicalSpecificationParameter,
                knowledgeBaseAPIDocumentsContentParameter], cancellationToken);

            generatedCodeParameter.ParameterValue = agentOutput.Result;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
