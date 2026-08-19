using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryServiceEWCodeStep(
        AgentMemoryExecutor agentMemoryExecutor,
        PastMemoriesQueryParameter pastMemoriesQueryParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter) : IEWStep
    {
        public string Name => "Agent Memory Service";

        public IEnumerable<Type> InputParameterTypes => [
            typeof(PastMemoriesQueryParameter),
            typeof(PastMemoriesQueryResultsParameter),
        ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(PastMemoriesQueryResultsParameter)];


        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var pastMemories = pastMemoriesQueryParameter.ValueAs(Values[typeof(PastMemoriesQueryParameter)]);
            if (pastMemories == null)
            {
                return new EWStepExecutionResult
                {
                    OutputMutations = new Dictionary<Type, object?>()
                };
            }
            var queriesList = pastMemories.Select(memory => memory.Memory).ToList();

            var agentInput = new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", queriesList)
            };

            var executorOutput = await agentMemoryExecutor.GetAsync(agentInput);

            var currentMemories = pastMemoriesQueryResultsParameter.ValueAs(Values[typeof(PastMemoriesQueryResultsParameter)]) ?? [];

            var retrievedMemories = executorOutput.Items.ToList();
            var allMemories = currentMemories.Concat(retrievedMemories).ToList();

            return new EWStepExecutionResult
            {
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(PastMemoriesQueryResultsParameter), allMemories }
                }
            };
        }
    }
}
