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
            Expires = DateTime.UtcNow.AddMinutes(15)
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

        // Buffer stream into memory so both S3 and fallback can safely read it repeatedly
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);

        if (IsConfigured)
        {
            try
            {
                ms.Position = 0;
                var request = new PutObjectRequest
                {
                    BucketName = Bucket,
                    Key = key,
                    InputStream = ms,
                    ContentType = contentType
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
        try
        {
            var localKey = $"uploads/users/{targetUserId}/{Guid.NewGuid():N}{extension}";
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", localKey.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            ms.Position = 0;
            await using var fileStream = File.Create(localPath);
            await ms.CopyToAsync(fileStream, cancellationToken);
            return new PresignResponse($"/{localKey}", string.Empty, $"/{localKey}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Local file write failed: {ex.Message}. Falling back to Base64 Data URI.");
            var base64 = Convert.ToBase64String(ms.ToArray());
            var dataUri = $"data:{contentType};base64,{base64}";
            return new PresignResponse(dataUri, string.Empty, dataUri);
        }
    }

    public async Task DeleteFileAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (objectKey.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return;

        if (objectKey.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || objectKey.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            var localKey = objectKey.TrimStart('/');
            var localPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", localKey.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localPath))
            {
                try { File.Delete(localPath); } catch { /* ignore */ }
            }
            return;
        }

        if (!IsConfigured)
            return;

        try
        {
            await S3Client!.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = Bucket,
                Key = objectKey.TrimStart('/')
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: S3 DeleteObjectAsync failed: {ex.Message}");
        }
    }
}
