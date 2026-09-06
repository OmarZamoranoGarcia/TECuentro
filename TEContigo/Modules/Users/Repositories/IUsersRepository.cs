using TEContigo.Modules.Auth.Repositories;
using TEContigo.Modules.Users.Models;

namespace TEContigo.Modules.Users.Repositories
{
    public interface IUsersRepository
    {
        Task<IEnumerable<UsersModel>> GetAllAsync();

        Task<UsersModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(UsersModel user);

        Task UpdateAsync(UsersModel user);

        Task DeleteAsync(long id);
    }
}
