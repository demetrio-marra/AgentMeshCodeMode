namespace AgentMesh.Models
{
    public class CodeSmellDetectorOutput
    {
        public bool Valid { get; set; }
        public string[] Feedbacks { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            return $"Valid: {Valid}\nFeedbacks: {string.Join(", ", Feedbacks)}";
        }
    }
}
