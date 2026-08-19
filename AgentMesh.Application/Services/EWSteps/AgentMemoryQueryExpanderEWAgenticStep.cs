using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryQueryExpanderEWAgenticStep(
        AgentMemoryQueryExpanderAgent agentMemoryQueryExpanderAgent) : IEWAgenticStep
    {
        public string Name => "Agent Memory Query Expander";

        public string? AgentName => "AgentMemoryQueryExpander";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(MissingValuesParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(PastMemoriesQueryParameter)
            ];
     
        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await agentMemoryQueryExpanderAgent.ExecuteAsync(Values, cancellationToken);
            
            var pastMemories = agentOutput.Result.Select(q => new AgentMemoryItem { Memory = q }).ToList();

            var ret = new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(PastMemoriesQueryParameter), pastMemories }
                }
            };

            return ret;
        }
    }
}
