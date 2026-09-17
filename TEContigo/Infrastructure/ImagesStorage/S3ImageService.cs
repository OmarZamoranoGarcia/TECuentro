using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using TEContigo.Infrastructure.ImagesStorage;

namespace TEContigo.Infrastructure.ImagesStorage;

public class S3ImageService : IImageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

    public S3ImageService(
        IAmazonS3 s3Client,
        IConfiguration configuration)
    {
        _s3Client = s3Client;

        _bucketName =
            configuration["AWS:BucketName"]
            ?? throw new InvalidOperationException(
                "AWS:BucketName no está configurado.");
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string folder)
    {
        await ValidateFileAsync(file);

        var extension = GetExtension(file.ContentType);

        var fileName = $"{Guid.NewGuid():N}{extension}";

        var photoPath =
            $"{folder.TrimEnd('/')}/{fileName}";

        await using var stream = file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = photoPath,
            InputStream = stream,
            ContentType = file.ContentType
        };

        await _s3Client.PutObjectAsync(request);

        return photoPath;
    }

    public async Task<string?> GetUrlAsync(
        string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return null;
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = photoPath,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        return await _s3Client.GetPreSignedURLAsync(request);
    }

    public async Task DeleteAsync(
        string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return;
        }

        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = photoPath
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    private static async Task ValidateFileAsync(
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException(
                "La imagen no puede estar vacía.");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException(
                "La imagen no puede superar los 5 MB.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ArgumentException(
                "El formato de imagen no es válido.");
        }

        await ValidateMagicBytesAsync(file);
    }

    private static async Task ValidateMagicBytesAsync(
        IFormFile file)
    {
        await using var stream = file.OpenReadStream();

        var header = new byte[12];

        var bytesRead = await stream.ReadAsync(header);

        if (bytesRead < 12)
        {
            throw new ArgumentException(
                "El archivo de imagen no es válido.");
        }

        var isValid = file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => IsJpeg(header),

            "image/png" => IsPng(header),

            "image/webp" => IsWebP(header),

            _ => false
        };

        if (!isValid)
        {
            throw new ArgumentException(
                "El contenido del archivo no corresponde a una imagen válida.");
        }
    }

    private static bool IsJpeg(byte[] header)
    {
        return header[0] == 0xFF &&
               header[1] == 0xD8 &&
               header[2] == 0xFF;
    }

    private static bool IsPng(byte[] header)
    {
        return header[0] == 0x89 &&
               header[1] == 0x50 &&
               header[2] == 0x4E &&
               header[3] == 0x47 &&
               header[4] == 0x0D &&
               header[5] == 0x0A &&
               header[6] == 0x1A &&
               header[7] == 0x0A;
    }

    private static bool IsWebP(byte[] header)
    {
        return header[0] == 0x52 &&
               header[1] == 0x49 &&
               header[2] == 0x46 &&
               header[3] == 0x46 &&
               header[8] == 0x57 &&
               header[9] == 0x45 &&
               header[10] == 0x42 &&
               header[11] == 0x50;
    }

    private static string GetExtension(
        string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",

            _ => throw new ArgumentException(
                "Formato de imagen no permitido.")
        };
    }
}