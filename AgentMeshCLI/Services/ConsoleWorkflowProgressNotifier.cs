using AgentMesh.Helpers;

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

        public async Task NotifyWorkflowStepEnd(string stepName, Dictionary<string, string> outputParameters)
        {
            ConsoleHelper.WriteLineWithColor($"Workflow step '{stepName}' has completed.", ConsoleColor.Magenta);

            foreach (var output in outputParameters)
            {
                WriteParameter(output.Key, output.Value);
            }

            ConsoleHelper.WriteLineWithColor("══════════════════════════════════════════════════════════════════════════", ConsoleColor.Gray);

            await Task.CompletedTask;
        }

        public async Task NotifyWorkflowStepStart(string stepName, Dictionary<string, string> inputParameters)
        {
            ConsoleHelper.WriteLineWithColor($"\nWorkflow step '{stepName}' has started.", ConsoleColor.Green);
            foreach (var input in inputParameters)
            {
                WriteParameter(input.Key, input.Value);
            }
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
