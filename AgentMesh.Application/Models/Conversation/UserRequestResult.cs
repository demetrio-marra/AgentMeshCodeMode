namespace AgentMesh.Application.Models.Conversation
{
    public readonly record struct UserRequestResult
    {
        public string? OpeningSentence { get; init; }
        public string? ClosingSentence { get; init; }
        public string? ConvenienceErrorSentence { get; init; }
    }
}
