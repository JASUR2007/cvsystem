using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Api;

public static class PositionAccess
{
    public static IQueryable<Cv> EligibleCvs(IQueryable<Cv> cvs, AppDbContext db)
    {
        return cvs.Where(cv => cv.Position.IsPublic || cv.Position.AccessRules.All(rule =>
            db.UserAttributeValues.Any(value => value.UserId == cv.CandidateId && value.AttributeId == rule.AttributeId &&
                ((rule.Attribute.Type == AttributeType.Numeric &&
                    ((rule.Operator == AccessOperator.Equals && value.NumberValue == rule.NumberValue) ||
                     (rule.Operator == AccessOperator.GreaterThan && value.NumberValue > rule.NumberValue) ||
                     (rule.Operator == AccessOperator.GreaterThanOrEqual && value.NumberValue >= rule.NumberValue) ||
                     (rule.Operator == AccessOperator.LessThan && value.NumberValue < rule.NumberValue) ||
                     (rule.Operator == AccessOperator.LessThanOrEqual && value.NumberValue <= rule.NumberValue))) ||
                 (rule.Attribute.Type == AttributeType.Boolean && rule.Operator == AccessOperator.Equals && value.BooleanValue == rule.BooleanValue) ||
                 (rule.Attribute.Type == AttributeType.Dropdown && rule.Operator == AccessOperator.Equals && value.SelectedOptionId == rule.OptionId) ||
                 ((rule.Attribute.Type == AttributeType.String || rule.Attribute.Type == AttributeType.Text) &&
                    ((rule.Operator == AccessOperator.Equals && value.TextValue == rule.ComparisonValue) ||
                     (rule.Operator == AccessOperator.Contains && value.TextValue != null && EF.Functions.ILike(value.TextValue, "%" + rule.ComparisonValue + "%"))))))));
    }

    public static IQueryable<Position> Eligible(IQueryable<Position> positions, AppDbContext db, Guid candidateId)
    {
        return positions.Where(position => position.IsPublic || position.AccessRules.All(rule =>
            db.UserAttributeValues.Any(value => value.UserId == candidateId && value.AttributeId == rule.AttributeId &&
                ((rule.Attribute.Type == AttributeType.Numeric &&
                    ((rule.Operator == AccessOperator.Equals && value.NumberValue == rule.NumberValue) ||
                     (rule.Operator == AccessOperator.GreaterThan && value.NumberValue > rule.NumberValue) ||
                     (rule.Operator == AccessOperator.GreaterThanOrEqual && value.NumberValue >= rule.NumberValue) ||
                     (rule.Operator == AccessOperator.LessThan && value.NumberValue < rule.NumberValue) ||
                     (rule.Operator == AccessOperator.LessThanOrEqual && value.NumberValue <= rule.NumberValue))) ||
                 (rule.Attribute.Type == AttributeType.Boolean && rule.Operator == AccessOperator.Equals && value.BooleanValue == rule.BooleanValue) ||
                 (rule.Attribute.Type == AttributeType.Dropdown && rule.Operator == AccessOperator.Equals && value.SelectedOptionId == rule.OptionId) ||
                 ((rule.Attribute.Type == AttributeType.String || rule.Attribute.Type == AttributeType.Text) &&
                    ((rule.Operator == AccessOperator.Equals && value.TextValue == rule.ComparisonValue) ||
                     (rule.Operator == AccessOperator.Contains && value.TextValue != null && EF.Functions.ILike(value.TextValue, "%" + rule.ComparisonValue + "%"))))))));
    }

    public static string? ValidateRule(AttributeDefinition attribute, AccessOperator operation, string value)
    {
        if (attribute.IsBuiltIn) return "Built-in attributes cannot be used in access rules.";
        if (value.Length > 500) return "Comparison value is too long.";
        return attribute.Type switch
        {
            AttributeType.Numeric when operation is AccessOperator.Equals or AccessOperator.GreaterThan or AccessOperator.GreaterThanOrEqual or AccessOperator.LessThan or AccessOperator.LessThanOrEqual
                && decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _) => null,
            AttributeType.Boolean when operation == AccessOperator.Equals && bool.TryParse(value, out _) => null,
            AttributeType.Dropdown when operation == AccessOperator.Equals && Guid.TryParse(value, out var optionId)
                && attribute.Options.Any(option => option.Id == optionId) => null,
            AttributeType.String or AttributeType.Text when operation is AccessOperator.Equals or AccessOperator.Contains && !string.IsNullOrWhiteSpace(value) => null,
            _ => "Invalid operator or value for this attribute."
        };
    }
}
