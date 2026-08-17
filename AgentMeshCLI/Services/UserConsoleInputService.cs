using AgentMesh.Application.Configuration;
using AgentMesh.Application.Services;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.JSSandbox;
using Microsoft.Extensions.Hosting;
using System.Globalization;

namespace AgentMesh.Services
{
    internal class UserConsoleInputService(
        UserConfiguration userConfiguration,
        SESJSSandboxConfiguration sesJSSandboxConfiguration,
        IEnumerable<AgentFlatConfigurationRecord> agentsConfigurations,
        ConversationSummarizationConfiguration conversationSummarizerConfiguration,
        AppInstance appInstance) : BackgroundService
    {
        public async Task Run(CancellationToken cancellationToken)
        {
            bool isFirstRun = true;

            Console.WriteLine("Welcome to AgentMesh! This is a console application that allows you to interact with the AgentMesh system.\n");

            PrintConfigurations();

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Enter your question or type /help:");
                Console.Write("> ");
                var requestText = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(requestText))
                {
                    Console.WriteLine("Please enter a valid question.");
                    continue;
                }

                if (string.Equals(requestText?.Trim(), "/exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (string.Equals(requestText?.Trim(), "/help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    continue;
                }

                if (string.Equals(requestText?.Trim(), "/new", StringComparison.OrdinalIgnoreCase))
                {
                    await appInstance.InitConversation();
                    ConsoleHelper.WriteLineWithColor("New conversation initialized.", ConsoleColor.Green);
                    continue;
                }

                if (isFirstRun)
                {
                    ConsoleHelper.WriteLineWithColor("You can cancel the current request by pressing Ctrl+C.\n", ConsoleColor.Yellow);
                    isFirstRun = false;
                }

                var previousTreatControlCAsInput = Console.TreatControlCAsInput;
                Console.TreatControlCAsInput = true;

                using var requestCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var cancelMonitorTask = MonitorRequestCancellationByKeyboardAsync(requestCancellationTokenSource, cancellationToken);

                try
                {
                    var executionResult = await appInstance.ProcessRequest(requestText!, requestCancellationTokenSource.Token);

                    ConsoleHelper.WriteLineWithColor("\nResponse for user:", ConsoleColor.Gray);
                    ConsoleHelper.WriteLineWithColor(executionResult.Message, ConsoleColor.Cyan);

                    ConsoleHelper.WriteLineWithColor($"\n\nConversation status:\nCount of messages {executionResult.CountOfMessages}\nCount of tokens: {executionResult.CountOfTokens}/{conversationSummarizerConfiguration.SummaryTokenThreshold}\nCumulated cost: {Math.Round(executionResult.CumulatedCost, 2)} $", ConsoleColor.Gray);
                    if (executionResult.ContextSummarizerHasRun)
                    {
                        ConsoleHelper.WriteLineWithColor($"Chat conversation has been summarized. Count of messages before: {executionResult.CountOfMessagesBeforeSummarization}", ConsoleColor.White);

                    }

                    ConsoleHelper.PrintTokenUsageSummary(executionResult.MainPipelineStepsData,
                        executionResult.AgentsCostData);
                }
                catch (OperationCanceledException) when (requestCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    ConsoleHelper.WriteLineWithColor("Request canceled.", ConsoleColor.Yellow);
                }
                finally
                {
                    requestCancellationTokenSource.Cancel();
                    await cancelMonitorTask;
                    Console.TreatControlCAsInput = previousTreatControlCAsInput;
                }
            }
        }

        private static async Task MonitorRequestCancellationByKeyboardAsync(CancellationTokenSource requestCancellationTokenSource, CancellationToken appCancellationToken)
        {
            while (!requestCancellationTokenSource.IsCancellationRequested && !appCancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(intercept: true);
                    if (keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                    {
                        requestCancellationTokenSource.Cancel();
                        ConsoleHelper.WriteLineWithColor("\nCurrent request cancellation requested...", ConsoleColor.Yellow);
                        return;
                    }
                }

                try
                {
                    await Task.Delay(50, appCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }


        private void PrintConfigurations()
        {
            Console.WriteLine($"Sandbox:\n\tUrl: {sesJSSandboxConfiguration.SandboxServiceURL}\n\tName: {sesJSSandboxConfiguration.SandboxName}\n\tAgentId: {userConfiguration.AgentId}\n");
            Console.WriteLine($"Conversation summarization configuration:\n\tSummaryTokenThreshold: {conversationSummarizerConfiguration.SummaryTokenThreshold}\n\tNumMessageToPreseve: {conversationSummarizerConfiguration.NumMessageToPreseve}\n");
            Console.WriteLine("Agent configurations:");
            foreach (var agentConfig in agentsConfigurations)
            {
                ConsoleHelper.PrintAgentConfiguration(agentConfig.AgentUniqueRole, agentConfig.ProviderModelName, Convert.ToDouble(agentConfig.Temperature, CultureInfo.InvariantCulture));
            }
            Console.WriteLine();
        }

        private void PrintHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("/help - Show this help message");
            Console.WriteLine("/exit - Exit the application");
            Console.WriteLine("/new - Initializes a new conversation");
            Console.WriteLine("Ctrl+C - Cancel the current request");
            Console.WriteLine("Any other text will be treated as a question to the AgentMesh system.\n");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Run(stoppingToken);
        }
    }
}
