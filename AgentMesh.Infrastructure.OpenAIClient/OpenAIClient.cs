using AgentMesh.Application.Models;
using AgentMesh.Application.Services;
using AgentMesh.Models;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Responses;
using Polly;
using System.ClientModel;
using System.Globalization;
using System.Linq;

namespace AgentMesh.Infrastructure.OpenAIClient
{
    public class OpenAIClient : IOpenAIClient
    {
        private readonly string _systemPrompt;
        private readonly float _temperature;
        private readonly ChatClient _client;


        public OpenAIClient(string model, string apikey, string endpoint, string temperature, string systemPrompt)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (apikey == null) throw new ArgumentNullException(nameof(apikey));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (systemPrompt == null) throw new ArgumentNullException(nameof(systemPrompt));

            _client = new ChatClient(
              model: model,
              credential: new ApiKeyCredential(apikey),
              options: new OpenAIClientOptions()
              {
                  Endpoint = new Uri(endpoint)
              }
            );

            _systemPrompt = systemPrompt;
            _temperature = float.Parse(temperature, CultureInfo.InvariantCulture);
        }


        public async Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<string> userInput)
        {
            var messages = userInput.Select(input => new AgentMessage
            {
                Role = AgentMessageRole.User,
                Content = input
            });

            return await GenerateResponseAsync(messages);
        }


        public async Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages)
        {
            var chatMessages = new List<ChatMessage>();

            var mergedPrompt = CreateMergedSystemPrompt(new List<AgentMessage>(messages).Prepend(new AgentMessage
            {
                Role = AgentMessageRole.System,
                Content = _systemPrompt
            }));
            chatMessages.Add(mergedPrompt);

            foreach (var message in messages)
            {
                ChatMessage chatMessage;

                switch (message.Role)
                {
                    case AgentMessageRole.System:
                        continue;   // skip
                    case AgentMessageRole.Assistant:
                        chatMessage = ChatMessage.CreateAssistantMessage(message.Content);
                        break;
                    default:
                        chatMessage = ChatMessage.CreateUserMessage(message.Content);
                        break;
                }

                chatMessages.Add(chatMessage);
            }

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = _temperature,
               // ToolChoice = ChatToolChoice.CreateNoneChoice(),
                ResponseFormat = ChatResponseFormat.CreateTextFormat(),
            };

           // chatCompletionOptions.Tools.Clear();

            var retryPolicy = Policy
                .HandleResult<ClientResult<ChatCompletion>>(result => string.IsNullOrWhiteSpace(GetResponseText(result)))
                .WaitAndRetryAsync(1, _ => TimeSpan.FromSeconds(5));

            var chatCompletionResult = await retryPolicy.ExecuteAsync(async () =>
                await _client.CompleteChatAsync(chatMessages, chatCompletionOptions));

            var responseText = GetResponseText(chatCompletionResult) ?? string.Empty;

            return new OpenAIClientResponse
            {
                Text = responseText,
                TotalTokenCount = chatCompletionResult.Value.Usage.TotalTokenCount,
                InputTokenCount = chatCompletionResult.Value.Usage.InputTokenCount,
                OutputTokenCount = chatCompletionResult.Value.Usage.OutputTokenCount
            };
        }

        private static string? GetResponseText(ClientResult<ChatCompletion> result)
        {
            return result?.Value?.Content?.FirstOrDefault()?.Text;
        }


        private static ChatMessage CreateMergedSystemPrompt(IEnumerable<AgentMessage> messages)
        {
            var systemMessages = messages
                .Where(m => m.Role == AgentMessageRole.System)
                .Select(m => m.Content);
            var mergedPrompt = string.Join("\n", systemMessages);
            return ChatMessage.CreateSystemMessage(mergedPrompt);
        }
    }
}