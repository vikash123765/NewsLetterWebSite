using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models.ViewModels;
using NewsLetterBanan.Services.Interfaces;

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
