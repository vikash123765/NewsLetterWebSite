using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;
using MailKit.Net.Smtp;
using MimeKit.Text;
namespace NewsLetterBanan.Models.Helper
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            string response = "";
            var message = new MimeMessage();
            message.Sender = MailboxAddress.Parse(_configuration["SenderEmail"]);
            message.Sender.Name = _configuration["SenderName"];
            message.To.Add(MailboxAddress.Parse(email));
            message.From.Add(message.Sender);
            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };
         
            using (var emailClient = new SmtpClient())
            {
                try
                {
                    // 🔐 Disable SSL certificate validation (DEV ONLY!)
                    emailClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    emailClient.Connect(_configuration["SmtpServer"], Convert.ToInt32(_configuration["SmtpPort"]), true);
                }
                catch (SmtpCommandException ex)
                {
                    response = "Error trying to connect:" + ex.Message + " StatusCode: " + ex.StatusCode;
                    return Task.FromResult(response);
                }
                catch (SmtpProtocolException ex)
                {
                    response = "Protocol error while trying to connect:" + ex.Message;
                    return Task.FromResult(response);
                }

                emailClient.AuthenticationMechanisms.Remove("XOAUTH2");
                emailClient.Authenticate(_configuration["SmtpUsername"], _configuration["SmtpPassword"]);
                try
                {
                    emailClient.Send(message);
                }
                catch (SmtpCommandException ex)
                {
                    response = "Error sending message: " + ex.Message + " StatusCode: " + ex.StatusCode;
                    switch (ex.ErrorCode)
                    {
                        case SmtpErrorCode.RecipientNotAccepted:
                            response += " Recipient not accepted: " + ex.Mailbox;
                            break;
                        case SmtpErrorCode.SenderNotAccepted:
                            response += " Sender not accepted: " + ex.Mailbox;
                            Console.WriteLine("\tSender not accepted: {0}", ex.Mailbox);
                            break;
                        case SmtpErrorCode.MessageNotAccepted:
                            response += " Message not accepted.";
                            break;
                    }

                }
                emailClient.Disconnect(true);
            }
            return Task.CompletedTask;
        }
    }
}