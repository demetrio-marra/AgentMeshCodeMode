using System.Text;

namespace AgentMesh.Application
{
    public class UserMessageBuilder
    {
        public static string BuildUserMessageString(string requestContext, string requestText)
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

        public static string AddAdditionalDataToUserMessageString(string existingUserMessage, string additionalDataLabel, string additionalDataContent)
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
