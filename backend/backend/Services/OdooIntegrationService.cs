using System.Security.Cryptography;
using System.Text;
using backend.Common.Enums;
using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Integrations;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class OdooIntegrationService(AppDbContext db, ILogger<OdooIntegrationService> logger) : IOdooIntegrationService {
    public async Task<PositionTokenResponse> GenerateTokenAsync(Guid positionId, CancellationToken cancellationToken = default) {
        var exists = await db.Positions.AnyAsync(p => p.Id == positionId, cancellationToken);
        if (!exists) {
            throw new NotFoundException("Position not found.");
        }

        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = "th_pos_" + Convert.ToHexString(randomBytes).ToLowerInvariant();
        var tokenHash = HashToken(rawToken);

        var existingToken = await db.PositionApiTokens.FirstOrDefaultAsync(t => t.PositionId == positionId, cancellationToken);
        if (existingToken is not null) {
            existingToken.TokenHash = tokenHash;
            existingToken.UpdatedAt = DateTime.UtcNow;
            existingToken.LastUsedAt = null;
            existingToken.IsActive = true;
        }
        else {
            existingToken = new PositionApiToken {
                Id = Guid.NewGuid(),
                PositionId = positionId,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.PositionApiTokens.Add(existingToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Generated new Odoo integration token for Position {PositionId}", positionId);

        return new PositionTokenResponse(positionId, rawToken, existingToken.CreatedAt);
    }

    public async Task<PositionTokenStatusResponse> GetTokenStatusAsync(Guid positionId, CancellationToken cancellationToken = default) {
        var exists = await db.Positions.AnyAsync(p => p.Id == positionId, cancellationToken);
        if (!exists) {
            throw new NotFoundException("Position not found.");
        }

        var token = await db.PositionApiTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.PositionId == positionId && t.IsActive, cancellationToken);

        return new PositionTokenStatusResponse(
            HasToken: token is not null,
            CreatedAt: token?.CreatedAt,
            LastUsedAt: token?.LastUsedAt
        );
    }

    public async Task<PositionAggregatedResultView> GetPositionResultsByTokenAsync(string rawToken, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(rawToken)) {
            throw new UnauthorizedAccessException("Missing API token.");
        }

        var tokenHash = HashToken(rawToken.Trim());
        var tokenEntity = await db.PositionApiTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.IsActive, cancellationToken);

        if (tokenEntity is null) {
            throw new UnauthorizedAccessException("Invalid or inactive API token.");
        }

        var position = await db.Positions.AsNoTracking()
            .Include(p => p.Attributes)
                .ThenInclude(pa => pa.Attribute)
                    .ThenInclude(ad => ad.Options)
            .FirstOrDefaultAsync(p => p.Id == tokenEntity.PositionId, cancellationToken);

        if (position is null) {
            throw new NotFoundException("Position associated with this token was not found.");
        }

        // 1. Fetch Published CVs only (drafts are excluded)
        var publishedCvs = await db.Cvs.AsNoTracking()
            .Where(c => c.PositionId == position.Id && c.Status == CvStatus.Published)
            .Select(c => new { c.Id, c.CandidateId })
            .ToListAsync(cancellationToken);

        var cvCount = publishedCvs.Count;
        var candidateIds = publishedCvs.Select(c => c.CandidateId).Distinct().ToList();

        // 2. Fetch master UserAttributeValues for candidates across the position's attributes
        var positionAttributeIds = position.Attributes.Select(pa => pa.AttributeId).ToList();

        var userValues = candidateIds.Count > 0 && positionAttributeIds.Count > 0
            ? await db.UserAttributeValues.AsNoTracking()
                .Where(uav => candidateIds.Contains(uav.UserId) && positionAttributeIds.Contains(uav.AttributeId))
                .Include(uav => uav.SelectedOption)
                .ToListAsync(cancellationToken)
            : [];

        var valuesByAttribute = userValues
            .GroupBy(v => v.AttributeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 3. Aggregate each Position Attribute
        var attributeAggregates = new List<PositionAttributeAggregateView>();
        foreach (var pa in position.Attributes.OrderBy(a => a.SortOrder)) {
            var attr = pa.Attribute;
            var values = valuesByAttribute.GetValueOrDefault(attr.Id) ?? [];
            var (nonEmptyCount, aggregation) = AggregateAttribute(attr, values);

            attributeAggregates.Add(new PositionAttributeAggregateView(
                AttributeId: attr.Id,
                Title: attr.Name,
                Type: attr.Type.ToString(),
                NonEmptyCount: nonEmptyCount,
                Aggregation: aggregation
            ));
        }

        // 4. Update LastUsedAt timestamp
        tokenEntity.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new PositionAggregatedResultView(
            PositionId: position.Id,
            Title: position.Title,
            CvCount: cvCount,
            GeneratedAt: DateTime.UtcNow,
            Attributes: attributeAggregates
        );
    }

    private static (int NonEmptyCount, object Aggregation) AggregateAttribute(AttributeDefinition attr, List<UserAttributeValue> values) {
        switch (attr.Type) {
            case AttributeType.Numeric: {
                var numericVals = values.Where(v => v.NumberValue.HasValue).Select(v => v.NumberValue!.Value).ToList();
                var count = numericVals.Count;
                var agg = count > 0
                    ? (object)new {
                        kind = "numeric",
                        average = Math.Round(numericVals.Average(), 2),
                        min = numericVals.Min(),
                        max = numericVals.Max()
                    }
                    : new {
                        kind = "numeric",
                        average = (decimal?)null,
                        min = (decimal?)null,
                        max = (decimal?)null
                    };
                return (count, agg);
            }

            case AttributeType.Dropdown: {
                var dropdownVals = values.Where(v => v.SelectedOptionId.HasValue)
                    .Select(v => v.SelectedOption?.Value ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                var count = dropdownVals.Count;
                var topValues = dropdownVals
                    .GroupBy(s => s)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(5)
                    .Select(g => new { value = g.Key, count = g.Count() })
                    .ToList();

                return (count, new { kind = "topValues", values = topValues });
            }

            case AttributeType.String: {
                var stringVals = values.Where(v => !string.IsNullOrWhiteSpace(v.TextValue))
                    .Select(v => v.TextValue!.Trim())
                    .ToList();

                var count = stringVals.Count;
                var topValues = stringVals
                    .GroupBy(s => s)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(5)
                    .Select(g => new { value = g.Key, count = g.Count() })
                    .ToList();

                return (count, new { kind = "topValues", values = topValues });
            }

            case AttributeType.Text: {
                var textVals = values.Where(v => !string.IsNullOrWhiteSpace(v.TextValue)).ToList();
                var count = textVals.Count;
                return (count, new { kind = "textSummary", nonEmptyCount = count });
            }

            case AttributeType.Boolean: {
                var boolVals = values.Where(v => v.BooleanValue.HasValue).Select(v => v.BooleanValue!.Value).ToList();
                var count = boolVals.Count;
                var trueCount = boolVals.Count(b => b);
                var falseCount = boolVals.Count(b => !b);
                var truePct = count > 0 ? Math.Round((double)trueCount / count * 100, 1) : 0;
                return (count, new { kind = "boolean", trueCount, falseCount, truePercentage = truePct });
            }

            case AttributeType.Date: {
                var dateVals = values.Where(v => v.DateValue.HasValue).Select(v => v.DateValue!.Value).ToList();
                var count = dateVals.Count;
                var minDate = count > 0 ? dateVals.Min().ToString("yyyy-MM-dd") : null;
                var maxDate = count > 0 ? dateVals.Max().ToString("yyyy-MM-dd") : null;
                return (count, new { kind = "date", min = minDate, max = maxDate });
            }

            case AttributeType.Period: {
                var periodVals = values.Where(v => v.PeriodStart.HasValue || v.PeriodEnd.HasValue).ToList();
                var count = periodVals.Count;
                var starts = periodVals.Where(v => v.PeriodStart.HasValue).Select(v => v.PeriodStart!.Value).ToList();
                var ends = periodVals.Where(v => v.PeriodEnd.HasValue).Select(v => v.PeriodEnd!.Value).ToList();
                var minStart = starts.Count > 0 ? starts.Min().ToString("yyyy-MM-dd") : null;
                var maxEnd = ends.Count > 0 ? ends.Max().ToString("yyyy-MM-dd") : null;
                return (count, new { kind = "period", minStart, maxEnd });
            }

            case AttributeType.Image: {
                var imgVals = values.Where(v => !string.IsNullOrWhiteSpace(v.ImageObjectKey)).ToList();
                var count = imgVals.Count;
                return (count, new { kind = "image", uploadedCount = count });
            }

            default: {
                return (0, new { kind = "unknown" });
            }
        }
    }

    private static string HashToken(string token) {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
