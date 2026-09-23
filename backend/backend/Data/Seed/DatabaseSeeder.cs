using backend.Auth;
using backend.Common.Enums;
using backend.Data;
using backend.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { Roles.Candidate, Roles.Recruiter, Roles.Administrator })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not create role {role}.");
                }
            }
        }

        foreach (var name in new[] { "First Name", "Last Name", "Location", "Personal Photo" })
        {
            if (!await db.Attributes.AnyAsync(attribute => attribute.Name == name))
            {
                db.Attributes.Add(new AttributeDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Category = "Personal Information",
                    Type = name == "Personal Photo" ? AttributeType.Image : AttributeType.String,
                    IsBuiltIn = true
                });
            }
        }

        await db.SaveChangesAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Seed default local administrator / developer account
        await EnsureUserAsync(
            userManager,
            email: "makej1318@gmail.com",
            password: "Password123!",
            firstName: "Jasur",
            lastName: "Karimov",
            roles: [Roles.Administrator, Roles.Recruiter, Roles.Candidate]);

        // Seed rich domain data (Attributes including CAP, Acme Corp Position, Candidate CVs, Projects, Discussions)
        await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider);

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var adminEmail = configuration["BootstrapAdmin:Email"];
        var adminPassword = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) && string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Both BootstrapAdmin:Email and BootstrapAdmin:Password are required.");
        }

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, Roles.Administrator))
            {
                throw new InvalidOperationException("Bootstrap admin email belongs to a non-admin account.");
            }

            return;
        }

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "System",
            LastName = "Administrator"
        };

        await using var transaction = await db.Database.BeginTransactionAsync();
        var created = await userManager.CreateAsync(admin, adminPassword);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException("Bootstrap administrator could not be created.");
        }

        var assigned = await userManager.AddToRoleAsync(admin, Roles.Administrator);
        if (!assigned.Succeeded)
        {
            throw new InvalidOperationException("Bootstrap administrator role could not be assigned.");
        }

        await transaction.CommitAsync();
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName,
        string[] roles)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                var errors = string.Join("; ", created.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create user {email}: {errors}");
            }
        }
        else
        {
            if (!await userManager.CheckPasswordAsync(user, password))
            {
                user.PasswordHash = userManager.PasswordHasher.HashPassword(user, password);
                await userManager.UpdateAsync(user);
            }
        }

        foreach (var role in roles)
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
