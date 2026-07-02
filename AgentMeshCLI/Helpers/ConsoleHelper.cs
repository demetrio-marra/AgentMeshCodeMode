using AgentMesh.Models.Workflows;

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

        public static string ToHumanReadableDuration(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 1)
            {
                return elapsed.TotalMilliseconds > 0 ? "<1s" : "0s";
            }

            var totalSeconds = (int)Math.Round(elapsed.TotalSeconds, MidpointRounding.AwayFromZero);

            if (totalSeconds < 60)
            {
                return $"{totalSeconds}s";
            }

            if (totalSeconds < 3600)
            {
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                return $"{minutes}m{seconds}s";
            }

            var hours = totalSeconds / 3600;
            var remainingMinutes = (totalSeconds % 3600) / 60;
            var remainingSeconds = totalSeconds % 60;
            return $"{hours}h{remainingMinutes}m{remainingSeconds}s";
        }

        public static void PrintTokenUsageSummary(List<WorkflowStepUsageEntry> tokenUsageEntries, Dictionary<string, decimal> agentInputCosts, Dictionary<string, decimal> agentOutputCosts)
        {
            if (tokenUsageEntries.Count == 0)
            {
                return;
            }

            var totalElapsedWorkflow = TimeSpan.FromMilliseconds(tokenUsageEntries.Sum(e => e.Elapsed.TotalMilliseconds));

            var agenticEntries = tokenUsageEntries
                .Where(e => e.IsAgentic && e.TokensUsage is not null)
                .ToList();

            var totalInputTokens = agenticEntries.Sum(e => e.TokensUsage!.InputTokens);
            var totalOutputTokens = agenticEntries.Sum(e => e.TokensUsage!.OutputTokens);
            var totalInputCost = 0m;
            var totalOutputCost = 0m;

            Console.WriteLine();
            WriteLineWithColor("                                                   ╔═══════════════════════════════════════════════════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
            WriteLineWithColor("                                                   ║                                           TOKEN USAGE SUMMARY                                         ║", ConsoleColor.Cyan);
            WriteLineWithColor("╔══════════════════════════════════════╦═══════════╬═══════════════════════════════════════════════════╦═══════════════════════════════════════════════════╬════════════════╗", ConsoleColor.Cyan);
            WriteLineWithColor("║  Agent/Step                          ║ Elapsed   ║                   INPUT TOKENS                    ║                  OUTPUT TOKENS                    ║  TOTAL COST($) ║", ConsoleColor.Cyan);
            WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬═══════════════╦═══════════════╦═══════════════════╬═══════════════╦═══════════════╦═══════════════════╬════════════════╣", ConsoleColor.Cyan);
            WriteLineWithColor("║                                      ║           ║    Tokens     ║  Percentage   ║      Cost ($)     ║    Tokens     ║  Percentage   ║      Cost ($)     ║                ║", ConsoleColor.Cyan);
            WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬═══════════════╬═══════════════╬═══════════════════╬═══════════════╬═══════════════╬═══════════════════╬════════════════╣", ConsoleColor.Cyan);

            foreach (var entry in tokenUsageEntries)
            {
                var inputTokensStr = "-".PadLeft(13, ' ');
                var outputTokensStr = "-".PadLeft(13, ' ');
                var inputPercentage = "-".PadLeft(13, ' ');
                var outputPercentage = "-".PadLeft(13, ' ');
                var inputCostStr = "-".PadLeft(17, ' ');
                var outputCostStr = "-".PadLeft(17, ' ');
                var totalAgentCostStr = "-".PadLeft(14, ' ');

                if (entry.IsAgentic && entry.TokensUsage is not null)
                {
                    var tokensUsage = entry.TokensUsage;
                    inputTokensStr = tokensUsage.InputTokens.ToString("N0").PadLeft(13);
                    outputTokensStr = tokensUsage.OutputTokens.ToString("N0").PadLeft(13);

                    inputPercentage = totalInputTokens > 0 ? (tokensUsage.InputTokens * 100.0 / totalInputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);
                    outputPercentage = totalOutputTokens > 0 ? (tokensUsage.OutputTokens * 100.0 / totalOutputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);

                    var inputCostPerMillion = agentInputCosts.ContainsKey(tokensUsage.AgentName) ? agentInputCosts[tokensUsage.AgentName] : 0m;
                    var outputCostPerMillion = agentOutputCosts.ContainsKey(tokensUsage.AgentName) ? agentOutputCosts[tokensUsage.AgentName] : 0m;

                    var inputCost = (tokensUsage.InputTokens / 1_000_000m) * inputCostPerMillion;
                    var outputCost = (tokensUsage.OutputTokens / 1_000_000m) * outputCostPerMillion;
                    var totalAgentCost = inputCost + outputCost;

                    totalInputCost += inputCost;
                    totalOutputCost += outputCost;

                    inputCostStr = inputCost.ToString("F6").PadLeft(17);
                    outputCostStr = outputCost.ToString("F6").PadLeft(17);
                    totalAgentCostStr = totalAgentCost.ToString("F6").PadLeft(14);
                }

                var rowName = entry.StepName.Length > 36 ? entry.StepName[..36] : entry.StepName;
                var agentNamePadded = rowName.PadRight(36);
                var elapsedPadded = ToHumanReadableDuration(entry.Elapsed).PadLeft(9);

                WriteLineWithColor($"║ {agentNamePadded} ║ {elapsedPadded} ║ {inputTokensStr} ║ {inputPercentage} ║ {inputCostStr} ║ {outputTokensStr} ║ {outputPercentage} ║ {outputCostStr} ║ {totalAgentCostStr} ║", ConsoleColor.White);
            }

            var grandTotalCost = totalInputCost + totalOutputCost;
            var totalInputTokensStr = totalInputTokens.ToString("N0").PadLeft(13);
            var totalOutputTokensStr = totalOutputTokens.ToString("N0").PadLeft(13);
            var totalInputCostStr = totalInputCost.ToString("F6").PadLeft(17);
            var totalOutputCostStr = totalOutputCost.ToString("F6").PadLeft(17);
            var grandTotalCostStr = grandTotalCost.ToString("F6").PadLeft(14);

            WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬═══════════════╬═══════════════╬═══════════════════╬═══════════════╬═══════════════╬═══════════════════╬════════════════╣", ConsoleColor.Cyan);
            WriteLineWithColor($"║ TOTAL                                ║           ║ {totalInputTokensStr} ║               ║ {totalInputCostStr} ║ {totalOutputTokensStr} ║               ║ {totalOutputCostStr} ║ {grandTotalCostStr} ║", ConsoleColor.Yellow);
            WriteLineWithColor("╚══════════════════════════════════════╩═══════════╩═══════════════╩═══════════════╩═══════════════════╩═══════════════╩═══════════════╩═══════════════════╩════════════════╝", ConsoleColor.Cyan);
            WriteLineWithColor($"Total elapsed workflow time: {ToHumanReadableDuration(totalElapsedWorkflow)}", ConsoleColor.Yellow);
            Console.WriteLine();
        }
    }
}
