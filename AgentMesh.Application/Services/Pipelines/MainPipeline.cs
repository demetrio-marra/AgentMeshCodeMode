using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class MainPipeline(MainPipelineStepSelector ewStepSelector, 
        IWorkflowProgressNotifier workflowProgressNotifier,
        IEnumerable<IEWParameter> parameters) : EWPipeline(ewStepSelector,
            workflowProgressNotifier, 
            parameters)
    {
    }
}
