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
                Console.WriteLine("Enter your question or type 'exit':");
                Console.Write("> ");
                var requestText = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(requestText))
                {
                    Console.WriteLine("Please enter a valid question.");
                    continue;
                }

                if (string.Equals(requestText?.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                var executionResult = await appInstance.ProcessRequest(requestText!, cancellationToken);

                ConsoleHelper.WriteLineWithColor("\nResponse for user:", ConsoleColor.Gray);
                ConsoleHelper.WriteLineWithColor(executionResult.Message, ConsoleColor.Cyan);

                ConsoleHelper.WriteLineWithColor($"\n\nConversation status: Count of messages {executionResult.CountOfMessages}. Count of tokens: {executionResult.CountOfTokens}/{conversationSummarizerConfiguration.SummaryTokenThreshold}. Minimum count of messages before summarization: {conversationSummarizerConfiguration.NumMessageToPreseve}\n", ConsoleColor.Gray);
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
            Console.WriteLine("Sandbox Url: " + sesJSSandboxConfiguration.SandboxServiceURL + ", SandboxName: " + sesJSSandboxConfiguration.SandboxName + ", AgentId: " + userConfiguration.AgentId);
            Console.WriteLine("Agent configurations:");
            foreach(var agentConfig in agentsConfigurations)
            {
                ConsoleHelper.PrintAgentConfiguration(agentConfig.AgentUniqueRole, agentConfig.ProviderModelName, Convert.ToDouble(agentConfig.Temperature, CultureInfo.InvariantCulture));
            }
            Console.WriteLine();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Run(stoppingToken);
        }
    }
}
