using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class QMDQueryTypesDocumentationParameter : BaseEWParameterConfiguration<string>
    {
        public override string Name => "QMD query types documentation";

        protected override string? GetDefaultValue()
        {
            // return the content of the file QMDQueryTypes.md as a string
            var filePath = Path.Combine(AppContext.BaseDirectory, "Prompts/QMDQueryTypes.md");
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                return null;
            }
        }
    }
}
