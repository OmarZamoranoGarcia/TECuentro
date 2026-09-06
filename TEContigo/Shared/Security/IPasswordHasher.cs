namespace TEContigo.Shared.Security
{
    public interface IPasswordHasher
    {
        Task<string> HashAsync(string password);
        Task<bool> VerifyAsync(string password, string hash);
    }
}
