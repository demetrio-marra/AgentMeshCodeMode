using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public abstract class EasyWorkflowStepBase : IEasyWorkflowStep
    {
        public abstract string Name { get; }
        public abstract bool IsAgentic { get; }
        public abstract string? AgentName { get; }
        public abstract bool IsInputStep { get; }
        public abstract bool IsOutputStep { get; }
        public abstract IEnumerable<AgentInputParameterConfigurationRecord> RequiredParameterNames { get; }

        public abstract Task<WorkflowStepResultRecord> ExecuteAsync(IEnumerable<ParameterRecord> inputParameters, CancellationToken cancellationToken = default);

        protected IEnumerable<AgentInputParameterRecord> ToAgentInputParameters(IEnumerable<ParameterRecord> inputParameters)
        {
            var requiredParameterNames = RequiredParameterNames.ToDictionary(p => p.Name, p => p);
            return inputParameters.Select(p => new AgentInputParameterRecord
            {
                Name = p.Name,
                Value = p.ValueForLLM,
                AsSystemPromptParameter = requiredParameterNames.TryGetValue(p.Name, out var config) ? config.AsSystemPromptParameter : false
            });
        }
    }
}
