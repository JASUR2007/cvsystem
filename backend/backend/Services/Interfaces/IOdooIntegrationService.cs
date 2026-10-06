using backend.DTOs.Integrations;

namespace backend.Services.Interfaces;

public interface IOdooIntegrationService {
    Task<PositionTokenResponse> GenerateTokenAsync(Guid positionId, CancellationToken cancellationToken = default);
    Task<PositionTokenStatusResponse> GetTokenStatusAsync(Guid positionId, CancellationToken cancellationToken = default);
    Task<PositionAggregatedResultView> GetPositionResultsByTokenAsync(string rawToken, CancellationToken cancellationToken = default);
}
