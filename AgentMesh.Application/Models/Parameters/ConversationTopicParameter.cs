using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class ConversationTopicParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "Conversation topic";
    }
}
