using backend.DTOs.Files;

namespace backend.Services.Interfaces;

public interface IFileStorageService
{
    PresignResponse GeneratePresignedUploadUrl(string contentType, Guid targetUserId);
    Task<PresignResponse> UploadFileAsync(Stream stream, string contentType, string fileName, Guid targetUserId, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string objectKey, CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}
