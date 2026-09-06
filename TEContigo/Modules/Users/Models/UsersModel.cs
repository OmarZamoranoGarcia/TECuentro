namespace TEContigo.Modules.Users.Models
{
    public class UsersModel
    {
        public long Id { get; set; }

        public string ControlNumber { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastNamePaternal { get; set; } = string.Empty;

        public string LastNameMaternal { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool EmailVerified { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
