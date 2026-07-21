namespace AgentMesh.Utils
{
    public static class ListsFormatter
    {
        public static string ToBulletList<T>(IEnumerable<T> items) => string.Join("\n", items.Select(item => $"- {item}"));
    }
}
