using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Settings;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SettingsService(AppDbContext db) : ISettingsService {
    public async Task<SettingsView> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default) {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        return new SettingsView(user.Language, user.Theme, user.Version);
    }

    public Task<SettingsView> UpdateLanguageAsync(Guid userId, SettingsUpdate request, CancellationToken cancellationToken = default) =>
        UpdateAsync(userId, request, true, cancellationToken);

    public Task<SettingsView> UpdateThemeAsync(Guid userId, SettingsUpdate request, CancellationToken cancellationToken = default) =>
        UpdateAsync(userId, request, false, cancellationToken);

    private async Task<SettingsView> UpdateAsync(Guid userId, SettingsUpdate request, bool isLanguage, CancellationToken cancellationToken) {
        if (isLanguage && request.Value is not ("en" or "ru" or "uz"))
            throw new ValidationException("Unsupported language.");

        if (!isLanguage && request.Value is not ("light" or "dark"))
            throw new ValidationException("Unsupported theme.");

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        if (user.Version != request.Version)
            throw new ConflictException("Settings were changed in another session.");

        if (isLanguage)
            user.Language = request.Value;
        else
            user.Theme = request.Value;

        user.Version++;
        await db.SaveChangesAsync(cancellationToken);

        return new SettingsView(user.Language, user.Theme, user.Version);
    }
}
