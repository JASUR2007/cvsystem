using backend.DTOs.Profile;

namespace backend.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileView> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProfileView> UpdateProfileAsync(Guid userId, ProfileUpdate request, CancellationToken cancellationToken = default);
    Task<List<AttributeValueView>> GetProfileAttributesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AttributeValueView> AddProfileAttributeAsync(Guid userId, Guid attributeId, CancellationToken cancellationToken = default);
    Task<AttributeValueView> UpdateProfileAttributeAsync(Guid userId, Guid attributeId, AttributeValueUpdate request, CancellationToken cancellationToken = default);
    Task RemoveProfileAttributeAsync(Guid userId, Guid attributeId, CancellationToken cancellationToken = default);
}
