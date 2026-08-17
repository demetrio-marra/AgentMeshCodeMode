using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class LanguageOfTheUserParameter : EWParameter<string>
    {
        public const string ParamName = "Language of the user";
        public LanguageOfTheUserParameter()
        {
            Name = ParamName;
        }
    }
}
