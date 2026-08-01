using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace AirportApp.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "Avenzia Airways";
                var password = _configuration["EmailSettings:Password"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"] ?? "587";
                
                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("Advertencia: Configuración de correo incompleta. No se enviará el correo.");
                    return;
                }

                int.TryParse(smtpPortStr, out int smtpPort);

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, password),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"Correo enviado exitosamente a {email} con asunto: {subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo a {email}: {ex.Message}");
                // No propagamos la excepción para evitar que el flujo de Identity falle,
                // pero lo registramos en consola.
            }
        }
    }
}
