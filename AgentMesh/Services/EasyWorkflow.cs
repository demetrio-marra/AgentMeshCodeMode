using AgentMesh.Models.ChatMessages;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public class EasyWorkflow
    {
        private readonly EasyWorkflowParametersProvider _easyWorkflowParametersProvider;
        private readonly IEasyWorkflowStepSelector _easyWorkflowStepSelector;

        public EasyWorkflow(EasyWorkflowParametersProvider easyWorkflowParametersProvider, 
            IEasyWorkflowStepSelector easyWorkflowStepSelector)
        {
            _easyWorkflowParametersProvider = easyWorkflowParametersProvider;
            _easyWorkflowStepSelector = easyWorkflowStepSelector;
        }

        public async Task<WorkflowResultRecord> ExecuteAsync(string userInput, IEnumerable<ContextMessage> chatHistory)
        {
            // set userInput and chatHistory parameters first
            var userInputParameter = _easyWorkflowParametersProvider.GetUserCurrentRequestParameter();
            if (userInputParameter == null)
            {
                throw new InvalidOperationException("User input parameter is not defined.");
            }
            var conversationHistoryParameter = _easyWorkflowParametersProvider.GetConversationHistoryParameter();
            if (conversationHistoryParameter == null) 
            {
                throw new InvalidOperationException("Conversation history parameter is not defined.");
            }
            
            _easyWorkflowParametersProvider.SetParameterValue(userInputParameter.Value.Name, userInput);
            _easyWorkflowParametersProvider.SetParameterValue(conversationHistoryParameter.Value.Name, chatHistory);

            // 1. chiamo la funzione che istanzia i prossim step da eseguire
            var nextSteps = _easyWorkflowStepSelector.NextStepsToRun(_easyWorkflowParametersProvider.GetParameters());

            var workflowStepsStatistics = new List<WorkflowStepStatisticsRecord>();

            while (nextSteps.Any()) {

                // run steps in parallel
                var stepTasks = nextSteps.Select(step => RunStep(step)).ToList();
                var stepsStatistics = await Task.WhenAll(stepTasks);
                
                workflowStepsStatistics.AddRange(stepsStatistics);

                // 6. ritorno al punto 1 fino a che non ci sono più step da eseguire
                nextSteps = _easyWorkflowStepSelector.NextStepsToRun(_easyWorkflowParametersProvider.GetParameters());
            }

            // 7. ritorno il risultato finale
            var inputStepTokensCount = workflowStepsStatistics.First(s => s.IsInputStep).AgentTokenUsageEntry?.InputTokens ?? 0;
            var outputStepTokensCount = workflowStepsStatistics.First(s => s.IsOutputStep).AgentTokenUsageEntry?.OutputTokens ?? 0;
            var responseForUser = _easyWorkflowParametersProvider.GetResponseForUserParameter()?.RawValue ?? "(no response)";
            
            var ret = new WorkflowResultRecord
            {
                ResponseForUser = responseForUser,
                ContextSizeInTokens = inputStepTokensCount + outputStepTokensCount,
                Steps = workflowStepsStatistics
            };

            return ret;
        }


        private async Task<WorkflowStepStatisticsRecord> RunStep(IEasyWorkflowStep step)
        {
            // 2. estraggo i parametri di input che servono allo step
            var requiredParameterNames = step.RequiredParameterNames.Select(p => p.Name).ToList();
            var stepInputParameters = _easyWorkflowParametersProvider.GetParameters(requiredParameterNames);

            // TODO: invia al progress i parametri di input per la visualizzazione

            // 3. chiamo la funzione che esegue lo step e mi restituisce il risultato
            var parametersBeforeSnapshot = _easyWorkflowParametersProvider.GetParameters();
            var stepStartTime = DateTime.UtcNow;

            var stepResult = await step.ExecuteAsync(stepInputParameters);

            var stepEndTime = DateTime.UtcNow;
            var parametersAfterSnapshot = _easyWorkflowParametersProvider.GetParameters();

            // 4. aggiorno i parametri con il risultato dello step
            _easyWorkflowParametersProvider.SetParameters(stepResult.OutputParameters);
            // TODO: invia al progress i parametri aggiornati per la visualizzazione

            // 5. compilo le statistiche di esecuzione dello step (token usage, ecc.) e le salvo in un registro di esecuzione
            var stepStatistics = new WorkflowStepStatisticsRecord
            {
                StepName = step.GetType().Name,
                StartedOnUtc = stepStartTime,
                CompletedOnUtc = stepEndTime,
                IsAgentic = step.IsAgentic,
                ParametersBefore = parametersBeforeSnapshot,
                ParametersAfter = parametersAfterSnapshot,
                AgentTokenUsageEntry = stepResult.AgentTokenUsageEntry,
                IsInputStep = step.IsInputStep,
                IsOutputStep = step.IsOutputStep
            };

            return stepStatistics;
        }
    }
}
