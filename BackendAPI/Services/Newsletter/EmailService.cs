using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BackendAPI.Services.Newsletter
{
    public interface IEmailService
    {
        Task SendAsync(IEnumerable<string> recipients, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(IEnumerable<string> recipients, string subject, string htmlBody)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"] ?? throw new InvalidOperationException("Smtp:Host is niet geconfigureerd.");
            var port = int.Parse(smtpSection["Port"] ?? "587");
            var user = smtpSection["Username"] ?? throw new InvalidOperationException("Smtp:Username is niet geconfigureerd.");
            var password = smtpSection["Password"] ?? throw new InvalidOperationException("Smtp:Password is niet geconfigureerd.");
            var fromName = smtpSection["FromName"] ?? "Pathe Tilburg";
            var fromEmail = smtpSection["FromEmail"] ?? user;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));

            foreach (var email in recipients)
                message.Bcc.Add(MailboxAddress.Parse(email));

            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Nieuwsbrief verstuurd naar {Count} ontvanger(s).", recipients.Count());
        }
    }
}
