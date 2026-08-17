using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PersonalAssistantOpeningSentenceParameter : EWParameter<string>
    {
        public const string ParamName = "Personal assistant opening sentence";
        public PersonalAssistantOpeningSentenceParameter()
        {
            Name = ParamName;
        }
    }
}
