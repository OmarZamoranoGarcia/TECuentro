using TEContigo.Modules.Email.DTOs;

namespace TEContigo.Modules.Email.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(VerificationEmailDto dto);
    }
}
