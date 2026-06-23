using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Globalization;

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


        public async Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<string> userInput, CancellationToken cancellationToken = default)
        {
            var messages = userInput.Select(input => new AgentMessage
            {
                Role = AgentMessageRole.User,
                Content = input
            });

            return await GenerateResponseAsync(messages, cancellationToken);
        }


        public async Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default)
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

            // use ExecuteRetryAsync to handle transient errors

            var responseTextBuilder = new System.Text.StringBuilder();
            int totalTokenCount = 0, inputTokenCount = 0, outputTokenCount = 0;

            try
            {
                await foreach (var update in _client.CompleteChatStreamingAsync(chatMessages, chatCompletionOptions, cancellationToken))
                {
                    foreach (var part in update.ContentUpdate)
                    {
                        responseTextBuilder.Append(part.Text);
                    }
                    if (update.Usage != null)
                    {
                        totalTokenCount = update.Usage.TotalTokenCount;
                        inputTokenCount = update.Usage.InputTokenCount;
                        outputTokenCount = update.Usage.OutputTokenCount;
                    }
                }
            }
            catch (ClientResultException ex) when (ex.Message.Contains("Tool choice is none, but model called a tool"))
            {
                throw new BadStructuredResponseException("", ex.Message, ex);
            }

            var responseText = responseTextBuilder.ToString();
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new BadStructuredResponseException("", "The response text is empty.");
            }

            return new OpenAIClientResponse
            {
                Text = responseText,
                TotalTokenCount = totalTokenCount,
                InputTokenCount = inputTokenCount,
                OutputTokenCount = outputTokenCount
            };
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