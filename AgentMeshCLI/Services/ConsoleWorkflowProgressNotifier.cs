using AgentMesh.Helpers;
using AgentMesh.Models.Workflows;

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

        public async Task NotifyWorkflowStepCompleted(string stepName, EWStepStatisticsRecord statistics)
        {
            ConsoleHelper.WriteLineWithColor($"Workflow step '{stepName}' has completed.", ConsoleColor.Magenta);
            WriteParameter("StepName", statistics.StepName);
            WriteParameter("Elapsed", statistics.HumanReadableElapsed);

            var parametersDiff = statistics.ParametersDiff.ToList();
            var diffValue = parametersDiff.Count == 0
                ? "(No differences)"
                : string.Join('\n', parametersDiff.Select(p => $"- {p.Name}: '{p.OldValue ?? string.Empty}' -> '{p.NewValue ?? string.Empty}'"));

            WriteParameter("Parameters", diffValue);

            ConsoleHelper.WriteLineWithColor("══════════════════════════════════════════════════════════════════════════", ConsoleColor.Gray);

            await Task.CompletedTask;
        }

        private static void WriteParameter(string key, string value)
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
    }
}
