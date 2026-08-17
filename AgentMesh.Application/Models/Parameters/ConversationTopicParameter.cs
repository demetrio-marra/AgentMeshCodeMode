using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class ConversationTopicParameter : EWParameter<string>
    {
        public const string ParamName = "Conversation topic";
        public ConversationTopicParameter()
        {
            Name = ParamName;
        }
    }
}
