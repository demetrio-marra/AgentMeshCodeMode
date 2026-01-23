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
            Console.WriteLine($"  Provider: {configuration.Provider}");
            Console.WriteLine($"  Model: {configuration.ModelName}");
            Console.WriteLine($"  Temperature: {configuration.ModelTemperature}");
        }

        public static void WriteLineWithColor(string message, ConsoleColor color)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }

        public static void PrintTokenUsageSummary(Dictionary<string, int> inputTokenUsage, Dictionary<string, int> outputTokenUsage, Dictionary<string, decimal> agentInputCosts, Dictionary<string, decimal> agentOutputCosts)
        {
            if (inputTokenUsage.Count == 0 && outputTokenUsage.Count == 0)
            {
                return;
            }

            var allAgentNames = inputTokenUsage.Keys.Union(outputTokenUsage.Keys).OrderByDescending(name =>
                (inputTokenUsage.ContainsKey(name) ? inputTokenUsage[name] : 0) +
                (outputTokenUsage.ContainsKey(name) ? outputTokenUsage[name] : 0)
            );

            var totalInputTokens = inputTokenUsage.Values.Sum();
            var totalOutputTokens = outputTokenUsage.Values.Sum();
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

            foreach (var agentName in allAgentNames)
            {
                var inputTokens = inputTokenUsage.ContainsKey(agentName) ? inputTokenUsage[agentName] : 0;
                var outputTokens = outputTokenUsage.ContainsKey(agentName) ? outputTokenUsage[agentName] : 0;

                var inputTokensStr = inputTokens.ToString("N0").PadLeft(13);
                var outputTokensStr = outputTokens.ToString("N0").PadLeft(13);

                var inputPercentage = totalInputTokens > 0 ? (inputTokens * 100.0 / totalInputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);
                var outputPercentage = totalOutputTokens > 0 ? (outputTokens * 100.0 / totalOutputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);

                var inputCostPerMillion = agentInputCosts.ContainsKey(agentName) ? agentInputCosts[agentName] : 0m;
                var outputCostPerMillion = agentOutputCosts.ContainsKey(agentName) ? agentOutputCosts[agentName] : 0m;

                var inputCost = (inputTokens / 1_000_000m) * inputCostPerMillion;
                var outputCost = (outputTokens / 1_000_000m) * outputCostPerMillion;
                var totalAgentCost = inputCost + outputCost;

                totalInputCost += inputCost;
                totalOutputCost += outputCost;

                var inputCostStr = inputCost.ToString("F6").PadLeft(25);
                var outputCostStr = outputCost.ToString("F6").PadLeft(25);
                var totalAgentCostStr = totalAgentCost.ToString("F6").PadLeft(14);

                var agentNamePadded = agentName.PadRight(29);

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
