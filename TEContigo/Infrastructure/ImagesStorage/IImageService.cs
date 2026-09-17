namespace TEContigo.Infrastructure.ImagesStorage
{
    public interface IImageService
    {
        Task<string> UploadAsync(IFormFile file,string folder);

        Task<string?> GetUrlAsync(string? photoPath);

        Task DeleteAsync(string? photoPath);
    }
}
