using AgentMesh.Application.Services;
using AgentMesh.Application.Configuration;
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
            Console.WriteLine("Welcome to AgentMesh! This is a console application that allows you to interact with the AgentMesh system.\n");

            PrintConfigurations();

            while (true)
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

                var executionResult = await appInstance.ProcessRequest(requestText!, cancellationToken);

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
        }
     

        private void PrintConfigurations()
        {
            Console.WriteLine($"Sandbox:\n\tUrl: {sesJSSandboxConfiguration.SandboxServiceURL}\n\tName: {sesJSSandboxConfiguration.SandboxName}\n\tAgentId: {userConfiguration.AgentId}\n");
            Console.WriteLine($"Conversation summarization configuration:\n\tSummaryTokenThreshold: {conversationSummarizerConfiguration.SummaryTokenThreshold}\n\tNumMessageToPreseve: {conversationSummarizerConfiguration.NumMessageToPreseve}\n");
            Console.WriteLine("Agent configurations:");
            foreach(var agentConfig in agentsConfigurations)
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
            Console.WriteLine("Any other text will be treated as a question to the AgentMesh system.\n");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Run(stoppingToken);
        }
    }
}
