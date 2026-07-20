namespace AgentMesh.Models.RequestAnalysis
{
    public class StructuredUserRequest
    {
        /// <summary>
        /// The user's intention or goal in their request. 
        /// It is a concise description of what the user wants to achieve or accomplish. 
        /// This property is used to capture the essence of the user's request and can be used for further processing, such as routing the request to the appropriate handler or generating a response.
        /// </summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// The main topic or subject that the user's request relates to, providing context for the conversation or interaction.
        /// If the user's request does not clearly indicate a specific topic, this property may be left empty or null.
        /// </summary>
        public string? ConversationTopic { get; set; } = string.Empty;

        /// <summary>
        /// A collection of features or capabilities mentioned by the user in their request.
        /// </summary>
        public IEnumerable<string> UserRequestedActions { get; set; } = [];

        /// <summary>
        /// A collection of specific values or parameters mentioned by the user that are relevant to their request.
        /// </summary>
        public IEnumerable<string> UserProvidedData { get; set; } = [];

        /// <summary>
        /// A collection of values or information that the user's request indicates are missing but necessary to complete the request.
        /// </summary>
        public IEnumerable<string> MissingValues { get; set; } = [];

        /// <summary>
        /// The natural language or locale of the user, indicating the language in which the user communicated their request.
        /// </summary>
        public string LanguageOfTheUser { get; set; } = string.Empty;
    }
}
