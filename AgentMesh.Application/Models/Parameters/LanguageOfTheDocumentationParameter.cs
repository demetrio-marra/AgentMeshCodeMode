using AgentMesh.Application.Configuration;
using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class LanguageOfTheDocumentationParameter : EWParameter<string>
    {
        public const string ParamName = "Language the documentation is written in";
        public LanguageOfTheDocumentationParameter(CodeModeWorkflowConfiguration codeModeWorkflowConfiguration)
        {
            Name = ParamName;
            ParameterValue = codeModeWorkflowConfiguration.LanguageOfKnowledgeBase;
        }
    }
}
