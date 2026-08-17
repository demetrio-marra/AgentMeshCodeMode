using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PersonalAssistantClosingSentenceParameter : EWParameter<string>
    {
        public const string ParamName = "Personal assistant closing sentence";
        public PersonalAssistantClosingSentenceParameter()
        {
            Name = ParamName;
        }
    }
}
