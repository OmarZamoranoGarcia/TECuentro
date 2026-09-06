using TEContigo.Modules.Users.Models;
using TEContigo.Modules.Users.DTOs;

namespace TEContigo.Modules.Users.Services
{
    public interface IUsersService
    {
        Task<IEnumerable<UsersModel>> GetAllAsync();

        Task<UsersModel?> GetByIdAsync(long id);

        Task<long> CreateAsync(CreateUserDto dto);

        Task UpdateAsync(long id, UpdateUserDto dto);

        Task DeleteAsync(long id);
    }
}
