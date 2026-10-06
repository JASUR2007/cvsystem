namespace backend.DTOs.Salesforce;

public sealed record SalesforceExportRequest(
    string? AccountName,
    string? Phone,
    string? Title,
    string? Industry,
    string? Description
);

public sealed record SalesforceExportResult(
    string AccountId,
    string ContactId,
    string InstanceUrl,
    string Message
);
