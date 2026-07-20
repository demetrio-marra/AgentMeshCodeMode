namespace AgentMesh.Models
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
        /// The category or classification of the user's intent, indicating the type of action or operation the user wants to perform.
        /// </summary>
        public UserIntentCategoryValues IntentCategory { get; set; }

        /// <summary>
        /// The main topic or subject that the user's request relates to, providing context for the conversation or interaction.
        /// </summary>
        public string ConversationTopic { get; set; } = string.Empty;

        /// <summary>
        /// A collection of features or capabilities mentioned by the user in their request.
        /// </summary>
        public IEnumerable<string> MentionedFeatures { get; set; } = [];

        /// <summary>
        /// A collection of objects or entities referenced by the user in their request.
        /// </summary>
        public IEnumerable<string> MentionedObjects { get; set; } = [];

        /// <summary>
        /// A collection of specific values or parameters mentioned by the user that are relevant to their request.
        /// </summary>
        public IEnumerable<string> MentionedValues { get; set; } = [];

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
