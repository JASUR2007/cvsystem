using backend.DTOs.Salesforce;

namespace backend.Services.Interfaces;

public interface ISalesforceService {
    Task<SalesforceExportResult> ExportUserAsync(Guid userId, SalesforceExportRequest request, CancellationToken cancellationToken = default);
}
