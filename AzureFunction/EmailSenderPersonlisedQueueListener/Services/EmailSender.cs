using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EmailSenderPersonlisedQueueListener.Models;

namespace EmailSenderPersonlisedQueueListener.Services
{
    public class EmailSender
    {
        private readonly ILogger _logger;

        public EmailSender(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EmailSender>();
        }

        public async Task SendEmailAsync(User user)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("vikash.kosaraj1234@gmail.com", "tfoz ibjs tcrb gjub"),
                    EnableSsl = true
                };

                string subject = user.HasActiveSubscription
                    ? "✨ Exclusive & Latest News Just for You!"
                    : "📰 Your Daily News Update";

                string body = GenerateEmailBody(user);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("vikash.kosaraj1234@gmail.com"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true, // Enable HTML formatting
                };
                mailMessage.To.Add(user.Email);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation($"📧 Email sent to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Failed to send email to {user.Email}: {ex.Message}");
            }
        }

        private string GenerateEmailBody(User user)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<h2>Hello {user.UserName},</h2>");

            if (user.HasActiveSubscription)
            {
                sb.AppendLine("<p>As a valued subscriber, you have access to exclusive articles! Plus, check out the latest and trending news.</p>");
                AppendArticlesSection(sb, "🔒 Exclusive Articles (For Subscribers Only)", user.ExclusiveArticles);
            }
            else
            {
                sb.AppendLine("<p>Here are your latest news updates.</p>");
            }

            AppendArticlesSection(sb, "📰 Latest Articles", user.LatestArticles);
            AppendArticlesSection(sb, "⭐ Editor’s Choice Articles", user.EditorsChoiceArticles);
            AppendArticlesSection(sb, "🔥 Most Popular Articles", user.MostPopularArticles);

            sb.AppendLine("<hr>");

            if (user.HasActiveSubscription)
            {
                sb.AppendLine("<p><b>📩 Enjoy your exclusive access!</b></p>");
            }
            else
            {
                sb.AppendLine("<p><b>🔔 Upgrade to a subscription for exclusive content!</b></p>");
                sb.AppendLine("<p>📍 <a href='https://news.example.com/subscribe'>Subscribe Now</a></p>");
            }

            sb.AppendLine("<p>📍 <a href='https://news.example.com'>Visit Our Website</a></p>");

            return sb.ToString();
        }

        private void AppendArticlesSection(StringBuilder sb, string title, List<Article> articles)
        {
            if (articles == null || articles.Count == 0) return;

            sb.AppendLine($"<h3>{title}</h3>");
            foreach (var article in articles)
            {
                sb.AppendLine("<hr>");
                sb.AppendLine($"<h4>📌 {article.Headline}</h4>");
                sb.AppendLine($"<p><b>{article.ContentSummary}</b></p>");
                sb.AppendLine($"<p>📅 <b>Date:</b> {article.DateStamp.ToShortDateString()}</p>");
                sb.AppendLine($"<p>📁 <b>Category:</b> {string.Join(", ", article.Categories)}</p>");
                sb.AppendLine($"<p>🏷️ <b>Tags:</b> {string.Join(", ", article.Tags)}</p>");
                sb.AppendLine($"<p>🔗 <a href='{article.SourceURL}'>Read More</a></p>");
                sb.AppendLine($"<p>👤 <b>Author:</b> {article.Author}</p>");
            }
        }
    }
}
