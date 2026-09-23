using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Profile;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ProfileService(AppDbContext db) : IProfileService
{
    public async Task<ProfileView> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("User profile not found.");

        return ToView(user);
    }

    public async Task<ProfileView> UpdateProfileAsync(Guid userId, ProfileUpdate request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new ValidationException("First and last names are required.");

        if (request.FirstName.Trim().Length > 100 || request.LastName.Trim().Length > 100)
            throw new ValidationException("Names must not exceed 100 characters.");

        if (request.Location?.Length > 200)
            throw new ValidationException("Location must not exceed 200 characters.");

        if (!AttributeValueHelper.ValidImageKey(request.PhotoObjectKey, userId))
            throw new ValidationException("Image must belong to this profile.");

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("User profile not found.");

        if (user.Version != request.Version)
            throw new ConflictException("Profile was changed in another session.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Location = request.Location?.Trim();
        user.PhotoObjectKey = request.PhotoObjectKey?.Trim();
        user.Version++;

        await db.SaveChangesAsync(cancellationToken);
        return ToView(user);
    }

    public async Task<List<AttributeValueView>> GetProfileAttributesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("User profile not found.");

        var values = await db.UserAttributeValues.AsNoTracking()
            .Where(value => value.UserId == userId)
            .Include(value => value.Attribute)
            .ToListAsync(cancellationToken);

        var builtIns = await db.Attributes.AsNoTracking().Where(attribute => attribute.IsBuiltIn).ToListAsync(cancellationToken);

        return builtIns.Select(attribute => AttributeValueHelper.View(attribute, null, user))
            .Concat(values.Select(value => AttributeValueHelper.View(value.Attribute, value, user)))
            .OrderBy(value => value.Category).ThenBy(value => value.Name).ToList();
    }

    public async Task<AttributeValueView> AddProfileAttributeAsync(Guid userId, Guid attributeId, CancellationToken cancellationToken = default)
    {
        var attribute = await db.Attributes.FindAsync([attributeId], cancellationToken);
        if (attribute is null)
            throw new NotFoundException("Attribute not found.");

        if (attribute.IsBuiltIn)
            throw new ValidationException("Built-in attributes already belong to the profile.");

        if (await db.UserAttributeValues.AnyAsync(value => value.UserId == userId && value.AttributeId == attributeId, cancellationToken))
            throw new ConflictException("Attribute already added.");

        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        var value = new UserAttributeValue { UserId = userId, AttributeId = attributeId };
        db.UserAttributeValues.Add(value);
        await db.SaveChangesAsync(cancellationToken);

        return AttributeValueHelper.View(attribute, value, user);
    }

    public async Task<AttributeValueView> UpdateProfileAttributeAsync(Guid userId, Guid attributeId, AttributeValueUpdate request, CancellationToken cancellationToken = default)
    {
        var value = await db.UserAttributeValues.Include(item => item.Attribute).ThenInclude(attribute => attribute.Options)
            .FirstOrDefaultAsync(item => item.UserId == userId && item.AttributeId == attributeId, cancellationToken);
        if (value is null)
            throw new NotFoundException("Attribute value not found.");

        if (value.Version != request.Version)
            throw new ConflictException("Attribute value was changed in another session.");

        var error = AttributeValueHelper.Validate(value.Attribute, request.Value);
        if (error is not null)
            throw new ValidationException(error);

        if (!AttributeValueHelper.ValidImageKey(request.Value.ImageObjectKey, userId))
            throw new ValidationException("Image must belong to this profile.");

        AttributeValueHelper.Apply(value, value.Attribute, request.Value);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.AsNoTracking().FirstAsync(item => item.Id == userId, cancellationToken);
        return AttributeValueHelper.View(value.Attribute, value, user);
    }

    public async Task RemoveProfileAttributeAsync(Guid userId, Guid attributeId, CancellationToken cancellationToken = default)
    {
        var value = await db.UserAttributeValues.FindAsync([userId, attributeId], cancellationToken);
        if (value is null)
            throw new NotFoundException("Attribute value not found.");

        if (await db.Cvs.AnyAsync(cv => cv.CandidateId == userId && cv.Position.Attributes.Any(attribute => attribute.AttributeId == attributeId), cancellationToken))
            throw new ConflictException("Attribute is required by an existing CV.");

        db.UserAttributeValues.Remove(value);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static ProfileView ToView(AppUser user) => new(
        user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty,
        user.Location, user.PhotoObjectKey, user.Language, user.Theme, user.Version);
}
