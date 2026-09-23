using Amazon.S3;
using Amazon.S3.Model;
using backend.Api;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController(IServiceProvider services, IConfiguration configuration, AppDbContext db) : ControllerBase
{
    [HttpPost("presign")]
    public IActionResult Presign(PresignRequest request, Guid? userId)
    {
        var s3 = services.GetService<IAmazonS3>();
        var bucket = configuration["S3:Bucket"];
        if (s3 is null || string.IsNullOrWhiteSpace(bucket)) return StatusCode(503, new { message = "Image storage is not configured." });
        var extension = request.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null
        };
        if (extension is null) return BadRequest(new { message = "Only JPEG, PNG and WebP images are supported." });
        var id = ApiModels.UserId(User);
        if (id is null) return Unauthorized();
        var targetId = ApiModels.IsAdmin(User) && userId is not null ? userId.Value : id.Value;
        var key = $"users/{targetId}/{Guid.NewGuid():N}{extension}";
        var upload = s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = request.ContentType,
            Expires = DateTime.UtcNow.AddMinutes(10)
        });
        var publicBase = configuration["S3:PublicBaseUrl"]?.TrimEnd('/');
        return Ok(new { objectKey = key, uploadUrl = upload, publicUrl = publicBase is null ? null : $"{publicBase}/{key}" });
    }

    [HttpDelete("{**objectKey}")]
    public async Task<IActionResult> Delete(string objectKey, CancellationToken cancellationToken)
    {
        var s3 = services.GetService<IAmazonS3>();
        var bucket = configuration["S3:Bucket"];
        if (s3 is null || string.IsNullOrWhiteSpace(bucket)) return StatusCode(503, new { message = "Image storage is not configured." });
        var id = ApiModels.UserId(User);
        if (id is null || (!ApiModels.IsAdmin(User) && !objectKey.StartsWith($"users/{id}/", StringComparison.Ordinal))) return Forbid();
        if (await db.Users.AnyAsync(user => user.PhotoObjectKey == objectKey, cancellationToken)
            || await db.UserAttributeValues.AnyAsync(value => value.ImageObjectKey == objectKey, cancellationToken))
            return Conflict(new { message = "Image is still in use." });
        await s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = bucket, Key = objectKey }, cancellationToken);
        return NoContent();
    }
}

public sealed record PresignRequest(string ContentType);
