using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NewsLetterBanan.Data;
using NewsLetterBanan.Helpers;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services.Interfaces;
using Newtonsoft.Json;

namespace NewsLetterBanan.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly IChatService _chatService;

        public ChatController(ILogger<ChatController> logger, IChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            return View(new ChatVM());
        }

        [HttpPost]
        public IActionResult Index(ChatVM chatVM)
        {
            var response = _chatService.GetChatResponseAsync(chatVM.UserMessage).Result;
            chatVM.Response = response;
            return View(chatVM);
        }
        public IActionResult ChatWithHistory()
        {
            ChatVM chat = new ChatVM();
            chat.ChatHistory = HttpContext.Session.Get<List<(string, string)>>("Chat");
            if (chat.ChatHistory is null)
            {
                chat.ChatHistory = new List<(string, string)>();
                HttpContext.Session.Set("Chat", chat.ChatHistory);
            }
            chat.UserMessage = string.Empty;
            return View(chat);
        }

        [HttpPost]
        public IActionResult ChatWithHistory(ChatVM chat)
        {
            chat.ChatHistory = HttpContext.Session.Get<List<(string, string)>>("Chat");
            if (chat.ChatHistory is null)
            {
                chat.ChatHistory = new List<(string, string)>();
            }
            chat.ChatHistory.Add(("User", chat.UserMessage));
            var response = _chatService.ChatResponseConversation(chat.ChatHistory);
            chat.ChatHistory.Add(("AI assistant", response.Result));
            HttpContext.Session.Set("Chat", chat.ChatHistory);
            chat.UserMessage = "";
            return View(chat);
        }

        public IActionResult ChatWithHistoryVM()
        {
            ChatVM chat = new ChatVM();
           
            return View(chat);
        }

        [HttpPost]
        public IActionResult ChatWithHistoryVM(ChatVM chat)
        {
            var temp= chat.SerializedHistory ?? string.Empty;
            chat.ChatHistory = JsonConvert.DeserializeObject<List<(string, string)>>(temp);
            if (chat.ChatHistory is null)
            {
                chat.ChatHistory = new List<(string, string)>();
            }
            chat.ChatHistory.Add(("User", chat.UserMessage));
            var response = _chatService.ChatResponseConversation(chat.ChatHistory);
            chat.ChatHistory.Add(("AI assistant", response.Result));
            chat.SerializedHistory = JsonConvert.SerializeObject(chat.ChatHistory);
            chat.UserMessage = "";
            return View(chat);
        }

        public IActionResult ChatWithHistoryTempData()
        {
            ChatVM chat = new ChatVM();

            return View(chat);
        }

        [HttpPost]
        public IActionResult ChatWithHistoryTempData(ChatVM chat)
        {
            var temp = (string)TempData["history"] ?? string.Empty;
            chat.ChatHistory = JsonConvert.DeserializeObject<List<(string, string)>>(temp);
            if (chat.ChatHistory is null)
            {
                chat.ChatHistory = new List<(string, string)>();
            }
            chat.ChatHistory.Add(("User", chat.UserMessage));
            var response = _chatService.ChatResponseConversation(chat.ChatHistory);
            chat.ChatHistory.Add(("AI assistant", response.Result));
            TempData["history"] = JsonConvert.SerializeObject(chat.ChatHistory);
            chat.UserMessage = "";
            return View(chat);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
