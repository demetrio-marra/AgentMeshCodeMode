using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    /// <summary>
    /// This parameter is built-in and cannot be modified by the developer. 
    /// It is used to store the final answer that will be returned to the user after the workflow execution is completed. The value of this parameter is set by the workflow steps and can be accessed by the application to present the final response to the user.
    /// </summary>
    public sealed class FinalAnswerParameter : EWParameter<string>
    {
        public const string ParamName = "Final answer";
        public FinalAnswerParameter()
        {
            Name = ParamName;
        }
    }
}
