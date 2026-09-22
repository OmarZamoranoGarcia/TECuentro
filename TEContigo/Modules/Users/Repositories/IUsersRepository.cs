using TEContigo.Modules.Auth.Repositories;
using TEContigo.Modules.Users.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Users.Repositories
{
    public interface IUsersRepository
    {
        Task<PagedResultDto<UsersModel>> GetAllAsync(int pageNumber, int pageSize);

        Task<UsersModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(UsersModel user);

        Task UpdateAsync(UsersModel user);

        Task DeleteAsync(long id);
    }
}
