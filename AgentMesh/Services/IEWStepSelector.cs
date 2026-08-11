namespace AgentMesh.Services
{
    /// <summary>
    /// Selects the next steps to run in a workflow based on the provided parameters.
    /// Implementations of this interface can define custom logic for determining the next step in the workflow.
    /// </summary>
    public interface IEWStepSelector
    {
        /// <summary>
        /// The step which initializes workflow parameters
        /// </summary>
        /// <returns></returns>
        IEWCodeStep GetInitStep();

        /// <summary>
        /// Returns the next steps to run in the workflow based on the provided parameters.
        /// If no more steps are available, it returns an empty collection.
        /// </summary>
        /// <returns>The steps to run</returns>
        /// <remarks>If more than one step are returned, they could be run in parallel</remarks>
        IEnumerable<IEWStep> NextStepsToRun();
    }
}
