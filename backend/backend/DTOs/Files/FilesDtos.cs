namespace backend.DTOs.Files;

public sealed record PresignRequest(string ContentType);

public sealed record PresignResponse(string ObjectKey, string UploadUrl, string? PublicUrl);
