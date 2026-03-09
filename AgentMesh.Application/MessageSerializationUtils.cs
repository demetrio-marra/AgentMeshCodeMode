using AgentMesh.Models;
using System.Text;

namespace AgentMesh.Application
{
    public class MessageSerializationUtils
    {
        public const string MESSAGES_SEPARATOR = "════════";

        private const string SECTION_BEGIN_MARKER = "<<<<<<<< BEGIN `{0}` SECTION >>>>>>>>";
        private const string SECTION_END_MARKER = "<<<<<<<< END `{0}` SECTION >>>>>>>>";

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
            
            var section = WrapSection("conversation history", sb.ToString().TrimEnd());
            return section;
        }

        public static string SerializeConversationHistory(IEnumerable<ContextMessage> contextMessages, string userLastRequest)
        {
            var ret = SerializeConversationHistory(contextMessages);
            var sb = new StringBuilder(ret);

            sb.Append(WrapSection("user's latest request", userLastRequest));

            return sb.ToString();
        }

        public static string SerializeRequestAndContext(string requestContext, string requestText)
        {
            var sb = new StringBuilder();
            sb.Append(WrapSection("context", requestContext));
            sb.Append(WrapSection("userRequest", requestText));

            return sb.ToString();
        }

        private static string WrapSection(string label, string content)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format(SECTION_BEGIN_MARKER, label));
            sb.AppendLine(content);
            sb.AppendLine(string.Format(SECTION_END_MARKER, label));
            return sb.ToString();
        }
    }
}
