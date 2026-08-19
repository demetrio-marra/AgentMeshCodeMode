using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DocumentationEWAgenticStep(
        DocumentationAgent documentationAgent) : IEWAgenticStep
    {
        public string Name => "Documentation";

        public string? AgentName => "Documentation";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => true;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(UserIntentParameter),
            typeof(PastMemoriesQueryResultsParameter),
            typeof(DomainsKnowledgeBaseDocumentsContentParameter),
            typeof(LanguageOfTheUserParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(PipelineResultDataParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await documentationAgent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(PipelineResultDataParameter), agentOutput.Result }
                }
            };
        }
    }
}
