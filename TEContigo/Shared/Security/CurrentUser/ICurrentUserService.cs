namespace TEContigo.Shared.Security.CurrentUser
{
    public interface ICurrentUserService
    {
        long UserId { get; }

        string Role { get; }

        bool IsAuthenticated { get; }

        bool IsInRole(string role);
    }
}
