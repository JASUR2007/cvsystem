using backend.DTOs.Settings;

namespace backend.Services.Interfaces;

public interface ISettingsService {
    Task<SettingsView> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SettingsView> UpdateLanguageAsync(Guid userId, SettingsUpdate request, CancellationToken cancellationToken = default);
    Task<SettingsView> UpdateThemeAsync(Guid userId, SettingsUpdate request, CancellationToken cancellationToken = default);
}
