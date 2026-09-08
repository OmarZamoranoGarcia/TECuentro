namespace TEContigo.Shared.Security.Password
{
    public interface IPasswordHasher
    {
        Task<string> HashAsync(string password);
        Task<bool> VerifyAsync(string password, string hash);
    }
}
