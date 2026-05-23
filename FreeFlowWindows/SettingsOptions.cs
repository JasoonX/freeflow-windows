namespace FreeFlowWindows;

internal sealed class SelectOption
{
    public SelectOption(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public string Value { get; }

    public override string ToString() => Label;
}

internal static class SettingsOptions
{
    public static readonly SelectOption[] DictationHotkeys =
    {
        new("Right Alt", NativeMethods.VK_RMENU.ToString()),
        new("Right Ctrl", NativeMethods.VK_RCONTROL.ToString()),
        new("F8", NativeMethods.VK_F8.ToString()),
        new("F9", NativeMethods.VK_F9.ToString()),
        new("F10", NativeMethods.VK_F10.ToString())
    };

    public static readonly SelectOption[] TranscriptionModels =
    {
        new("Whisper Large v3", "whisper-large-v3"),
        new("Whisper Large v3 Turbo", "whisper-large-v3-turbo")
    };

    public static readonly SelectOption[] PolishModels =
    {
        new("Llama 4 Scout 17B", "meta-llama/llama-4-scout-17b-16e-instruct"),
        new("GPT-OSS 20B", "openai/gpt-oss-20b"),
        new("GPT-OSS 120B", "openai/gpt-oss-120b")
    };

    public static readonly SelectOption[] Languages =
    {
        new("Auto-detect", ""),
        new("Arabic", "ar"),
        new("Chinese", "zh"),
        new("Dutch", "nl"),
        new("English", "en"),
        new("French", "fr"),
        new("German", "de"),
        new("Hindi", "hi"),
        new("Italian", "it"),
        new("Japanese", "ja"),
        new("Korean", "ko"),
        new("Polish", "pl"),
        new("Portuguese", "pt"),
        new("Russian", "ru"),
        new("Spanish", "es"),
        new("Ukrainian", "uk"),
        new("Vietnamese", "vi")
    };
}
