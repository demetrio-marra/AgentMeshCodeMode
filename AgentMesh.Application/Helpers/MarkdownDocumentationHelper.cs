using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AgentMesh.Application.Helpers
{
    public static class MarkdownDocumentationHelper
    {
        /// <summary>
        /// Reads a markdown document and extracts the specified section along with its ancestor sections and their direct body text.
        /// </summary>
        /// <param name="markdownContent">The markdown content to search within.</param>
        /// <param name="sectionTitle">The title of the section to extract.</param>
        /// <returns>The extracted section along with its ancestor sections and their direct body text.</returns>
        /// <remarks>Implemented using Copilot</remarks>
        public static string GetMarkdownSection(string markdownContent, string sectionTitle)
        {
            if (string.IsNullOrWhiteSpace(markdownContent) || string.IsNullOrWhiteSpace(sectionTitle))
                return string.Empty;

            var cleanMarkdown = RemoveQMDSyntax(markdownContent);

            var lines = cleanMarkdown.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Find the target section and its heading level
            int targetLineIndex = -1;
            int targetLevel = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                int level = GetHeadingLevel(lines[i]);
                if (level > 0)
                {
                    var headingText = lines[i].TrimStart().Substring(level).Trim();
                    if (headingText.Equals(sectionTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        targetLineIndex = i;
                        targetLevel = level;
                        break;
                    }
                }
            }

            if (targetLineIndex < 0)
                return string.Empty;

            var result = new List<string>();
            int currentHeadingLevel = 0;

            // Collect ancestor sections and their direct body text (lines before the target)
            for (int i = 0; i < targetLineIndex; i++)
            {
                int level = GetHeadingLevel(lines[i]);
                if (level > 0)
                {
                    currentHeadingLevel = level;
                    if (level < targetLevel)
                        result.Add(lines[i]);
                    // skip sibling or deeper headings
                }
                else if (currentHeadingLevel < targetLevel)
                {
                    result.Add(lines[i]);
                }
            }

            // Collect the target section including all its subsections
            for (int i = targetLineIndex; i < lines.Length; i++)
            {
                if (i > targetLineIndex)
                {
                    int level = GetHeadingLevel(lines[i]);
                    if (level > 0 && level <= targetLevel)
                        break;
                }
                result.Add(lines[i]);
            }

            return string.Join(Environment.NewLine, result);
        }

        private static int GetHeadingLevel(string line)
        {
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("#"))
                return 0;

            int level = 0;
            while (level < trimmed.Length && trimmed[level] == '#')
                level++;

            // A valid markdown heading requires a space after the #'s
            if (level >= trimmed.Length || trimmed[level] != ' ')
                return 0;

            return level;
        }

        private static string RemoveQMDSyntax(string markdownContent)
        {
            // Remove any text between <!-- and --> tags, which may span multiple lines
            markdownContent = System.Text.RegularExpressions.Regex.Replace(markdownContent, "<!--.*?-->", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);

            // Remove any line numbers from text. The format is "\d+:"
            markdownContent = System.Text.RegularExpressions.Regex.Replace(markdownContent, @"^\d+:\s*", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);

            return markdownContent;
        }
    }
}
