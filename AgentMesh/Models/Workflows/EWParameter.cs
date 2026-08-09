using AgentMesh.Services;

namespace AgentMesh.Models.Workflows
{
    public abstract class EWParameter<T> : IEWParameter
    {
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Set true only for the parameter that is used to store the conversation history for the workflow.
        /// </summary>
        public bool IsConversationHistoryParameter { get; init; }

        /// <summary>
        /// Set true only for the parameter that is used to store the last request from the user.
        /// </summary>
        public bool IsUserCurrentRequestParameter { get; init; }

        /// <summary>
        /// Set true only for the parameter that is used to store the response for the user.
        /// </summary>
        public bool IsResponseForUserParameter { get; init; }

        public T? ParameterValue { get; set; }

        public IEWParameterSerializer DisplayValueSerializer { get; init; } = new DefaultEWParameterSerializer();

        public string GetDisplayValue()
        {
            return DisplayValueSerializer.Serialize(ParameterValue);
        }
    }
}
