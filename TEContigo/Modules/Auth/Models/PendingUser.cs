namespace TEContigo.Modules.Auth.Models
{
    public class PendingUser
    {
        public long Id { get; set; }

        public long ControlNumber { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastNamePaternal { get; set; } = string.Empty;

        public string LastNameMaternal { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string VerificationCodeHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
