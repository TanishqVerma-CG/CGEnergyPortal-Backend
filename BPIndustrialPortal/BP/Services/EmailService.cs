using BP.IService;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BP.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("Smtp");

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(smtp["From"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp["Host"], int.Parse(smtp["Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtp["Username"], smtp["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
