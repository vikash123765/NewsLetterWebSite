using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EmailSenderSubscriptionExpiryQueueListener.Models;
using Microsoft.Extensions.Logging;

namespace EmailSenderSubscriptionExpiryQueueListener.Services
{
    public class EmailSender
    {

        private readonly ILogger _logger;

        public EmailSender(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EmailSender>();
        }

        public async Task SendEmailAsync(UserSubscription user)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("vikash.kosaraj1234@gmail.com", "tfoz ibjs tcrb gjub"),
                    EnableSsl = true
                };

                string subject = "Subscription Expiry Notification";
                string body = $"Hello {user.UserName},\n\nYour subscriptions are expiring soon:\n";

                foreach (var sub in user.ExpiringSubscriptions)
                {
                    body += $"- {sub.SubscriptionType}: {sub.ExpiryDate}\n";
                }

                body += "\nRegards,\nYour Service Team";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("vikash.kosaraj1234@gmail.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(user.Email);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"Email sent to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {user.Email}: {ex.Message}");
            }
        }
    }
}