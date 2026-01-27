using AgentMesh.Application.Models;

namespace AgentMesh.Helpers
{
    internal class ConsoleHelper
    {
        public static bool ConfirmStep(string prompt)
        {
            Console.Write($"{prompt} (press 'a' to abort, any other key to continue): ");
            var keyInfo = Console.ReadKey(intercept: true);
            Console.WriteLine();
            return keyInfo.Key != ConsoleKey.A;
        }

        public static void PrintAgentConfiguration(string friendlyName, string agentName, dynamic configuration)
        {
            Console.WriteLine($"- {friendlyName} ({agentName})");
            Console.WriteLine($"  LLM: {configuration.LLM}");
            Console.WriteLine($"  Temperature: {configuration.ModelTemperature}");
        }

        public static void WriteLineWithColor(string message, ConsoleColor color)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }

        public static void PrintTokenUsageSummary(List<AgentTokenUsageEntry> tokenUsageEntries, Dictionary<string, decimal> agentInputCosts, Dictionary<string, decimal> agentOutputCosts)
        {
            if (tokenUsageEntries.Count == 0)
            {
                return;
            }

            var totalInputTokens = tokenUsageEntries.Sum(e => e.InputTokens);
            var totalOutputTokens = tokenUsageEntries.Sum(e => e.OutputTokens);
            var totalInputCost = 0m;
            var totalOutputCost = 0m;

            Console.WriteLine();
            WriteLineWithColor("╔═══════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
            WriteLineWithColor("║                                                              TOKEN USAGE SUMMARY                                                                      ║", ConsoleColor.Cyan);
            WriteLineWithColor("╠═══════════════════════════════╦═══════════════════════════════════════════════════════════╦═══════════════════════════════════════════════════════════╬════════════════╗", ConsoleColor.Cyan);
            WriteLineWithColor("║                               ║                   INPUT TOKENS                            ║                  OUTPUT TOKENS                            ║                ║", ConsoleColor.Cyan);
            WriteLineWithColor("║  Agent                        ╠═══════════════╦═══════════════╦═══════════════════════════╬═══════════════╦═══════════════╦═══════════════════════════╣  TOTAL COST($) ║", ConsoleColor.Cyan);
            WriteLineWithColor("║                               ║    Tokens     ║  Percentage   ║      Cost ($)             ║    Tokens     ║  Percentage   ║      Cost ($)             ║                ║", ConsoleColor.Cyan);
            WriteLineWithColor("╠═══════════════════════════════╬═══════════════╬═══════════════╬═══════════════════════════╬═══════════════╬═══════════════╬═══════════════════════════╬════════════════╣", ConsoleColor.Cyan);

            foreach (var entry in tokenUsageEntries)
            {
                var inputTokensStr = entry.InputTokens.ToString("N0").PadLeft(13);
                var outputTokensStr = entry.OutputTokens.ToString("N0").PadLeft(13);

                var inputPercentage = totalInputTokens > 0 ? (entry.InputTokens * 100.0 / totalInputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);
                var outputPercentage = totalOutputTokens > 0 ? (entry.OutputTokens * 100.0 / totalOutputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);

                var inputCostPerMillion = agentInputCosts.ContainsKey(entry.AgentName) ? agentInputCosts[entry.AgentName] : 0m;
                var outputCostPerMillion = agentOutputCosts.ContainsKey(entry.AgentName) ? agentOutputCosts[entry.AgentName] : 0m;

                var inputCost = (entry.InputTokens / 1_000_000m) * inputCostPerMillion;
                var outputCost = (entry.OutputTokens / 1_000_000m) * outputCostPerMillion;
                var totalAgentCost = inputCost + outputCost;

                totalInputCost += inputCost;
                totalOutputCost += outputCost;

                var inputCostStr = inputCost.ToString("F6").PadLeft(25);
                var outputCostStr = outputCost.ToString("F6").PadLeft(25);
                var totalAgentCostStr = totalAgentCost.ToString("F6").PadLeft(14);

                var agentNamePadded = entry.AgentName.PadRight(29);

                WriteLineWithColor($"║ {agentNamePadded} ║ {inputTokensStr} ║ {inputPercentage} ║ {inputCostStr} ║ {outputTokensStr} ║ {outputPercentage} ║ {outputCostStr} ║ {totalAgentCostStr} ║", ConsoleColor.White);
            }

            var grandTotalCost = totalInputCost + totalOutputCost;
            var totalInputTokensStr = totalInputTokens.ToString("N0").PadLeft(13);
            var totalOutputTokensStr = totalOutputTokens.ToString("N0").PadLeft(13);
            var totalInputCostStr = totalInputCost.ToString("F6").PadLeft(25);
            var totalOutputCostStr = totalOutputCost.ToString("F6").PadLeft(25);
            var grandTotalCostStr = grandTotalCost.ToString("F6").PadLeft(14);

            WriteLineWithColor("╠═══════════════════════════════╬═══════════════╬═══════════════╬═══════════════════════════╬═══════════════╬═══════════════╬═══════════════════════════╬════════════════╣", ConsoleColor.Cyan);
            WriteLineWithColor($"║ TOTAL                         ║ {totalInputTokensStr} ║               ║ {totalInputCostStr} ║ {totalOutputTokensStr} ║               ║ {totalOutputCostStr} ║ {grandTotalCostStr} ║", ConsoleColor.Yellow);
            WriteLineWithColor("╚═══════════════════════════════╩═══════════════╩═══════════════╩═══════════════════════════╩═══════════════╩═══════════════╩═══════════════════════════╩════════════════╝", ConsoleColor.Cyan);
            Console.WriteLine();
        }
    }
}
