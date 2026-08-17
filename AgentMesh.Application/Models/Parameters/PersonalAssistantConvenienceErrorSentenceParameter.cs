using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class PersonalAssistantConvenienceErrorSentenceParameter : EWParameter<string>
    {
        public const string ParamName = "Personal assistant convenience error sentence";
        public PersonalAssistantConvenienceErrorSentenceParameter()
        {
            Name = ParamName;
        }
    }
}
