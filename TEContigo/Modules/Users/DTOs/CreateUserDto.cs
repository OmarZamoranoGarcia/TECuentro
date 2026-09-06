namespace TEContigo.Modules.Users.DTOs
{
    public class CreateUserDto
    {
        public string ControlNumber { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastNamePaternal { get; set; } = string.Empty;

        public string LastNameMaternal { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
