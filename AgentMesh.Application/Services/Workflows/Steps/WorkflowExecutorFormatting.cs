using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Application.Services.Workflows.Steps;

public static class WorkflowExecutorFormatting
{
    public static string GetElapsedTime(TimeSpan elapsed) => $"{elapsed.TotalMilliseconds:0}ms";

    public static Dictionary<string, string> ToDictionary(StructuredUserRequest structuredUserRequest)
    {
        return new Dictionary<string, string>
        {
            { "Intent", structuredUserRequest.Intent },
            { "Intent category", structuredUserRequest.IntentCategory.ToString() },
            { "Conversation topic", structuredUserRequest.ConversationTopic ?? string.Empty },
            { "User requested actions", structuredUserRequest.UserRequestedActions.Any() ? ToBulletList(structuredUserRequest.UserRequestedActions) : "(No actions)" },
            { "User provided data", structuredUserRequest.UserProvidedData.Any() ? ToBulletList(structuredUserRequest.UserProvidedData) : "(No data)" },
            { "User preferences", structuredUserRequest.UserPreferences.Any() ? ToBulletList(structuredUserRequest.UserPreferences) : "(No user preferences)" },
            { "Missing values", structuredUserRequest.MissingValues.Any() ? ToBulletList(structuredUserRequest.MissingValues) : "(No missing values)" },
            { "Language of the user", structuredUserRequest.LanguageOfTheUser }
        };
    }

    public static string ToBulletList<T>(IEnumerable<T> items)
        => string.Join("\n", items.Select(item => $"- {item}"));

    public static string SerializeDocumentation(IEnumerable<KnowledgeBaseDocumentContent> documents)
    {
        var serializedDocs = documents.Select(kv => $"{kv.Content}\n\nOriginal file: {kv.File}");
        return string.Join(Environment.NewLine + "---" + Environment.NewLine + "---", serializedDocs);
    }
}

