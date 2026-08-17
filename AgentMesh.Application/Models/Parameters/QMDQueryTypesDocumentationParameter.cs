using AgentMesh.Models;

namespace AgentMesh.Application.Models.Parameters
{
    public sealed class QMDQueryTypesDocumentationParameter : EWParameter<string>
    {
        public const string ParamName = "QMD query types documentation";
        private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

        public QMDQueryTypesDocumentationParameter()
        {
            Name = ParamName;
            ParameterValue = LoadDocumentationQueriesGenerationReference();
        }

        private static string? LoadDocumentationQueriesGenerationReference()
        {
            var candidatePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Prompts", QmdQueryTypesFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Prompts", QmdQueryTypesFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "AgentMeshCLI", "Prompts", QmdQueryTypesFileName)
            };

            foreach (var candidatePath in candidatePaths)
            {
                if (File.Exists(candidatePath))
                    return File.ReadAllText(candidatePath);
            }

            return null;
        }
    }
}
