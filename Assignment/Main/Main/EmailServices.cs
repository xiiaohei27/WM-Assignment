using System.Net;
using System.Net.Mail;

namespace Main
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendVerificationEmailAsync(string toEmail, string verificationLink);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"];
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail ?? "noreply@moviecenter.com", fromName ?? "Movie Center");
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var smtpClient = new SmtpClient(smtpHost);
            smtpClient.Port = smtpPort;
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential(username, password);

            try
            {
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                // Log the error (in production, use proper logging)
                Console.WriteLine($"Email sending failed: {ex.Message}");
                // For development, you might want to skip actual sending
                // throw; // Uncomment in production
            }
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
        {
            var subject = "Verify Your Email - Movie Center";
            var body = $@"
                <html>
                <body>
                    <h2>Welcome to Movie Center!</h2>
                    <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verify Email</a></p>
                    <p>Or copy and paste this link into your browser:</p>
                    <p>{verificationLink}</p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you didn't create this account, please ignore this email.</p>
                    <br>
                    <p>Best regards,<br>Movie Center Team</p>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Reset Your Password - Movie Center";
            var body = $@"
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>We received a request to reset your password. Click the link below to reset it:</p>
                    <p><a href='{resetLink}' style='background-color: #2196F3; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                    <p>Or copy and paste this link into your browser:</p>
                    <p>{resetLink}</p>
                    <p>This link will expire in 1 hour.</p>
                    <p>If you didn't request a password reset, please ignore this email.</p>
                    <br>
                    <p>Best regards,<br>Movie Center Team</p>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}