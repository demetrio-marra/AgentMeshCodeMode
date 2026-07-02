namespace AgentMesh.Models.CodeSmellDetector
{
    public class CodeSmellDetectorOutput
    {
        public bool Valid { get; set; }
        public string[] Feedbacks { get; set; } = [];

        public override string ToString()
        {
            return $"Valid: {Valid}\nFeedbacks: {string.Join(", ", Feedbacks)}";
        }
    }
}
