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
                var lines = output.Value.Split('\n');
                var paramPadding = output.Key.Length + 2;
                
                if (lines.Length == 1)
                {
                    ConsoleHelper.WriteLineWithColor($"{output.Key}: {lines[0]}", ConsoleColor.White);
                }
                else
                {
                    var paddedLines = lines.Skip(1).Select(line => new string(' ', paramPadding) + line);
                    var paramLines = lines[0] + Environment.NewLine + string.Join(Environment.NewLine, paddedLines);
                    ConsoleHelper.WriteLineWithColor($"{output.Key}: {paramLines}", ConsoleColor.White);
                }
            }
            await Task.CompletedTask;
        }

        public async Task NotifyWorkflowStepStart(string stepName, Dictionary<string, string> inputParameters)
        {
            ConsoleHelper.WriteLineWithColor($"\nWorkflow step '{stepName}' has started.", ConsoleColor.Cyan);
            foreach (var input in inputParameters)
            {
                var lines = input.Value.Split('\n');
                var paramPadding = input.Key.Length + 2;
                
                if (lines.Length == 1)
                {
                    ConsoleHelper.WriteLineWithColor($"{input.Key}: {lines[0]}", ConsoleColor.White);
                }
                else
                {
                    var paddedLines = lines.Skip(1).Select(line => new string(' ', paramPadding) + line);
                    var paramLines = lines[0] + Environment.NewLine + string.Join(Environment.NewLine, paddedLines);
                    ConsoleHelper.WriteLineWithColor($"{input.Key}: {paramLines}", ConsoleColor.White);
                }
            }
            await Task.CompletedTask;
        }
    }
}
