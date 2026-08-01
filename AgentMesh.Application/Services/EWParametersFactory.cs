using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Workflows.ParameterSerializers;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class EWParametersFactory(DisplayValuesEWParameterSerializer displayValuesParameterSerializer) : IEWParametersFactory
    {
        private readonly DisplayValuesEWParameterSerializer _displayValuesParameterSerializer = displayValuesParameterSerializer;

        public IEnumerable<IEWParameter> CreateParameters()
        {
            return new List<IEWParameter>
            {
                new UserLastRequestParameter(),
                new InitialContextMessagesParameter(_displayValuesParameterSerializer),
                new UserIntentParameter(),
                new IntentCategoryParameter(),
                new LanguageOfTheUserParameter(),
                new ConversationTopicParameter(),
                new UserPreferencesParameter(),
                new UserProvidedDataParameter(),
                new UserRequestedActionsParameter(),
                new MissingValuesParameter(),
                new KnowledgeBaseAPIDocumentsContentParameter(_displayValuesParameterSerializer),
                new PastMemoriesQueryParameter(_displayValuesParameterSerializer),
                new DomainsKnowledgeBaseQueryParameter(_displayValuesParameterSerializer),
                new PastMemoriesQueryResultsParameter(_displayValuesParameterSerializer),
                new KnowledgeBaseQueryResultsParameter(_displayValuesParameterSerializer),
                new DomainsKnowledgeBaseDocumentsContentParameter(_displayValuesParameterSerializer),
                new BusinessRequirementsParameter(),
                new FunctionalAnalystRejectedParameter(),
                new FunctionalAnalystRejectReasonsParameter(),
                new TechnicalSpecificationParameter(),
                new TechnicalAnalystRejectedParameter(),
                new TechnicalAnalystRejectReasonsParameter(),
                new ShouldEngageCoderParameter(),
                new APISKnowledgeBaseQueryResultsParameter(_displayValuesParameterSerializer),
                new SelectedAPIsFileLocationsParameter(),
                new DocumentationContentParameter(),
                new GeneratedCodeParameter(),
                new LastCodeWithLineNumbersParameter(),
                new CodeExecutionFailuresDetectorIterationCountParameter(),
                new CodeExecutionAnalysisParameter(),
                new SandboxResultParameter(),
                new SandboxExecutionIdParameter(),
                new CodeExecutionResultTypeParameter(),
                new ExecutionErrorParameter(),
                new DomainExpertOutputParameter(),
                new PersonalAssistantOpeningSentenceParameter(),
                new PersonalAssistantClosingSentenceParameter(),
                new PersonalAssistantConvenienceErrorSentenceParameter(),
                new FinalAnswerParameter()
            };
        }
    }
}
