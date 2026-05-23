namespace FreeFlowWindows;

internal sealed class DictationController : IDisposable
{
    private readonly SettingsStore settingsStore;
    private readonly Action<DictationStatus, string> notify;
    private readonly WaveInRecorder recorder = new();
    private readonly GroqClient groq = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private AppSettings settings;
    private CancellationTokenSource? activeOperation;
    private bool recording;
    private string lastTranscript = "";
    private IntPtr injectionTargetWindow;

    public DictationController(
        SettingsStore settingsStore,
        AppSettings settings,
        Action<DictationStatus, string> notify)
    {
        this.settingsStore = settingsStore;
        this.settings = settings;
        this.notify = notify;
        recorder.PcmChunkCaptured += OnPcmChunkCaptured;
    }

    public void UpdateSettings(AppSettings newSettings)
    {
        settings = newSettings;
    }

    public async Task BeginDictationAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (recording)
            {
                return;
            }

            if (!settings.IsConfigured)
            {
                notify(DictationStatus.Error, "Groq API key required");
                return;
            }

            activeOperation?.Cancel();
            activeOperation?.Dispose();
            activeOperation = new CancellationTokenSource();
            injectionTargetWindow = NativeMethods.GetForegroundWindow();

            recorder.Start();
            recording = true;
            notify(DictationStatus.Listening, "Listening");
        }
        catch (Exception ex)
        {
            notify(DictationStatus.Error, $"Could not start recording: {ex.Message}");
            recording = false;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task FinishDictationAsync()
    {
        CancellationToken token;
        byte[] wav;
        AppSettings capturedSettings;

        await gate.WaitAsync();
        try
        {
            if (!recording)
            {
                return;
            }

            recording = false;
            wav = recorder.Stop();
            capturedSettings = settings;
            token = activeOperation?.Token ?? CancellationToken.None;
        }
        catch (Exception ex)
        {
            notify(DictationStatus.Error, $"Could not stop recording: {ex.Message}");
            return;
        }
        finally
        {
            gate.Release();
        }

        try
        {
            if (wav.Length <= 44)
            {
                notify(DictationStatus.Error, "No audio was captured.");
                return;
            }

            notify(DictationStatus.Processing, "Processing");
            var raw = await groq.TranscribeAsync(wav, capturedSettings, token);
            var polished = await groq.PolishAsync(raw, capturedSettings, token);
            if (string.IsNullOrWhiteSpace(polished))
            {
                notify(DictationStatus.Error, "No speech was detected.");
                return;
            }

            lastTranscript = polished;
            _ = await TextInjector.PasteTextAsync(polished, injectionTargetWindow);
            notify(DictationStatus.Success, "Inserted");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            notify(DictationStatus.Error, $"Dictation failed: {ex.Message}");
        }
    }

    private void OnPcmChunkCaptured(object? sender, byte[] chunk)
    {
        // Groq mode sends the full WAV after recording completes.
    }

    public void CopyLastTranscript()
    {
        if (string.IsNullOrWhiteSpace(lastTranscript))
        {
            notify(DictationStatus.Info, "No transcript yet");
            return;
        }

        Clipboard.SetText(lastTranscript, TextDataFormat.UnicodeText);
        notify(DictationStatus.Info, "Copied");
    }

    public void Dispose()
    {
        recorder.PcmChunkCaptured -= OnPcmChunkCaptured;
        activeOperation?.Cancel();
        activeOperation?.Dispose();
        recorder.Dispose();
        gate.Dispose();
        settingsStore.Save(settings);
    }
}
