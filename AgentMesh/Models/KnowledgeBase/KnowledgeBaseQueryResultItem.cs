namespace AgentMesh.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryResultItem : IEquatable<KnowledgeBaseQueryResultItem>
    {
        /// <summary>
        /// The unique identifier of the knowledge base entry.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The title of the knowledge base entry.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// A brief summary or description of the knowledge base entry.
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// The original documentation file name associated with the knowledge base entry, if available.
        /// </summary>
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// The relevance score of the knowledge base entry in relation to the search query, if available.
        /// </summary>
        public double? Relevance { get; set; }

        public bool Equals(KnowledgeBaseQueryResultItem? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Id, other.Id, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(Title, other.Title, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(Summary, other.Summary, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(File, other.File, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as KnowledgeBaseQueryResultItem);

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(Id),
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(Title),
                Summary is null ? 0 : StringComparer.InvariantCultureIgnoreCase.GetHashCode(Summary),
                StringComparer.InvariantCultureIgnoreCase.GetHashCode(File));
        }
    }
}
