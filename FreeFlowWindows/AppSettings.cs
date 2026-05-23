namespace FreeFlowWindows;

internal sealed class AppSettings
{
    public string GroqApiKey { get; set; } = "";
    public string GroqTranscriptionModel { get; set; } = "whisper-large-v3";
    public string GroqPolishModel { get; set; } = "meta-llama/llama-4-scout-17b-16e-instruct";
    public bool GroqPolishText { get; set; } = true;
    public string Language { get; set; } = "";
    public bool OpenAtStartup { get; set; }
    public uint DictationHotkeyVirtualKey { get; set; } = NativeMethods.VK_RMENU;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GroqApiKey);
}
