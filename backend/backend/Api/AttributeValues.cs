using backend.Data;

namespace backend.Api;

public sealed record AttributeValueInput(
    string? TextValue,
    decimal? NumberValue,
    DateOnly? DateValue,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    bool? BooleanValue,
    Guid? SelectedOptionId,
    string? ImageObjectKey);

public sealed record AttributeValueView(
    Guid AttributeId,
    string Name,
    string Category,
    AttributeType Type,
    string? TextValue,
    decimal? NumberValue,
    DateOnly? DateValue,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    bool? BooleanValue,
    Guid? SelectedOptionId,
    string? ImageObjectKey,
    int Version);

public static class AttributeValues
{
    public static string? Validate(AttributeDefinition attribute, AttributeValueInput input)
    {
        return attribute.Type switch
        {
            AttributeType.String when input.TextValue?.Length > 500 => "Text is too long.",
            AttributeType.Text when input.TextValue?.Length > 10000 => "Text is too long.",
            AttributeType.Image when input.ImageObjectKey?.Length > 500 => "Image key is too long.",
            AttributeType.Period when input.PeriodStart > input.PeriodEnd => "Period end must follow start.",
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
