using Amazon.S3;
using Amazon.S3.Model;
using backend.Common.Exceptions;
using backend.DTOs.Files;
using backend.Services.Interfaces;

namespace backend.Storage;

public class AcdnS3Service(IServiceProvider services, IConfiguration configuration) : IFileStorageService
{
    private IAmazonS3? S3Client => services.GetService<IAmazonS3>();
    private string? Bucket => configuration["S3:Bucket"];
    private string? PublicBaseUrl => configuration["S3:PublicBaseUrl"]?.TrimEnd('/');

    public bool IsConfigured => S3Client is not null && !string.IsNullOrWhiteSpace(Bucket);

    public PresignResponse GeneratePresignedUploadUrl(string contentType, Guid targetUserId)
    {
        if (!IsConfigured)
            throw new ValidationException("Image storage is not configured.");

        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new ValidationException("Only JPEG, PNG and WebP images are supported.")
        };

        var key = $"users/{targetUserId}/{Guid.NewGuid():N}{extension}";
        var uploadUrl = S3Client!.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(15),
            ContentType = contentType
        });

        var publicUrl = PublicBaseUrl is null ? null : $"{PublicBaseUrl}/{key}";
        return new PresignResponse(key, uploadUrl, publicUrl);
    }

    public async Task<PresignResponse> UploadFileAsync(Stream stream, string contentType, string fileName, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => ".jpg",
                ".png" => ".png",
                ".webp" => ".webp",
                _ => throw new ValidationException("Only JPEG, PNG and WebP images are supported.")
            }
        };

        var key = $"users/{targetUserId}/{Guid.NewGuid():N}{extension}";

        if (IsConfigured)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                    DisablePayloadSigning = true
                };

                await S3Client!.PutObjectAsync(request, cancellationToken);
                var publicUrl = PublicBaseUrl is null ? null : $"{PublicBaseUrl}/{key}";
                return new PresignResponse(key, string.Empty, publicUrl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: S3 PutObjectAsync failed: {ex.Message}. Falling back to local storage.");
            }
        }

        // Guaranteed fallback to local static storage
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", key.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        await using var fileStream = File.Create(localPath);
        await stream.CopyToAsync(fileStream, cancellationToken);
        return new PresignResponse(key, string.Empty, $"/uploads/{key}");
    }

    public async Task DeleteFileAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new ValidationException("Image storage is not configured.");

        await S3Client!.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = Bucket,
            Key = objectKey
        }, cancellationToken);
    }
}
