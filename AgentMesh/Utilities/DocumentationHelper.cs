using System.Text;

namespace AgentMesh.Utilities
{
    public class DocumentationHelper
    {
        /// <summary>
        /// Extracts the content under the top-level title section (<c>#</c>) and the content of
        /// the <c>## Documentation for &lt;section&gt;</c> sub-section, then concatenates them.
        /// </summary>
        public static string ExtractFor(string documentationFileContent, string section)
        {
            if (string.IsNullOrWhiteSpace(documentationFileContent))
                return string.Empty;

            var titleHeading = "# ";
            var sectionHeading = $"## Documentation for {section}";

            var titleContent = new StringBuilder();
            var sectionContent = new StringBuilder();

            // State: 0 = before any heading, 1 = inside title section, 2 = inside target ## section
            int state = 0;
            bool titleDone = false;
            bool sectionDone = false;
            bool inCodeBlock = false;

            using var reader = new StringReader(documentationFileContent);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                // Track fenced code blocks (```) to avoid treating code lines as headings
                if (line.TrimStart().StartsWith("```"))
                    inCodeBlock = !inCodeBlock;

                if (state == 0)
                {
                    if (!inCodeBlock)
                    {
                        // Look for the top-level title (single #, not ##)
                        if (line.StartsWith(titleHeading) && !line.StartsWith("## "))
                        {
                            state = 1;
                            continue; // skip the heading line itself
                        }

                        // While title not yet found, also watch for the target ## section directly
                        if (!titleDone && line.TrimEnd().Equals(sectionHeading, StringComparison.OrdinalIgnoreCase))
                        {
                            state = 2;
                        }
                    }
                    continue;
                }

                if (state == 1)
                {
                    // Inside the title section: collect content directly under #, stop at any heading
                    if (!inCodeBlock && line.StartsWith("#"))
                    {
                        titleDone = true;
                        state = 0;

                        // The new heading may already be our target section
                        if (line.TrimEnd().Equals(sectionHeading, StringComparison.OrdinalIgnoreCase))
                        {
                            state = 2;
                        }
                        continue;
                    }
                    titleContent.AppendLine(line);
                    continue;
                }

                if (state == 2)
                {
                    // Inside the target ## section: stop only at same level (##) or higher (#),
                    // sub-headings (###, ####, ...) are part of the section content
                    if (!inCodeBlock)
                    {
                        var headingLevel = GetHeadingLevel(line);
                        if (headingLevel > 0 && headingLevel <= 2)
                        {
                            sectionDone = true;
                            break;
                        }
                    }
                    sectionContent.AppendLine(line);
                    continue;
                }

                // state == 0 after title: scan for the target section heading
                if (!inCodeBlock && titleDone && !sectionDone)
                {
                    if (line.TrimEnd().Equals(sectionHeading, StringComparison.OrdinalIgnoreCase))
                    {
                        state = 2;
                    }
                }
            }

            var result = new StringBuilder();

            var titleText = titleContent.ToString().Trim();
            if (titleText.Length > 0)
                result.Append(titleText);

            var sectionText = sectionContent.ToString().Trim();
            if (sectionText.Length > 0)
            {
                if (result.Length > 0)
                    result.AppendLine().AppendLine();
                result.Append(sectionText);
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns the heading level (number of leading <c>#</c> chars) of a markdown heading line,
        /// or 0 if the line is not a valid heading (must be followed by a space).
        /// </summary>
        private static int GetHeadingLevel(string line)
        {
            int level = 0;
            while (level < line.Length && line[level] == '#')
                level++;
            if (level > 0 && level < line.Length && line[level] == ' ')
                return level;
            return 0;
        }
    }
}
