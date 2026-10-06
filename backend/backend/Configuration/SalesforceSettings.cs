namespace backend.Configuration;

public sealed class SalesforceSettings {
    public const string SectionName = "Salesforce";

    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;
    public string Domain { get; set; } = "https://login.salesforce.com";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? SecurityToken { get; set; }
}
