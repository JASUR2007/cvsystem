namespace backend.DTOs.Settings;

public sealed record SettingsUpdate(string Value, int Version);

public sealed record SettingsView(string Language, string Theme, int Version);
