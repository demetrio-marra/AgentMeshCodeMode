using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface ISummarizationPipeline : IEWPipeline
    {
        string SummarizedContent { get; }
        DateTime SummarizedContentDatetime { get; }
        IEnumerable<ContextMessage> ChatMessagesToSummarize { set; }
    }
}
