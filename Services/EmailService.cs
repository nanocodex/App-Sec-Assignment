using System.Net;
using System.Net.Mail;

namespace WebApplication1.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                var subject = "Password Reset Request";
                var body = $@"
                    <html>
                    <body>
                        <h2>Password Reset Request</h2>
                        <p>You have requested to reset your password. Click the link below to reset your password:</p>
                        <p><a href='{resetLink}'>Reset Password</a></p>
                        <p>This link will expire in 1 hour.</p>
                        <p>If you did not request this password reset, please ignore this email.</p>
                        <br/>
                        <p>Thank you,<br/>IT2163 Application Security Team</p>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendPasswordChangedNotificationAsync(string email, string userName)
        {
            try
            {
                var subject = "Password Changed Successfully";
                var body = $@"
                    <html>
                    <body>
                        <h2>Password Changed</h2>
                        <p>Hello {userName},</p>
                        <p>Your password has been changed successfully.</p>
                        <p>If you did not make this change, please contact support immediately.</p>
                        <br/>
                        <p>Thank you,<br/>IT2163 Application Security Team</p>
                    </body>
                    </html>
                ";

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password changed notification to {Email}", email);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // For demonstration purposes, we'll log the email instead of actually sending it
                // In production, you would configure SMTP settings
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort");
                var smtpUsername = _configuration["Email:SmtpUsername"];
                var smtpPassword = _configuration["Email:SmtpPassword"];
                var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@appsec.com";

                // Check if SMTP is configured
                if (string.IsNullOrEmpty(smtpHost))
                {
                    // Log the email for testing purposes
                    _logger.LogInformation(
                        "EMAIL (SMTP not configured):\nTo: {ToEmail}\nSubject: {Subject}\nBody:\n{Body}",
                        toEmail, subject, body);
                    
                    // Return true to simulate successful email sending
                    return true;
                }

                // Actual SMTP email sending (when configured)
                using var smtpClient = new SmtpClient(smtpHost, smtpPort);
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }
    }
}
