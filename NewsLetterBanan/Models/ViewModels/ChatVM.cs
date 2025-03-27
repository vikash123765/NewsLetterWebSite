namespace NewsLetterBanan.Models.ViewModels
{
    public class ChatVM
    {
        public string UserMessage { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public List<(string Role, string Content)> ChatHistory { get; set; } = new();

        public string SerializedHistory { get; set; } = string.Empty;
    }
}
