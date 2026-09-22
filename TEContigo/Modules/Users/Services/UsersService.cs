using TEContigo.Modules.Users.DTOs;
using TEContigo.Modules.Users.Models;
using TEContigo.Modules.Users.Repositories;
using TEContigo.Shared.Pagination;
using TEContigo.Shared.Security.Password;

namespace TEContigo.Modules.Users.Services
{
    public class UsersService : IUsersService
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IPasswordHasher _passwordHasher;

        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 50;

        public UsersService(
            IUsersRepository usersRepository,
            IPasswordHasher passwordHasher)
        {
            _usersRepository = usersRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<PagedResultDto<UsersModel>> GetAllAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize < 1)
            {
                pageSize = DefaultPageSize;
            }
            else if (pageSize > MaxPageSize)
            {
                pageSize = MaxPageSize;
            }

            return await _usersRepository.GetAllAsync(pageNumber, pageSize);
        }

        public async Task<UsersModel?> GetByIdAsync(long id)
        {
            var user = await _usersRepository.GetByIdAsync(id);

            if (user == null)
            {
                throw new KeyNotFoundException(
                    "El usuario no existe.");
            }

            return user;
        }

        public async Task<long> CreateAsync(CreateUserDto dto)
        {
            var passwordHash =
                await _passwordHasher.HashAsync(dto.Password);

            var user = new UsersModel
            {
                ControlNumber = dto.ControlNumber,
                FirstName = dto.FirstName,
                LastNamePaternal = dto.LastNamePaternal,
                LastNameMaternal = dto.LastNameMaternal,
                Email = dto.Email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                Role = "USER",
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            return await _usersRepository.CreateAsync(user);
        }

        public async Task UpdateAsync(long id,UpdateUserDto dto)
        {
            var existingUser =
                await _usersRepository.GetByIdAsync(id);

            if (existingUser == null)
            {
                throw new KeyNotFoundException(
                    "El usuario no existe.");
            }

            existingUser.ControlNumber = dto.ControlNumber;
            existingUser.FirstName = dto.FirstName;
            existingUser.LastNamePaternal = dto.LastNamePaternal;
            existingUser.LastNameMaternal = dto.LastNameMaternal;
            existingUser.Email = dto.Email.Trim().ToLowerInvariant();

            await _usersRepository.UpdateAsync(existingUser);
        }

        public async Task DeleteAsync(long id)
        {
            var existingUser =
                await _usersRepository.GetByIdAsync(id);

            if (existingUser == null)
            {
                throw new KeyNotFoundException(
                    "El usuario no existe.");
            }

            await _usersRepository.DeleteAsync(id);
        }
    }
}
