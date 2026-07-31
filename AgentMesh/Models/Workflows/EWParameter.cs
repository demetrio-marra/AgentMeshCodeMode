using AgentMesh.Utils;
using System.Text.Json;

namespace AgentMesh.Models.Workflows
{
    public class EWParameter
    {
        public const string NoDataPlaceholder = "(None)";

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

        public Type ParameterType { get; init; } = typeof(string);

        public object? ParameterValue { get; set; }

        /// <summary>
        /// A function that takes the parameter value and returns a string representation for visualization purposes.
        /// By default, it serializes the object to JSON using the default serialization options.
        /// </summary>
        public Func<object?, string> SerializeForVisualization { get; init; } = (ob) => ob == null ? NoDataPlaceholder : JsonSerializer.Serialize(ob, SerializationUtils.DefaultSerializeOptions);
    }
}
