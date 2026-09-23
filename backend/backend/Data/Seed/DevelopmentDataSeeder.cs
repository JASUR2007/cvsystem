using backend.Common.Enums;
using backend.Data;
using backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace backend.Data.Seed;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Positions.AnyAsync()) return;

        db.Attributes.AddRange(
            new AttributeDefinition { Id = Guid.NewGuid(), Name = "English Level", Category = "Language", Type = AttributeType.String },
            new AttributeDefinition { Id = Guid.NewGuid(), Name = "Docker", Category = "Technology", Type = AttributeType.Boolean },
            new AttributeDefinition { Id = Guid.NewGuid(), Name = ".NET Experience", Category = "Experience", Type = AttributeType.Numeric },
            new AttributeDefinition { Id = Guid.NewGuid(), Name = "React Experience", Category = "Experience", Type = AttributeType.Numeric },
            new AttributeDefinition { Id = Guid.NewGuid(), Name = "PostgreSQL", Category = "Technology", Type = AttributeType.Boolean },
            new AttributeDefinition { Id = Guid.NewGuid(), Name = "Remote Work", Category = "Preferences", Type = AttributeType.Boolean }
        );

        db.Positions.AddRange(
            new Position { Id = Guid.NewGuid(), Title = "Backend Developer", Company = "TechCorp", Level = PositionLevel.Middle, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Position { Id = Guid.NewGuid(), Title = "Data Engineer", Company = "DataSoft", Level = PositionLevel.Junior, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Position { Id = Guid.NewGuid(), Title = "DevOps Engineer", Company = "CloudSystems", Level = PositionLevel.Senior, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Position { Id = Guid.NewGuid(), Title = "Frontend Developer", Company = "WebLabs", Level = PositionLevel.Middle, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        db.Tags.AddRange(new[] { ".NET", "React", "Docker", "PostgreSQL", "Python", "AWS", "TypeScript", "Laravel" }.Select(t => new Tag { Id = Guid.NewGuid(), Name = t }));

        await db.SaveChangesAsync();
    }
}
