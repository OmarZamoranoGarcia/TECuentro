using TEContigo.Modules.Users.DTOs;
using TEContigo.Modules.Users.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Users.Services
{
    public interface IUsersService
    {
        Task<PagedResultDto<UsersModel>> GetAllAsync(int pageNumber, int pageSize);

        Task<UsersModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(CreateUserDto dto);

        Task UpdateAsync(long id, UpdateUserDto dto);

        Task DeleteAsync(long id);
    }
}
