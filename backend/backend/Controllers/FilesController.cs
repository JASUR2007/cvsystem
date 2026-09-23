using backend.Auth;
using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Files;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController(
    IFileStorageService storageService,
    ICurrentUserService currentUser,
    AppDbContext db) : ControllerBase
{
    [HttpPost("presign")]
    public ActionResult<PresignResponse> Presign(PresignRequest request, Guid? userId)
    {
        var id = currentUser.RequireUserId();
        var targetId = currentUser.IsAdmin && userId is not null ? userId.Value : id;

        var result = storageService.GeneratePresignedUploadUrl(request.ContentType, targetId);
        return Ok(result);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PresignResponse>> Upload([FromForm] IFormFile? file, [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("No file provided.");

        if (file.Length > 5 * 1024 * 1024)
            throw new ValidationException("File size exceeds 5 MB limit.");

        var contentType = file.ContentType?.ToLowerInvariant() ?? "application/octet-stream";
        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(contentType))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => throw new ValidationException("Only JPEG, PNG and WebP images are supported.")
            };
        }

        var id = currentUser.RequireUserId();
        var targetId = currentUser.IsAdmin && userId is not null ? userId.Value : id;

        await using var stream = file.OpenReadStream();
        var result = await storageService.UploadFileAsync(stream, contentType, file.FileName, targetId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{**objectKey}")]
    public async Task<IActionResult> Delete(string objectKey, CancellationToken cancellationToken)
    {
        var id = currentUser.RequireUserId();
        var normalizedKey = objectKey.TrimStart('/');
        if (!currentUser.IsAdmin && !normalizedKey.StartsWith($"users/{id}/", StringComparison.Ordinal) && !normalizedKey.StartsWith($"uploads/users/{id}/", StringComparison.Ordinal))
            throw new ForbiddenException();

        if (await db.Users.AnyAsync(user => user.PhotoObjectKey == objectKey, cancellationToken)
            || await db.UserAttributeValues.AnyAsync(value => value.ImageObjectKey == objectKey, cancellationToken))
            throw new ConflictException("Image is still in use.");

        await storageService.DeleteFileAsync(objectKey, cancellationToken);
        return NoContent();
    }
}
