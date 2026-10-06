using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using backend.Common.Exceptions;
using backend.Configuration;
using backend.DTOs.Salesforce;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace backend.Services;

public sealed class SalesforceService(
    IHttpClientFactory httpClientFactory,
    IOptions<SalesforceSettings> settingsOptions,
    UserManager<AppUser> userManager,
    ILogger<SalesforceService> logger) : ISalesforceService {

    private readonly SalesforceSettings _settings = settingsOptions.Value;

    public async Task<SalesforceExportResult> ExportUserAsync(Guid userId, SalesforceExportRequest request, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(_settings.ConsumerKey) || string.IsNullOrWhiteSpace(_settings.ConsumerSecret)) {
            throw new ValidationException("Salesforce integration is not configured. Please ensure Salesforce:ConsumerKey and Salesforce:ConsumerSecret are configured.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) {
            throw new NotFoundException("User not found.");
        }

        var httpClient = httpClientFactory.CreateClient("Salesforce");

        // 1. Authenticate with Salesforce OAuth 2.0
        var (accessToken, instanceUrl) = await AuthenticateAsync(httpClient, cancellationToken);

        // 2. Create Account in Salesforce
        var accountId = await CreateAccountAsync(httpClient, accessToken, instanceUrl, user, request, cancellationToken);

        // 3. Create Contact in Salesforce linked to Account
        var contactId = await CreateContactAsync(httpClient, accessToken, instanceUrl, accountId, user, request, cancellationToken);

        logger.LogInformation("Successfully created Salesforce Account {AccountId} and Contact {ContactId} for user {UserId}", accountId, contactId, userId);

        return new SalesforceExportResult(
            AccountId: accountId,
            ContactId: contactId,
            InstanceUrl: instanceUrl,
            Message: $"Successfully exported user to Salesforce CRM. Account ID: {accountId}, Contact ID: {contactId}."
        );
    }

    private async Task<(string AccessToken, string InstanceUrl)> AuthenticateAsync(HttpClient httpClient, CancellationToken cancellationToken) {
        var domain = string.IsNullOrWhiteSpace(_settings.Domain) ? "https://login.salesforce.com" : _settings.Domain.Trim();
        if (!domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
            domain = $"https://{domain}";
        }
        var tokenUrl = $"{domain.TrimEnd('/')}/services/oauth2/token";

        var formParams = new Dictionary<string, string> {
            ["client_id"] = _settings.ConsumerKey,
            ["client_secret"] = _settings.ConsumerSecret
        };

        if (!string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Password)) {
            formParams["grant_type"] = "password";
            formParams["username"] = _settings.Username;
            formParams["password"] = _settings.Password + (_settings.SecurityToken ?? "");
        }
        else {
            formParams["grant_type"] = "client_credentials";
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, tokenUrl) {
            Content = new FormUrlEncodedContent(formParams)
        };

        HttpResponseMessage response;
        try {
            response = await httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to connect to Salesforce token endpoint at {TokenUrl}", tokenUrl);
            throw new ValidationException($"Failed to reach Salesforce server: {ex.Message}");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Salesforce OAuth error (status {StatusCode}): {ResponseContent}", response.StatusCode, content);
            var errorMessage = ExtractErrorMessage(content, "OAuth authentication failed.");
            throw new ValidationException($"Salesforce authentication failed: {errorMessage}");
        }

        try {
            var jsonNode = JsonNode.Parse(content);
            var token = jsonNode?["access_token"]?.GetValue<string>();
            var instanceUrl = jsonNode?["instance_url"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(instanceUrl)) {
                throw new ValidationException("Invalid OAuth token response received from Salesforce.");
            }

            return (token, instanceUrl.TrimEnd('/'));
        }
        catch (JsonException ex) {
            logger.LogError(ex, "Failed to parse Salesforce OAuth token response");
            throw new ValidationException("Failed to parse Salesforce token response.");
        }
    }

    private async Task<string> CreateAccountAsync(
        HttpClient httpClient,
        string accessToken,
        string instanceUrl,
        AppUser user,
        SalesforceExportRequest request,
        CancellationToken cancellationToken) {

        var accountEndpoint = $"{instanceUrl}/services/data/v60.0/sobjects/Account";

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var accountName = !string.IsNullOrWhiteSpace(request.AccountName)
            ? request.AccountName.Trim()
            : (!string.IsNullOrWhiteSpace(fullName) ? $"{fullName} (TalentHub)" : (user.Email ?? "TalentHub User"));

        var accountPayload = new Dictionary<string, object?> {
            ["Name"] = accountName
        };

        var phone = !string.IsNullOrWhiteSpace(request.Phone) ? request.Phone.Trim() : user.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(phone)) {
            accountPayload["Phone"] = phone;
        }

        if (!string.IsNullOrWhiteSpace(user.Location)) {
            accountPayload["BillingCity"] = user.Location;
        }

        if (!string.IsNullOrWhiteSpace(request.Industry)) {
            accountPayload["Industry"] = request.Industry.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Description)) {
            accountPayload["Description"] = request.Description.Trim();
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, accountEndpoint) {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = new StringContent(JsonSerializer.Serialize(accountPayload), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Salesforce Account creation error (status {StatusCode}): {ResponseContent}", response.StatusCode, content);
            var errorMessage = ExtractErrorMessage(content, "Failed to create Account in Salesforce.");
            throw new ValidationException($"Salesforce Account error: {errorMessage}");
        }

        var jsonNode = JsonNode.Parse(content);
        var accountId = jsonNode?["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(accountId)) {
            throw new ValidationException("Salesforce did not return an Account ID.");
        }

        return accountId;
    }

    private async Task<string> CreateContactAsync(
        HttpClient httpClient,
        string accessToken,
        string instanceUrl,
        string accountId,
        AppUser user,
        SalesforceExportRequest request,
        CancellationToken cancellationToken) {

        var contactEndpoint = $"{instanceUrl}/services/data/v60.0/sobjects/Contact";

        var lastName = !string.IsNullOrWhiteSpace(user.LastName)
            ? user.LastName.Trim()
            : (!string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName.Trim() : "TalentHub User");

        var firstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName.Trim() : null;

        var contactPayload = new Dictionary<string, object?> {
            ["AccountId"] = accountId,
            ["LastName"] = lastName,
            ["LeadSource"] = "TalentHub Platform"
        };

        if (!string.IsNullOrWhiteSpace(firstName)) {
            contactPayload["FirstName"] = firstName;
        }

        if (!string.IsNullOrWhiteSpace(user.Email)) {
            contactPayload["Email"] = user.Email.Trim();
        }

        var phone = !string.IsNullOrWhiteSpace(request.Phone) ? request.Phone.Trim() : user.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(phone)) {
            contactPayload["Phone"] = phone;
        }

        if (!string.IsNullOrWhiteSpace(request.Title)) {
            contactPayload["Title"] = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.Location)) {
            contactPayload["MailingCity"] = user.Location;
        }

        if (!string.IsNullOrWhiteSpace(request.Description)) {
            contactPayload["Description"] = request.Description.Trim();
        }

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, contactEndpoint) {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
            Content = new StringContent(JsonSerializer.Serialize(contactPayload), Encoding.UTF8, "application/json")
        };

        var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Salesforce Contact creation error (status {StatusCode}): {ResponseContent}", response.StatusCode, content);
            var errorMessage = ExtractErrorMessage(content, "Failed to create Contact in Salesforce.");
            throw new ValidationException($"Salesforce Contact error: {errorMessage}");
        }

        var jsonNode = JsonNode.Parse(content);
        var contactId = jsonNode?["id"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(contactId)) {
            throw new ValidationException("Salesforce did not return a Contact ID.");
        }

        return contactId;
    }

    private static string ExtractErrorMessage(string content, string defaultMessage) {
        try {
            var node = JsonNode.Parse(content);
            if (node is JsonArray array && array.Count > 0) {
                var first = array[0];
                var message = first?["message"]?.GetValue<string>();
                var errorCode = first?["errorCode"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(message)) {
                    return !string.IsNullOrWhiteSpace(errorCode) ? $"{errorCode}: {message}" : message;
                }
            }
            if (node is JsonObject obj) {
                var errorDesc = obj["error_description"]?.GetValue<string>();
                var error = obj["error"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(errorDesc)) return errorDesc;
                if (!string.IsNullOrWhiteSpace(error)) return error;

                var message = obj["message"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(message)) return message;
            }
        }
        catch {
            // If not JSON, return truncated content or default
            if (!string.IsNullOrWhiteSpace(content) && content.Length < 300) {
                return content;
            }
        }

        return defaultMessage;
    }
}
