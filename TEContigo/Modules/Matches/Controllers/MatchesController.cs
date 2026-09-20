using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEContigo.Modules.Matches.Services;

namespace TEContigo.Modules.Matches.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchesService _matchesService;

        public MatchesController(IMatchesService matchesService)
        {
            _matchesService = matchesService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var matches = await _matchesService.GetAllForCurrentUserAsync();

            return Ok(matches);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var match = await _matchesService.GetByIdAsync(id);

            return Ok(match);
        }

        [HttpPost("{id:long}/request-chat")]
        public async Task<IActionResult> RequestChat(long id)
        {
            var result = await _matchesService.RequestChatAsync(id);

            return Ok(result);
        }

        [HttpPost("{id:long}/confirm-return")]
        public async Task<IActionResult> ConfirmReturn(long id)
        {
            var result = await _matchesService.ConfirmReturnAsync(id);

            return Ok(result);
        }
    }
}
