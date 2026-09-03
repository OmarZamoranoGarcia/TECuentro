namespace TEContigo.Modules.Email.DTOs
{
    public class VerificationEmailDto
    {
        public string To { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}
