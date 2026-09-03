using System.ComponentModel.DataAnnotations;

namespace TEContigo.Modules.Auth.DTOs
{
    public class RegisterDto
    {
        public long ControlNumber { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastNamePaternal { get; set; } = string.Empty;

        public string LastNameMaternal { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}