using TEContigo.Modules.ContactRequests.DTOs;

namespace TEContigo.Modules.ContactRequests.Services
{
    public interface IContactRequestsService
    {
        Task<IEnumerable<ContactRequestDto>> GetAllForCurrentUserAsync();
        Task<ContactRequestDto?> GetByIdAsync(long id);
        Task<ContactRequestResponseDto> CreateAsync(CreateContactRequestDto dto);
        Task<ContactRequestResponseDto> AcceptAsync(long id);
        Task<ContactRequestResponseDto> RejectAsync(long id);
        Task<ContactRequestResponseDto> CancelAsync(long id);
    }
}
