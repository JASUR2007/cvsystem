using backend.DTOs.Files;

namespace backend.Services.Interfaces;

public interface IFileStorageService
{
    PresignResponse GeneratePresignedUploadUrl(string contentType, Guid targetUserId);
    Task DeleteFileAsync(string objectKey, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}
