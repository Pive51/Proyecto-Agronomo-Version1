using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace AgroSystemServer.Services
{
    public interface IEmailService
    {
        Task EnviarCorreoRecuperacionAsync(string destinatario, string token);
        Task EnviarComprobanteVentaAsync(string destinatario, int idVenta, byte[] pdfBytes);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarCorreoRecuperacionAsync(string destinatario, string token)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]
            ));
            email.To.Add(new MailboxAddress("", destinatario));
            email.Subject = "AgroSystem - Código de Recuperación de Contraseña";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2>Recuperación de Contraseña</h2>
                    <p>Has solicitado restablecer tu contraseña en AgroSystem.</p>
                    <p>Tu código de seguridad de 6 dígitos es:</p>
                    <h1 style='color: #2E7D32; background: #e8f5e9; padding: 10px; width: fit-content; border-radius: 5px;'>
                        {token}
                    </h1>
                    <p>Este código <strong>expirará en 15 minutos</strong>.</p>
                    <p>Si no fuiste tú quien solicitó esto, ignora este mensaje.</p>
                    <hr/>
                    <small style='color: #777;'>Soporte Técnico AgroSystem</small>
                </div>";

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailSettings:SmtpUsername"], _config["EmailSettings:SmtpPassword"]);
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al conectar con el servidor de correo: " + ex.Message);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }

        public async Task EnviarComprobanteVentaAsync(string destinatario, int idVenta, byte[] pdfBytes)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:SenderName"],
                _config["EmailSettings:SenderEmail"]
            ));
            email.To.Add(new MailboxAddress("", destinatario));
            email.Subject = $"Agro Verde - Comprobante de Venta #{idVenta}";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                    <h2>Comprobante de Venta #{idVenta}</h2>
                    <p>Adjunto encontrarás el comprobante de tu compra en Agro Verde.</p>
                    <p>Gracias por tu preferencia.</p>
                    <hr/>
                    <small style='color: #777;'>Este comprobante no tiene validez tributaria ante el SRI.</small>
                </div>";

            builder.Attachments.Add($"Comprobante_Venta_{idVenta}.pdf", pdfBytes, new ContentType("application", "pdf"));

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailSettings:SmtpUsername"], _config["EmailSettings:SmtpPassword"]);
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al conectar con el servidor de correo: " + ex.Message);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}