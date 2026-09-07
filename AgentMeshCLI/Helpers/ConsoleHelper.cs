using AgentMesh.Application.Models.Costs;
using AgentMesh.Models;

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

        public static void PrintAgentConfiguration(string agentName, string LLM, double temperature)
        {
            Console.WriteLine($"{agentName,30}     LLM: {LLM,10}    Temperature: {temperature,5}");
        }

        public static void WriteWithColor(string text, ConsoleColor color)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = previousColor;
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

        public static void PrintTokenUsageSummary(IEnumerable<EWStepStatisticsRecord> tokenUsageEntries,
            IEnumerable<AgentExecutionCost> costsData)
        {
            var entries = tokenUsageEntries.ToList();

            if (entries.Count == 0)
            {
                return;
            }

            var totalElapsedWorkflow = TimeSpan.FromMilliseconds(entries.Sum(e => e.Elapsed.TotalMilliseconds));

            var costsByAgent = costsData
                .GroupBy(c => c.AgentName)
                .ToDictionary(g => g.Key, g => new Queue<AgentExecutionCost>(g));

            var tokenRows = new List<(EWStepStatisticsRecord Step, AgentExecutionCost? Cost)>();
            var hourlyRows = new List<(EWStepStatisticsRecord Step, AgentExecutionCost Cost)>();

            foreach (var entry in entries)
            {
                if (!entry.IsAgentic)
                {
                    tokenRows.Add((entry, null));
                    continue;
                }

                AgentExecutionCost? matchingCost = null;
                var agentName = entry.AgentName ?? string.Empty;
                if (costsByAgent.TryGetValue(agentName, out var queue) && queue.Count > 0)
                {
                    matchingCost = queue.Dequeue();
                }

                if (matchingCost.HasValue && matchingCost.Value.IsHourlyCost)
                {
                    hourlyRows.Add((entry, matchingCost.Value));
                }
                else
                {
                    tokenRows.Add((entry, matchingCost));
                }
            }

            var tokenAgenticRows = tokenRows.Where(r => r.Step.IsAgentic).ToList();
            var totalInputTokens = tokenAgenticRows.Sum(r => r.Step.InputTokens ?? 0);
            var totalOutputTokens = tokenAgenticRows.Sum(r => r.Step.OutputTokens ?? 0);
            var totalInputCost = tokenAgenticRows.Sum(r => r.Cost?.InputCost ?? 0m);
            var totalOutputCost = tokenAgenticRows.Sum(r => r.Cost?.OutputCost ?? 0m);
            var totalHourlyCost = hourlyRows.Sum(r => r.Cost.TotalCost);

            Console.WriteLine();
            WriteLineWithColor("                                                   ╔═══════════════════════════════════════════════════════════════════════════════════════════════════════╗", ConsoleColor.Gray);
            WriteLineWithColor("                                                   ║                                           TOKEN USAGE SUMMARY                                         ║", ConsoleColor.Gray);
            WriteLineWithColor("╔══════════════════════════════════════╦═══════════╬═══════════════════════════════════════════════════╦═══════════════════════════════════════════════════╬════════════════╗", ConsoleColor.Gray);
            WriteLineWithColor("║  Agent/Step                          ║ Elapsed   ║                   INPUT TOKENS                    ║                  OUTPUT TOKENS                    ║  TOTAL COST($) ║", ConsoleColor.Gray);
            WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬═══════════════╦═══════════════╦═══════════════════╬═══════════════╦═══════════════╦═══════════════════╬════════════════╣", ConsoleColor.Gray);
            WriteLineWithColor("║                                      ║           ║    Tokens     ║  Percentage   ║      Cost ($)     ║    Tokens     ║  Percentage   ║      Cost ($)     ║                ║", ConsoleColor.Gray);
            WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬═══════════════╬═══════════════╬═══════════════════╬═══════════════╬═══════════════╬═══════════════════╬════════════════╣", ConsoleColor.Gray);

            foreach (var row in tokenRows)
            {
                var entry = row.Step;

                var inputTokensStr = "-".PadLeft(13, ' ');
                var outputTokensStr = "-".PadLeft(13, ' ');
                var inputPercentage = "-".PadLeft(13, ' ');
                var outputPercentage = "-".PadLeft(13, ' ');
                var inputCostStr = "-".PadLeft(17, ' ');
                var outputCostStr = "-".PadLeft(17, ' ');
                var totalAgentCostStr = "-".PadLeft(14, ' ');

                if (entry.IsAgentic)
                {
                    var inputTokens = entry.InputTokens ?? 0;
                    var outputTokens = entry.OutputTokens ?? 0;

                    inputTokensStr = inputTokens.ToString("N0").PadLeft(13);
                    outputTokensStr = outputTokens.ToString("N0").PadLeft(13);
                    inputPercentage = totalInputTokens > 0 ? (inputTokens * 100.0 / totalInputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);
                    outputPercentage = totalOutputTokens > 0 ? (outputTokens * 100.0 / totalOutputTokens).ToString("F2").PadLeft(13) : "0.00".PadLeft(13);

                    if (row.Cost.HasValue)
                    {
                        inputCostStr = row.Cost.Value.InputCost.ToString("F6").PadLeft(17);
                        outputCostStr = row.Cost.Value.OutputCost.ToString("F6").PadLeft(17);
                        totalAgentCostStr = row.Cost.Value.TotalCost.ToString("F6").PadLeft(14);
                    }
                }

                var rowName = entry.StepName.Length > 36 ? entry.StepName[..36] : entry.StepName;
                var agentNamePadded = rowName.PadRight(36);
                var elapsedPadded = ToHumanReadableDuration(entry.Elapsed).PadLeft(9);

                WriteLineWithColor($"║ {agentNamePadded} ║ {elapsedPadded} ║ {inputTokensStr} ║ {inputPercentage} ║ {inputCostStr} ║ {outputTokensStr} ║ {outputPercentage} ║ {outputCostStr} ║ {totalAgentCostStr} ║", ConsoleColor.White);
            }

            var tokenTableTotalCost = totalInputCost + totalOutputCost;
            var totalInputTokensStr = totalInputTokens.ToString("N0").PadLeft(13);
            var totalOutputTokensStr = totalOutputTokens.ToString("N0").PadLeft(13);
            var totalInputCostStr = totalInputCost.ToString("F6").PadLeft(17);
            var totalOutputCostStr = totalOutputCost.ToString("F6").PadLeft(17);
            var tokenTableTotalCostStr = tokenTableTotalCost.ToString("F6").PadLeft(14);

            WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬═══════════════╬═══════════════╬═══════════════════╬═══════════════╬═══════════════╬═══════════════════╬════════════════╣", ConsoleColor.Gray);
            WriteLineWithColor($"║ TOTAL (TOKEN-BASED)                  ║           ║ {totalInputTokensStr} ║               ║ {totalInputCostStr} ║ {totalOutputTokensStr} ║               ║ {totalOutputCostStr} ║ {tokenTableTotalCostStr} ║", ConsoleColor.Yellow);
            WriteLineWithColor("╚══════════════════════════════════════╩═══════════╩═══════════════╩═══════════════╩═══════════════════╩═══════════════╩═══════════════╩═══════════════════╩════════════════╝", ConsoleColor.Gray);

            if (hourlyRows.Count > 0)
            {
                WriteLineWithColor("", ConsoleColor.White);
                WriteLineWithColor("                                       ╔═══════════════════════════════════════════════╗", ConsoleColor.Gray);
                WriteLineWithColor("                                       ║             HOURLY COST SUMMARY               ║", ConsoleColor.Gray);
                WriteLineWithColor("╔══════════════════════════════════════╬═══════════╦══════════════════╦════════════════╣", ConsoleColor.Gray);
                WriteLineWithColor("║  Agent/Step                          ║ Elapsed   ║ Cost/Hour ($)    ║  TOTAL COST($) ║", ConsoleColor.Gray);
                WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬══════════════════╬════════════════╣", ConsoleColor.Gray);

                foreach (var row in hourlyRows)
                {
                    var rowName = row.Step.StepName.Length > 36 ? row.Step.StepName[..36] : row.Step.StepName;
                    var agentNamePadded = rowName.PadRight(36);
                    var elapsedPadded = ToHumanReadableDuration(row.Step.Elapsed).PadLeft(9);
                    var costPerHourStr = (row.Cost.CostPerHour ?? 0m).ToString("F6").PadLeft(16);
                    var totalCostStr = row.Cost.TotalCost.ToString("F6").PadLeft(14);

                    WriteLineWithColor($"║ {agentNamePadded} ║ {elapsedPadded} ║ {costPerHourStr} ║ {totalCostStr} ║", ConsoleColor.White);
                }

                var totalHourlyCostStr = totalHourlyCost.ToString("F6").PadLeft(14);
                WriteLineWithColor("╠══════════════════════════════════════╬═══════════╬══════════════════╬════════════════╣", ConsoleColor.Gray);
                WriteLineWithColor($"║ TOTAL (HOURLY-BASED)                 ║           ║                  ║ {totalHourlyCostStr} ║", ConsoleColor.Yellow);
                WriteLineWithColor("╚══════════════════════════════════════╩═══════════╩══════════════════╩════════════════╝", ConsoleColor.Gray);
            }

            var grandTotalCost = tokenTableTotalCost + totalHourlyCost;
            WriteLineWithColor($"Grand total cost: {grandTotalCost:F6} $", ConsoleColor.Yellow);
            WriteLineWithColor($"Total elapsed workflow time: {ToHumanReadableDuration(totalElapsedWorkflow)}", ConsoleColor.Yellow);
            Console.WriteLine();
        }
    }
}
