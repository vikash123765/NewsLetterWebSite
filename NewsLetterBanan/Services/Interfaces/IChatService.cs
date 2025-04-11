namespace NewsLetterBanan.Services.Interfaces
{
    public interface IChatService
    {
        Task<string> GetChatResponseAsync(string userMessage);
        Task<string> ChatResponseConversation(List<(string, string)> messages);


        // ✅ New Admin AI method
        Task<string> AdminChatResponse(List<(string, string)> chatHistory);
    }
}