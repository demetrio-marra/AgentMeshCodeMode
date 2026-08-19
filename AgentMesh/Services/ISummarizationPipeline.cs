using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface ISummarizationPipeline : IEWPipeline
    {
        string SummarizedContent { get; }
        DateTime SummarizedContentDatetime { get; }

        void SetParameterInitialValues(string summarizationLanguage, IEnumerable<ContextMessage> chatMessagesToSummarize, DateTime requestDateTime);
    }
}
