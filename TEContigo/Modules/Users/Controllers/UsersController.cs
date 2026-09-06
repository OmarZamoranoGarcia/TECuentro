using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.Users.DTOs;
using TEContigo.Modules.Users.Services;

namespace TEContigo.Modules.Users.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN,MODERATOR")]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _usersService.GetAllAsync();

            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetUser(long id)
        {
            var result = await _usersService.GetByIdAsync(id);

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            var userId = await _usersService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetUser),
                new { id = userId },
                new
                {
                    id = userId,
                    message = "Usuario creado correctamente."
                });
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateUser(
            long id,
            UpdateUserDto dto)
        {
            await _usersService.UpdateAsync(id, dto);

            return NoContent();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            await _usersService.DeleteAsync(id);

            return NoContent();
        }
    }
}