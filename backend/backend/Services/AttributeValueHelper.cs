using backend.Common.Enums;
using backend.DTOs.Profile;
using backend.Entities;

namespace backend.Services;

public static class AttributeValueHelper
{
    public static bool ValidImageKey(string? key, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(key)) return true;
        var normalized = key.Trim();
        if (normalized.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return true;

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            normalized = uri.AbsolutePath;
        }

        normalized = normalized.TrimStart('/');
        return normalized.StartsWith($"users/{userId}/", StringComparison.Ordinal)
            || normalized.StartsWith($"uploads/users/{userId}/", StringComparison.Ordinal);
    }

    public static string? Validate(AttributeDefinition attribute, AttributeValueInput input)
    {
        return attribute.Type switch
        {
            AttributeType.String when input.TextValue is not null && input.TextValue.Trim().Length > 200 => "Value must be at most 200 characters.",
            AttributeType.Text when input.TextValue is not null && input.TextValue.Trim().Length > 2000 => "Value must be at most 2000 characters.",
            AttributeType.Numeric when input.NumberValue is not null && (input.NumberValue < -1000000 || input.NumberValue > 1000000) => "Numeric value out of range.",
            AttributeType.Period when input.PeriodStart is not null && input.PeriodEnd is not null && input.PeriodEnd < input.PeriodStart => "End date must follow start date.",
            AttributeType.Image when input.ImageObjectKey is not null && !input.ImageObjectKey.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && input.ImageObjectKey.Trim().Length > 500 => "Image key is too long.",
            AttributeType.Dropdown when input.SelectedOptionId is not null && !attribute.Options.Any(option => option.Id == input.SelectedOptionId) => "Invalid option.",
            _ => null
        };
    }

    public static void Apply(UserAttributeValue value, AttributeDefinition attribute, AttributeValueInput input)
    {
        value.TextValue = attribute.Type is AttributeType.String or AttributeType.Text ? input.TextValue?.Trim() : null;
        value.NumberValue = attribute.Type == AttributeType.Numeric ? input.NumberValue : null;
        value.DateValue = attribute.Type == AttributeType.Date ? input.DateValue : null;
        value.PeriodStart = attribute.Type == AttributeType.Period ? input.PeriodStart : null;
        value.PeriodEnd = attribute.Type == AttributeType.Period ? input.PeriodEnd : null;
        value.BooleanValue = attribute.Type == AttributeType.Boolean ? input.BooleanValue : null;
        value.SelectedOptionId = attribute.Type == AttributeType.Dropdown ? input.SelectedOptionId : null;
        value.ImageObjectKey = attribute.Type == AttributeType.Image ? input.ImageObjectKey?.Trim() : null;
        value.UpdatedAt = DateTime.UtcNow;
        value.Version++;
    }

    public static bool IsFilled(UserAttributeValue? value, AttributeDefinition attribute, AppUser user)
    {
        if (attribute.IsBuiltIn)
        {
            return attribute.Name switch
            {
                "First Name" => !string.IsNullOrWhiteSpace(user.FirstName),
                "Last Name" => !string.IsNullOrWhiteSpace(user.LastName),
                "Location" => !string.IsNullOrWhiteSpace(user.Location),
                "Personal Photo" => !string.IsNullOrWhiteSpace(user.PhotoObjectKey),
                _ => false
            };
        }

        return attribute.Type switch
        {
            AttributeType.String or AttributeType.Text => !string.IsNullOrWhiteSpace(value?.TextValue),
            AttributeType.Image => !string.IsNullOrWhiteSpace(value?.ImageObjectKey),
            AttributeType.Numeric => value?.NumberValue is not null,
            AttributeType.Date => value?.DateValue is not null,
            AttributeType.Period => value?.PeriodStart is not null && value?.PeriodEnd is not null,
            AttributeType.Boolean => value?.BooleanValue is not null,
            AttributeType.Dropdown => value?.SelectedOptionId is not null,
            _ => false
        };
    }

    public static AttributeValueView View(AttributeDefinition attribute, UserAttributeValue? value, AppUser user)
    {
        var builtInText = attribute.Name switch
        {
            "First Name" => user.FirstName,
            "Last Name" => user.LastName,
            "Location" => user.Location,
            _ => null
        };

        return new AttributeValueView(
            attribute.Id,
            attribute.Name,
            attribute.Category,
            attribute.Type,
            attribute.IsBuiltIn ? builtInText : value?.TextValue,
            value?.NumberValue,
            value?.DateValue,
            value?.PeriodStart,
            value?.PeriodEnd,
            value?.BooleanValue,
            value?.SelectedOptionId,
            attribute.Name == "Personal Photo" ? user.PhotoObjectKey : value?.ImageObjectKey,
            value?.Version ?? user.Version);
    }
}
