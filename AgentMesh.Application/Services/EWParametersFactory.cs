using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class EWParametersFactory : IEWParametersFactory
    {
        public IEnumerable<IEWParameter> CreateParameters()
        {
            return new List<IEWParameter>
            {
                new UserLastRequestParameter(),
                new InitialContextMessagesParameter(),
                new UserIntentParameter(),
                new IntentCategoryParameter(),
                new LanguageOfTheUserParameter(),
                new ConversationTopicParameter(),
                new UserPreferencesParameter(),
                new UserProvidedDataParameter(),
                new UserRequestedActionsParameter(),
                new MissingValuesParameter(),
                new KnowledgeBaseAPIDocumentsContentParameter(),
                new PastMemoriesQueryParameter(),
                new DomainsKnowledgeBaseQueryParameter(),
                new PastMemoriesQueryResultsParameter(),
                new KnowledgeBaseQueryResultsParameter(),
                new DomainsKnowledgeBaseDocumentsContentParameter(),
                new BusinessRequirementsParameter(),
                new FunctionalAnalystRejectedParameter(),
                new FunctionalAnalystRejectReasonsParameter(),
                new TechnicalSpecificationParameter(),
                new TechnicalAnalystRejectedParameter(),
                new TechnicalAnalystRejectReasonsParameter(),
                new ShouldEngageCoderParameter(),
                new APISKnowledgeBaseQueryResultsParameter(),
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
