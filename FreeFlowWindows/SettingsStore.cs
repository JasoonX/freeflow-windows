using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace FreeFlowWindows;

internal sealed class SettingsStore
{
    private readonly string settingsPath;

    public SettingsStore(string? settingsPath = null)
    {
        var directory = settingsPath == null
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FreeFlow")
            : Path.GetDirectoryName(settingsPath) ?? ".";
        Directory.CreateDirectory(directory);
        this.settingsPath = settingsPath ?? Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var dto = JsonSerializer.Deserialize<SettingsDto>(
                File.ReadAllText(settingsPath));
            if (dto == null)
            {
                return new AppSettings();
            }

            return new AppSettings
            {
                GroqApiKey = Unprotect(dto.ProtectedGroqApiKey),
                GroqTranscriptionModel = string.IsNullOrWhiteSpace(dto.GroqTranscriptionModel)
                    ? "whisper-large-v3"
                    : dto.GroqTranscriptionModel,
                GroqPolishModel = string.IsNullOrWhiteSpace(dto.GroqPolishModel)
                    ? "meta-llama/llama-4-scout-17b-16e-instruct"
                    : dto.GroqPolishModel,
                GroqPolishText = dto.GroqPolishText,
                Language = dto.Language?.Trim() ?? "",
                OpenAtStartup = dto.OpenAtStartup,
                DictationHotkeyVirtualKey = IsKnownHotkey(dto.DictationHotkeyVirtualKey)
                    ? dto.DictationHotkeyVirtualKey
                    : NativeMethods.VK_RMENU
            };
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var dto = new SettingsDto
        {
            ProtectedGroqApiKey = Protect(settings.GroqApiKey),
            GroqTranscriptionModel = settings.GroqTranscriptionModel,
            GroqPolishModel = settings.GroqPolishModel,
            GroqPolishText = settings.GroqPolishText,
            Language = settings.Language,
            OpenAtStartup = settings.OpenAtStartup,
            DictationHotkeyVirtualKey = settings.DictationHotkeyVirtualKey
        };
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json);
    }

    public void Delete()
    {
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
        }
    }

    private static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(value);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return "";
        }
    }

    private static bool IsKnownHotkey(uint virtualKey)
    {
        return SettingsOptions.DictationHotkeys.Any(
            option => uint.TryParse(option.Value, out var value) && value == virtualKey);
    }

    private sealed class SettingsDto
    {
        public string ProtectedGroqApiKey { get; set; } = "";
        public string GroqTranscriptionModel { get; set; } = "whisper-large-v3";
        public string GroqPolishModel { get; set; } = "meta-llama/llama-4-scout-17b-16e-instruct";
        public bool GroqPolishText { get; set; } = true;
        public string Language { get; set; } = "";
        public bool OpenAtStartup { get; set; }
        public uint DictationHotkeyVirtualKey { get; set; } = NativeMethods.VK_RMENU;
    }
}
