using AgentMesh.Models;
using System.Text;

namespace AgentMesh.Application
{
    public class MessageSerializationUtils
    {
        public const string MESSAGES_SEPARATOR = "════════";

        public static string SerializeConversationHistory(IEnumerable<ContextMessage> contextMessages)
        {
            var sb = new StringBuilder();
            foreach (var message in contextMessages)
            {
                var role = message.Role == ContextMessageRole.User ? "User" : "Assistant";
                sb.AppendLine($"{role} {message.Date:yyyy-MM-ddTHH:mm:ssZ}");
                sb.AppendLine(message.Text);
                sb.AppendLine(MESSAGES_SEPARATOR);
            }
            return sb.ToString();
        }

        public static string SerializeConversationHistory(IEnumerable<ContextMessage> contextMessages, string userLastRequest)
        {
            var ret = SerializeConversationHistory(contextMessages);
            var sb = new StringBuilder(ret);

            sb.AppendLine($"<user_last_request>");
            sb.AppendLine(userLastRequest);
            sb.AppendLine("</user_last_request>");

            return sb.ToString();
        }

        public static string SerializeRequestAndContext(string requestContext, string requestText)
        {
            var sb = new StringBuilder();

            sb.AppendLine("```context");
            sb.AppendLine(requestContext);
            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("```userRequest");
            sb.AppendLine(requestText);
            sb.AppendLine("```");
            sb.AppendLine();

            return sb.ToString();
        }

        public static string AddAdditionalDataToSerializedMessage(string existingUserMessage, string additionalDataLabel, string additionalDataContent)
        {
            var sb = new StringBuilder(existingUserMessage);
            sb.AppendLine("```" + additionalDataLabel);
            sb.AppendLine(additionalDataContent);
            sb.AppendLine("```");
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
