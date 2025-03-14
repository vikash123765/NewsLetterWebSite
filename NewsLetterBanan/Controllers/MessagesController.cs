using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NewsLetterBanan.Data;
using NewsLetterBanan.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace NewsLetterBanan.Controllers
{
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MessagesController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }



        [HttpGet("/Message/Send")]
        public async Task<IActionResult> SendAsync()
        {
            Console.WriteLine("📩 Send GET action called!");

            try
            {
                var usersWithRoles = new List<SelectListItem>();

                // ✅ Fetch all users first (prevent multiple DbContext operations at once)
                var users = await _context.Users.ToListAsync();

                // ✅ Fetch roles one by one (avoids concurrent DbContext usage)
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Count > 1) // User must have more than one role
                    {
                        string displayText = $"{user.UserName} ({string.Join(", ", roles)})";
                        usersWithRoles.Add(new SelectListItem
                        {
                            Value = user.Id,
                            Text = displayText
                        });
                    }
                }

                Console.WriteLine($"👥 Found {usersWithRoles.Count} eligible users with multiple roles.");

                // ✅ Populate ViewBag
                ViewBag.Users = usersWithRoles;

                if (usersWithRoles.Count == 0)
                {
                    Console.WriteLine("⚠️ No users found with multiple roles.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching users: {ex.Message}");
            }

            return View(new SendMessageViewModel());
        }




        [HttpPost("/Message/Send")]

        public async Task<IActionResult> Send(SendMessageViewModel model)
        {
            Console.WriteLine("📩 Send action called!");

            // ✅ Debug ModelState
            if (!ModelState.IsValid)
            {
                Console.WriteLine("⚠️ ModelState is invalid!");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"🚨 Model Error: {error.ErrorMessage}");
                }
                return View(model);
            }

            // ✅ Ensure the current user is logged in
            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
            {
                Console.WriteLine("❌ Sender is null. User must be logged in.");
                ModelState.AddModelError("", "You must be logged in to send a message.");
                return View(model);
            }

            Console.WriteLine($"✅ Sender found: {sender.Id}");

            // ✅ Ensure ReceiverId is valid
            if (string.IsNullOrEmpty(model.ReceiverId))
            {
                Console.WriteLine("❌ ReceiverId is null or empty.");
                ModelState.AddModelError("ReceiverId", "Receiver is required.");
                return View(model);
            }

            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.ReceiverId);
            if (receiver == null)
            {
                Console.WriteLine($"❌ Receiver with ID {model.ReceiverId} not found.");
                ModelState.AddModelError("ReceiverId", "Selected user does not exist.");
                return View(model);
            }

            Console.WriteLine($"✅ Receiver found: {receiver.Id}");

            // ✅ Ensure Content is not empty
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                Console.WriteLine("❌ Message content is empty.");
                ModelState.AddModelError("Content", "Message content cannot be empty.");
                return View(model);
            }

            // ✅ Create the message
            var message = new Message
            {
                Content = model.Content,
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            Console.WriteLine("📌 Creating message...");
            Console.WriteLine($"📝 Content: {message.Content}");
            Console.WriteLine($"🆔 Sender: {message.SenderId}, Receiver: {message.ReceiverId}");
            Console.WriteLine($"⏰ Timestamp: {message.Timestamp}");

            try
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Message saved to database");

                // ✅ Add to Inbox table
                var inbox = new Inbox
                {
                    UserId = receiver.Id,
                    MessageId = message.Id
                };
                _context.Inboxes.Add(inbox);
                Console.WriteLine("📥 Message added to Inbox");

                // ✅ Add to Sent table
                var sent = new Sent
                {
                    SenderId = sender.Id,
                    MessageId = message.Id
                };
                _context.SentMessages.Add(sent);
                Console.WriteLine("📤 Message added to Sent");

                await _context.SaveChangesAsync();
                Console.WriteLine("✅ Message successfully sent!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving message: {ex.Message}");
                return StatusCode(500, "Error saving message.");
            }

            return RedirectToAction("Sent");
        }

        [HttpGet("/Message/Inbox")]
        // GET: View User's Inbox
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var inboxMessages = await _context.Inboxes
                .Where(i => i.UserId == user.Id)
                .Include(i => i.Message)
                .ThenInclude(m => m.Sender)
                .OrderByDescending(i => i.Message.Timestamp)
                .ToListAsync();

            return View(inboxMessages);
        }


        [HttpGet("/Message/Sent")]
        // GET: View User's Sent Messages
        public async Task<IActionResult> Sent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Retrieve each sent message separately, including Message and its Receiver
            var sentMessages = await _context.SentMessages
                .Where(s => s.SenderId == user.Id)
                .Include(s => s.Message)
                    .ThenInclude(m => m.Receiver)
                .OrderByDescending(s => s.Message.Timestamp)
                .ToListAsync();
            return View(sentMessages);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int inboxId)
        {
            var inboxItem = await _context.Inboxes.FindAsync(inboxId);
            if (inboxItem == null) return NotFound();

            inboxItem.IsRead = true;

            var sentMessage = await _context.SentMessages
                .FirstOrDefaultAsync(m => m.MessageId == inboxItem.MessageId);

            if (sentMessage != null)
            {
                sentMessage.IsRead = true;
            }

            await _context.SaveChangesAsync();

            // ✅ Redirect to Inbox instead of returning JSON
            return RedirectToAction("Inbox");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteInboxMessage(int inboxId)
        {
            var inboxItem = await _context.Inboxes.FindAsync(inboxId);
            if (inboxItem == null) return NotFound();

            _context.Inboxes.Remove(inboxItem);
            await _context.SaveChangesAsync();

            Console.WriteLine($"🗑️ Deleted inbox message with ID: {inboxId}");

            return RedirectToAction("Inbox"); // Redirect back to inbox
        }
        [HttpPost]
        public async Task<IActionResult> DeleteSentMessage(int sentId)
        {
            var sentItem = await _context.SentMessages.FindAsync(sentId);
            if (sentItem == null) return NotFound();

            _context.SentMessages.Remove(sentItem);
            await _context.SaveChangesAsync();

            Console.WriteLine($"🗑️ Deleted sent message with ID: {sentId}");

            return RedirectToAction("Sent"); // Redirect back to sent messages
        }

        [HttpGet]
        public async Task<IActionResult> EditMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null) return NotFound();

            return View(message);
        }

        [HttpPost]
        public async Task<IActionResult> EditMessage(Message updatedMessage)
        {
            var message = await _context.Messages.FindAsync(updatedMessage.Id);
            if (message == null) return NotFound();

            message.Content = updatedMessage.Content; // Update message content
            await _context.SaveChangesAsync();

            Console.WriteLine($"✏️ Updated message with ID: {message.Id}");

            return RedirectToAction("Sent"); // Redirect back to Sent messages
        }
        [HttpGet]
        public async Task<IActionResult> UnreadMessagesCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(0);

            var unreadCount = await _context.Inboxes
                .Where(i => i.UserId == user.Id && !i.IsRead)
                .CountAsync();

            return Json(unreadCount);
        }




    }
}
