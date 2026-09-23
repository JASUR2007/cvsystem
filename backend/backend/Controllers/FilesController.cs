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

    [HttpDelete("{**objectKey}")]
    public async Task<IActionResult> Delete(string objectKey, CancellationToken cancellationToken)
    {
        var id = currentUser.RequireUserId();
        if (!currentUser.IsAdmin && !objectKey.StartsWith($"users/{id}/", StringComparison.Ordinal))
            throw new ForbiddenException();

        if (await db.Users.AnyAsync(user => user.PhotoObjectKey == objectKey, cancellationToken)
            || await db.UserAttributeValues.AnyAsync(value => value.ImageObjectKey == objectKey, cancellationToken))
            throw new ConflictException("Image is still in use.");

        await storageService.DeleteFileAsync(objectKey, cancellationToken);
        return NoContent();
    }
}
