using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

using TEContigo.Modules.Email.DTOs;
using TEContigo.Infrastructure.Database;

namespace TEContigo.Modules.Email.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public EmailService(IConfiguration configuration, IDbConnectionFactory dbConnectionFactory)
        {
            _configuration = configuration;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task SendVerificationEmailAsync(VerificationEmailDto dto)
        {
            var smtpSettings = _configuration.GetSection("Email");

            var host = smtpSettings["Host"]
                ?? throw new InvalidOperationException(
                    "Email Host no configurado.");

            var port = int.Parse(
                smtpSettings["Port"]
                ?? throw new InvalidOperationException(
                    "Email Port no configurado."));

            var username = smtpSettings["Username"]
                ?? throw new InvalidOperationException(
                    "Email Username no configurado.");

            var password = smtpSettings["Password"]
                ?? throw new InvalidOperationException(
                    "Email Password no configurado.");

            var fromName = smtpSettings["FromName"]
                ?? "Avisos Tec";

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    fromName,
                    username));

            message.To.Add(
                MailboxAddress.Parse(dto.To));

            message.Subject =
                "Código de verificación - Avisos Tec";

            var body = new BodyBuilder
            {
                HtmlBody = $"""
                <h2>Verificación de correo</h2>

                <p>
                    Gracias por registrarte en Avisos Tec.
                </p>

                <p>
                    Tu código de verificación es:
                </p>

                <h1>{dto.Code}</h1>

                <p>
                    Este código expirará en
                    <strong>5 minutos</strong>.
                </p>

                <p>
                    Si no realizaste este registro,
                    puedes ignorar este correo.
                </p>

                <p>
                    Favor de no responder a este correo.
                </p>
                """
            };

            message.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                host,
                port,
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                username,
                password);

            await smtp.SendAsync(message);

            await smtp.DisconnectAsync(true);
        }
    }
}
