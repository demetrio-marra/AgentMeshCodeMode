using AgentMesh.Services;
using AgentMesh.Application.Models;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Services.Executors;

public static class WorkflowExecutorFormatting
{
    public static string GetElapsedTime(TimeSpan elapsed) => $"{elapsed.TotalMilliseconds:0}ms";

    public static string ToBulletList<T>(IEnumerable<T> items)
        => string.Join("\n", items.Select(item => $"- {item}"));

    public static string SerializeDocumentation(IEnumerable<KnowledgeBaseDocumentContent> documents)
    {
        var serializedDocs = documents.Select(kv => $"{kv.Content}\n\nOriginal file: {kv.File}");
        return string.Join(Environment.NewLine + "---" + Environment.NewLine + "---", serializedDocs);
    }
}

