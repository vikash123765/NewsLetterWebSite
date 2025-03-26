using Azure.AI.OpenAI;
using NewsLetterBanan.Services.Interfaces;
using OpenAI.Chat;

namespace NewsLetterBanan.Services
{
    public class ChatService : IChatService
    {
        private readonly AzureOpenAIClient _aiClient;
        private readonly string _deploymentName;
        public ChatService(AzureOpenAIClient aiClient, IConfiguration configuration)
        {
            _aiClient = aiClient;
            _deploymentName = configuration["AzureOpenAI:DeploymentName"]!;
        }
        public async Task<string> GetChatResponseAsync(string userMessage)
        {
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an AI assistant that helps people find information."),
                new UserChatMessage(userMessage)
            };
            // Create chat completion options

            var options = new ChatCompletionOptions
            {

                Temperature = (float)0.7,
                MaxOutputTokenCount = 800,
                TopP = (float)0.95,
                FrequencyPenalty = (float)0.8,
                PresencePenalty = (float)0
            };
            try
            {
                // Initialize the ChatClient with the specified deployment name
                ChatClient chatClient = _aiClient.GetChatClient("gpt-4o");
                // Create the chat completion request
                ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages, options);

                // Print the response
                if (completion != null)
                {
                    return completion.Content[0].Text;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<string> ChatResponseConversation(List<(string, string)> messages)
        {
            var conversation = "";
            foreach (var message in messages)
            {
                conversation += message.Item1;
                conversation += message.Item2;
            }
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage("You are an AI assistant that helps people find information."),
                new UserChatMessage(conversation)
            };
            // Create chat completion options

            var options = new ChatCompletionOptions
            {

                Temperature = (float)0.7,
                MaxOutputTokenCount = 800,
                TopP = (float)0.95,
                FrequencyPenalty = (float)0.8,
                PresencePenalty = (float)0
            };
            try
            {
                // Initialize the ChatClient with the specified deployment name
                ChatClient chatClient = _aiClient.GetChatClient("gpt-4o");
                // Create the chat completion request
                ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages, options);

                // Print the response
                if (completion != null)
                {
                    return completion.Content[0].Text;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}