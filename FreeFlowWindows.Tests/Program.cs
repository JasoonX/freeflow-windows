using FreeFlowWindows;
using System.Text;
using System.Text.Json;

var tests = new (string Name, Action Test)[]
{
    ("Groq settings are configured by API key", TestGroqSettingsConfigured),
    ("Groq endpoint constants are correct", TestGroqEndpoints),
    ("SendInput struct matches Windows layout", TestSendInputLayout),
    ("Polish prompt matches dictation cleanup contract", TestPolishPromptContract),
    ("WAV encoder writes a valid PCM header", TestWavEncoder),
    ("Settings store persists and reloads secrets", TestSettingsStoreRoundTrip),
    ("Settings store persists startup and hotkey", TestSettingsStoreStartupAndHotkey),
    ("Settings store preserves auto-detect language", TestSettingsStoreAutoDetectLanguage),
};

var failed = 0;
foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

if (failed > 0)
{
    Environment.Exit(1);
}

static void TestGroqSettingsConfigured()
{
    Assert.False(new AppSettings().IsConfigured);
    Assert.True(new AppSettings { GroqApiKey = "gsk_test" }.IsConfigured);
}

static void TestGroqEndpoints()
{
    Assert.Equal(
        "https://api.groq.com/openai/v1/audio/transcriptions",
        GroqClient.TranscriptionEndpoint);
    Assert.Equal(
        "https://api.groq.com/openai/v1/chat/completions",
        GroqClient.ChatCompletionsEndpoint);
}

static void TestSendInputLayout()
{
    var expectedSize = IntPtr.Size == 8 ? 40 : 28;
    Assert.Equal(expectedSize, System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.Input>());
}

static void TestPolishPromptContract()
{
    Assert.Contains("speech-to-text cleanup assistant", GroqClient.PolishSystemPrompt);
    Assert.Contains("Return only the cleaned text", GroqClient.PolishSystemPrompt);
    Assert.Contains("Preserve the speaker's words", GroqClient.PolishSystemPrompt);
    Assert.Contains("Do not answer questions", GroqClient.PolishSystemPrompt);
    Assert.Contains("Do not fabricate", GroqClient.PolishSystemPrompt);
    Assert.Contains("Fix punctuation", GroqClient.PolishSystemPrompt);
}

static void TestWavEncoder()
{
    var wav = WavEncoder.Encode(new byte[] { 1, 0, 2, 0 }, 16000, 1, 16);
    Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
    Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
    Assert.Equal("fmt ", Encoding.ASCII.GetString(wav, 12, 4));
    Assert.Equal("data", Encoding.ASCII.GetString(wav, 36, 4));
    Assert.Equal(48, wav.Length);
}

static void TestSettingsStoreRoundTrip()
{
    var path = TempSettingsPath();
    var store = new SettingsStore(path);
    store.Save(new AppSettings
    {
        GroqApiKey = "gsk_roundtrip",
        GroqTranscriptionModel = "whisper-large-v3",
        GroqPolishModel = "openai/gpt-oss-20b",
        GroqPolishText = false,
        Language = "en"
    });

    var loaded = store.Load();
    Assert.Equal("gsk_roundtrip", loaded.GroqApiKey);
    Assert.Equal("openai/gpt-oss-20b", loaded.GroqPolishModel);
    Assert.False(loaded.GroqPolishText);
    store.Delete();
}

static void TestSettingsStoreStartupAndHotkey()
{
    var path = TempSettingsPath();
    var store = new SettingsStore(path);
    store.Save(new AppSettings
    {
        GroqApiKey = "gsk_roundtrip",
        OpenAtStartup = true,
        DictationHotkeyVirtualKey = NativeMethods.VK_F9
    });

    var loaded = store.Load();
    Assert.True(loaded.OpenAtStartup);
    Assert.Equal((uint)NativeMethods.VK_F9, loaded.DictationHotkeyVirtualKey);
    store.Delete();
}

static void TestSettingsStoreAutoDetectLanguage()
{
    var path = TempSettingsPath();
    var store = new SettingsStore(path);
    store.Save(new AppSettings
    {
        GroqApiKey = "gsk_roundtrip",
        GroqTranscriptionModel = "whisper-large-v3",
        GroqPolishModel = "openai/gpt-oss-20b",
        GroqPolishText = true,
        Language = ""
    });

    var loaded = store.Load();
    Assert.Equal("", loaded.Language);
    store.Delete();
}

static string TempSettingsPath()
{
    var dir = Path.Combine(Path.GetTempPath(), "FreeFlowWindows.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(dir);
    return Path.Combine(dir, "settings.json");
}

static class Assert
{
    public static void True(bool value)
    {
        if (!value) throw new Exception("Expected true.");
    }

    public static void False(bool value)
    {
        if (value) throw new Exception("Expected false.");
    }

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"Expected '{expected}', got '{actual}'.");
        }
    }

    public static void Contains(string expectedSubstring, string actual)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new Exception($"Expected text to contain '{expectedSubstring}'.");
        }
    }
}
