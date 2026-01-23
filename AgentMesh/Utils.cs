using System.Text.RegularExpressions;

namespace AgentMesh
{
    public class Utils
    {
        public static readonly Regex SectionRegex = new Regex(@"BEGIN_(?<section>[A-Z_]+)\s*\r?\n(?<content>[\s\S]*?)\r?\n\s*END_\k<section>", RegexOptions.Compiled | RegexOptions.Multiline);
    }
}
