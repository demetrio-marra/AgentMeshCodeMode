using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    /// <summary>
    /// Selects the next steps to run in a workflow based on the provided parameters. 
    /// Implementations of this interface can define custom logic for determining the next step in the workflow.
    /// </summary>
    public interface IEWStepSelector
    {
        /// <summary>
        /// Returns the next steps to run in the workflow based on the provided parameters.
        /// If no more steps are available, it returns an empty collection.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        /// <remarks>If more than one step are returned, they could be run in parallel</remarks>
        IEnumerable<IEasyWorkflowStep> NextStepsToRun(IEnumerable<ParameterRecord> parameters);
    }
}
