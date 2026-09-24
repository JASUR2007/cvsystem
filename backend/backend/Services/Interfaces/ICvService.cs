using backend.DTOs.Attributes;
using backend.DTOs.Cvs;
using backend.DTOs.Profile;

namespace backend.Services.Interfaces;

public interface ICvService {
    Task<List<CvListItem>> ListCandidateCvsAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<CvDetail> CreateCvAsync(Guid positionId, Guid candidateId, CancellationToken cancellationToken = default);
    Task<CvDetail> GetCvDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttributeValueView> UpdateCvAttributeAsync(Guid id, Guid attributeId, AttributeValueUpdate request, CancellationToken cancellationToken = default);
    Task<object> PublishCvAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteCvAsync(Guid id, CancellationToken cancellationToken = default);
}
