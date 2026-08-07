using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Coder;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class CoderEWStep(
        CoderAgent coderAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Coder";

        public bool IsAgentic => true;

        public string? AgentName => CoderAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.BusinessRequirements,
            EWParameterNames.TechnicalSpecification,
            EWParameterNames.SelectedAPIsFileLocations,
            EWParameterNames.KnowledgeBaseAPIDocumentsContent
        ];

        private readonly CoderAgent _coderAgent = coderAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var requirementsParameter = inputParameters.Single(p => p.Name == EWParameterNames.BusinessRequirements);
            if (requirementsParameter is not BusinessRequirementsParameter typedRequirements)
                throw new InvalidOperationException($"Parameter {EWParameterNames.BusinessRequirements} is not of type BusinessRequirementsParameter");

            var specParameter = inputParameters.Single(p => p.Name == EWParameterNames.TechnicalSpecification);
            if (specParameter is not TechnicalSpecificationParameter typedSpec)
                throw new InvalidOperationException($"Parameter {EWParameterNames.TechnicalSpecification} is not of type TechnicalSpecificationParameter");

            var selectedApisParameter = inputParameters.Single(p => p.Name == EWParameterNames.SelectedAPIsFileLocations);
            if (selectedApisParameter is not SelectedAPIsFileLocationsParameter typedSelectedApis)
                throw new InvalidOperationException($"Parameter {EWParameterNames.SelectedAPIsFileLocations} is not of type SelectedAPIsFileLocationsParameter");

            var apiDocsParameter = inputParameters.Single(p => p.Name == EWParameterNames.KnowledgeBaseAPIDocumentsContent);
            if (apiDocsParameter is not KnowledgeBaseAPIDocumentsContentParameter typedApiDocs)
                throw new InvalidOperationException($"Parameter {EWParameterNames.KnowledgeBaseAPIDocumentsContent} is not of type KnowledgeBaseAPIDocumentsContentParameter");

            var docsToPass = (typedApiDocs.ParameterValue ?? []).Select(doc => new KnowledgeBaseGetDocsOutputItem
            {
                File = doc.File,
                Content = doc.Content
            });

            var agentInput = new CoderAgentInput
            {
                BusinessRequirements = typedRequirements.ParameterValue ?? "(No business requirements)",
                TechnicalSpecification = typedSpec.ParameterValue ?? "(No technical specification)",
                SelectedAPIsFileLocations = typedSelectedApis.ParameterValue ?? [],
                KnowledgeBaseAPIDocumentsContent = docsToPass
            };

            var agentOutput = await _coderAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.GeneratedCode, agentOutput.CodeToRun);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
