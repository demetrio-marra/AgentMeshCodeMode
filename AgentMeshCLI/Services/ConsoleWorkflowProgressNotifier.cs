using AgentMesh.Helpers;
using AgentMesh.Models.Workflows;
using System.ComponentModel;
using System.Numerics;

namespace AgentMesh.Services
{
    internal class ConsoleWorkflowProgressNotifier : IWorkflowProgressNotifier
    {
        public async Task NotifyWorkflowEnd()
        {
            ConsoleHelper.WriteLineWithColor("\nWorkflow has completed successfully.", ConsoleColor.Gray);
            await Task.CompletedTask;
        }

        public async Task NotifyWorkflowStart()
        {
            ConsoleHelper.WriteLineWithColor("\nWorkflow has started.", ConsoleColor.Gray);
            await Task.CompletedTask;
        }

        public async Task NotifyWorkflowStepStarted(string stepName)
        {
            ConsoleHelper.WriteLineWithColor($"Workflow step '{stepName}' has started.", ConsoleColor.DarkGray);
            await Task.CompletedTask;
        }

        public async Task NotifyWorkflowStepCompleted(string stepName, EWStepStatisticsRecord statistics)
        {
            ConsoleHelper.WriteLineWithColor($"Workflow step '{stepName}' has completed.", ConsoleColor.Yellow);
            WriteExecStatistic("StepName", statistics.StepName);
            WriteExecStatistic("Elapsed", statistics.HumanReadableElapsed);

            var parametersDiff = statistics.ParametersDiff.ToList();

            List<TextOrConsoleColor> parametersTextOrConsoleColor = parametersDiff.Count == 0 ? [new TextOrConsoleColor { Color = ConsoleColor.White, Text = "(No differences)" }] :
                [.. parametersDiff.SelectMany(p => new List<TextOrConsoleColor>
                {
                    new() { Color = ConsoleColor.White, Text = p.Name },
                    new() { Color = ConsoleColor.Magenta, Text = $"{p.OldValue ?? string.Empty}" },
                    new() { Color = ConsoleColor.Green, Text = $"{p.NewValue ?? string.Empty}" },
                    new() { Color = ConsoleColor.Gray, Text = new string('-', 9) }
                })];


            WriteParameters(parametersTextOrConsoleColor);

            ConsoleHelper.WriteLineWithColor("══════════════════════════════════════════════════════════════════════════", ConsoleColor.Yellow);

            await Task.CompletedTask;
        }

        private static void WriteParameters(IEnumerable<TextOrConsoleColor> textElements)
        {
            var paramPadding = "Params".Length + 2;

            ConsoleHelper.WriteWithColor($"Params: ", ConsoleColor.DarkYellow);
            
            // do not pad first element only
            bool isFirstElement = true;
            foreach (var element in textElements)
            {
                var paddedLines = element.Text?.Split('\n');
                List<string> allPaddedLines;
                
                if (isFirstElement)
                {
                    allPaddedLines =  [paddedLines[0], .. paddedLines.Skip(1).Select(line => new string(' ', paramPadding) + line)];
                    isFirstElement = false;
                }
                else
                {
                    allPaddedLines =  paddedLines.Select(line => new string(' ', paramPadding) + line).ToList();
                }

                foreach (var paddedLine in allPaddedLines)
                {
                    if (element.Color.HasValue)
                    {
                        ConsoleHelper.WriteLineWithColor(paddedLine, element.Color.Value);
                    }
                    else
                    {
                        Console.WriteLine(paddedLine);
                    }
                }
            }
        }

        private static void WriteExecStatistic(string key, string value)
        {
            var lines = value.Split('\n');
            var paramPadding = key.Length + 2;

            ConsoleHelper.WriteWithColor($"{key}: ", ConsoleColor.DarkYellow);
            ConsoleHelper.WriteLineWithColor(lines[0], ConsoleColor.White);

            if (lines.Length <= 1)
            {
                return;
            }

            var paddedLines = lines.Skip(1).Select(line => new string(' ', paramPadding) + line);
            foreach (var paddedLine in paddedLines)
            {
                ConsoleHelper.WriteLineWithColor(paddedLine, ConsoleColor.White);
            }
        }


        internal class TextOrConsoleColor
        {
            public string? Text { get; init; }
            public ConsoleColor? Color { get; init; }
        }
    }
}
