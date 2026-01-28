using AgentMesh.Application.Models;
using AgentMesh.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace AgentMesh.Application.Services
{
    public abstract class ChatManagerAgentBaseClass
    {
        private const string MESSAGES_SEPARATOR = "════════";

        private readonly List<AgentMessage> _context;
        private IOpenAIClient _openAIClient;
        private IOpenAIClient _summaryOpenAIClient;
        private readonly ILogger _logger;

        private string _summaryPrompt;
        private int _numMessageToPreseve;
        private int _summaryTokenThreshold;

        protected ChatManagerAgentBaseClass(IOpenAIClient openAIClient, IOpenAIClient summaryOpenAIClient, ChatManagerAgentConfiguration chatManagerConfiguration, ILogger logger)
        {
            _context = new List<AgentMessage>();
            _openAIClient = openAIClient;
            _summaryOpenAIClient = summaryOpenAIClient;
            _summaryPrompt = chatManagerConfiguration.SummaryPrompt;
            _summaryTokenThreshold = chatManagerConfiguration.SummaryTokenThreshold;
            _numMessageToPreseve = chatManagerConfiguration.NumMessageToPreseve;
            _logger = logger;
        }

        protected List<AgentMessage> Context => _context;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected async Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogDebug("Generating response with {MessageCount} messages", messages.Count());

            // create a single message with transcript of all messages
            var transcription = new List<string>();
            foreach (var message in messages)
            {
                if (message.Role == AgentMessageRole.System)
                    continue;

                var role = "User";
                if (message.Role == AgentMessageRole.Assistant)
                {
                    role = "Expert";
                }

                var messageTranscript = $"Role: {role}\nContent: {message.Content}";
                transcription.Add(messageTranscript);
            }

            var messagesSent = new List<AgentMessage>();
            messagesSent.AddRange(messages.Where(m => m.Role == AgentMessageRole.System).ToList());
            var transcriptListString = string.Join($"\n{MESSAGES_SEPARATOR}\n", transcription);
            messagesSent.Add(new AgentMessage
            {
                Role = AgentMessageRole.User,
                Content = transcriptListString
            }); 

            var response = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                return await _openAIClient.GenerateResponseAsync(messagesSent);
            }, "ChatManagerAgent", _logger);
            
            stopwatch.Stop();
            _logger.LogDebug("Response generated in {ElapsedMs}ms. Input tokens: {InputTokens}, Output tokens: {OutputTokens}, Total tokens: {TotalTokens}",
                stopwatch.ElapsedMilliseconds, response.InputTokenCount, response.OutputTokenCount, response.TotalTokenCount);
            
            if (response.TotalTokenCount >= _summaryTokenThreshold)
            {
                _logger.LogDebug("Token threshold reached ({TotalTokens} >= {Threshold}), triggering context summary",
                    response.TotalTokenCount, _summaryTokenThreshold);
                await SummaryContextAsync();
            }
            return response;
        }

        /// <summary>
        /// Adds a user message to the chat context.
        /// </summary>
        /// <param name="userText">The text content of the user's message.</param>
        protected void AddUserMessage(string userText)
        {
            _context.Add(new AgentMessage { Role = AgentMessageRole.User, Content = userText });
            _logger.LogDebug("Added user message to context. Total messages: {MessageCount}", _context.Count);
        }

        private async Task SummaryContextAsync()
        {
            if (_context.Count < _numMessageToPreseve)
            {
                _logger.LogDebug("Skipping context summary. Message count ({MessageCount}) below preservation threshold ({Threshold})",
                    _context.Count, _numMessageToPreseve);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var originalMessageCount = _context.Count;
            _logger.LogDebug("Starting context summarization. Current messages: {MessageCount}, Messages to preserve: {PreserveCount}",
                originalMessageCount, _numMessageToPreseve);

            var summaryInputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.User, Content = _summaryPrompt + ":\n\n<chathistory>" + string.Join("\n\n", _context.Select(x=> $"Role:{x.Role}\nContent:{x.Content}")) + "</chathistory>" }
            };
            
            var summaryResponse = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                return await _summaryOpenAIClient.GenerateResponseAsync(summaryInputMessages);
            }, "ChatManagerAgent_Summary", _logger);

            _logger.LogDebug("Summary generated. Input tokens: {InputTokens}, Output tokens: {OutputTokens}, Total tokens: {TotalTokens}",
                summaryResponse.InputTokenCount, summaryResponse.OutputTokenCount, summaryResponse.TotalTokenCount);

            _context.RemoveRange(0, _context.Count - _numMessageToPreseve);
            _context.Insert(0, new AgentMessage { Role = AgentMessageRole.Assistant, Content = "This is a summary of the previous messages:\n\n" + summaryResponse.Text ?? string.Empty });

            _logger.LogInformation($"Context summarized. Summary: {summaryResponse.Text}");
            
            stopwatch.Stop();
            _logger.LogDebug("Context summarization completed in {ElapsedMs}ms. Messages reduced from {OriginalCount} to {NewCount}",
                stopwatch.ElapsedMilliseconds, originalMessageCount, _context.Count);
        }
    }
}
