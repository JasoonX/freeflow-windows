using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace FreeFlowWindows;

internal partial class SettingsWindow : Window
{
    private readonly bool apiKeyOnly;
    private bool showingReadyState;

    public AppSettings Settings { get; private set; }

    public SettingsWindow(AppSettings settings, bool apiKeyOnly = false)
    {
        InitializeComponent();
        this.apiKeyOnly = apiKeyOnly;
        Settings = settings;
        ApplySystemTheme();

        GroqKeyBox.Password = settings.GroqApiKey;
        DictationHotkeyBox.ItemsSource = SettingsOptions.DictationHotkeys;
        GroqSttModelBox.ItemsSource = SettingsOptions.TranscriptionModels;
        GroqPolishModelBox.ItemsSource = SettingsOptions.PolishModels;
        LanguageBox.ItemsSource = SettingsOptions.Languages;
        DictationHotkeyBox.SelectedValue = SelectKnownValue(
            settings.DictationHotkeyVirtualKey.ToString(),
            SettingsOptions.DictationHotkeys,
            NativeMethods.VK_RMENU.ToString());
        GroqSttModelBox.SelectedValue = SelectKnownValue(
            settings.GroqTranscriptionModel,
            SettingsOptions.TranscriptionModels,
            "whisper-large-v3");
        GroqPolishModelBox.SelectedValue = SelectKnownValue(
            settings.GroqPolishModel,
            SettingsOptions.PolishModels,
            "meta-llama/llama-4-scout-17b-16e-instruct");
        GroqPolishBox.IsChecked = settings.GroqPolishText;
        LanguageBox.SelectedValue = SelectKnownValue(settings.Language, SettingsOptions.Languages, "");
        OpenAtStartupBox.IsChecked = settings.OpenAtStartup;

        if (apiKeyOnly)
        {
            Title = "Set Up FreeFlow for Windows";
            SubtitleText.Text = "Setup";
            FirstStartHelp.Visibility = Visibility.Visible;
            AdvancedSettings.Visibility = Visibility.Collapsed;
            SaveButton.Content = "Continue";
        }

        GroqKeyBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (showingReadyState)
        {
            DialogResult = true;
            Close();
            return;
        }

        var apiKey = GroqKeyBox.Password.Trim();
        if (apiKeyOnly && string.IsNullOrWhiteSpace(apiKey))
        {
            System.Windows.MessageBox.Show(
                this,
                "Enter a Groq API key.",
                "Groq API key required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            GroqKeyBox.Focus();
            return;
        }

        if (apiKeyOnly && !LooksLikeGroqApiKey(apiKey))
        {
            System.Windows.MessageBox.Show(
                this,
                "Groq API keys start with gsk_.",
                "Invalid API key",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            GroqKeyBox.Focus();
            return;
        }

        Settings = new AppSettings
        {
            GroqApiKey = apiKey,
            GroqTranscriptionModel = (GroqSttModelBox.SelectedValue as string) ?? "whisper-large-v3",
            GroqPolishModel = (GroqPolishModelBox.SelectedValue as string)
                ?? "meta-llama/llama-4-scout-17b-16e-instruct",
            GroqPolishText = GroqPolishBox.IsChecked == true,
            Language = (LanguageBox.SelectedValue as string) ?? "",
            OpenAtStartup = OpenAtStartupBox.IsChecked == true,
            DictationHotkeyVirtualKey = SelectedHotkeyVirtualKey()
        };

        if (apiKeyOnly)
        {
            ShowReadyState();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void ShowReadyState()
    {
        showingReadyState = true;
        SetupPanel.Visibility = Visibility.Collapsed;
        ReadyPanel.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Collapsed;
        SaveButton.Content = "Done";
        SubtitleText.Text = "Ready";
        ReadyText.Text = $"Hold {SelectedHotkeyLabel()} to dictate.";
        SaveButton.Focus();
    }

    private uint SelectedHotkeyVirtualKey()
    {
        return uint.TryParse(DictationHotkeyBox.SelectedValue as string, out var virtualKey)
            ? virtualKey
            : NativeMethods.VK_RMENU;
    }

    private string SelectedHotkeyLabel()
    {
        var value = SelectedHotkeyVirtualKey().ToString();
        return SettingsOptions.DictationHotkeys
            .FirstOrDefault(option => option.Value == value)
            ?.Label
            ?? "Right Alt";
    }

    private static bool LooksLikeGroqApiKey(string value)
    {
        return value.StartsWith("gsk_", StringComparison.Ordinal)
            && value.Length >= 20
            && !value.Any(char.IsWhiteSpace);
    }

    private static string SelectKnownValue(
        string value,
        IEnumerable<SelectOption> options,
        string fallback)
    {
        var trimmed = value.Trim();
        return options.Any(option => option.Value == trimmed) ? trimmed : fallback;
    }

    private void ApplySystemTheme()
    {
        if (!UsesDarkAppTheme())
        {
            return;
        }

        Background = Brush("#111318");
        SetBrush("TextBrush", "#F4F7FB");
        SetBrush("MutedBrush", "#A8B0C2");
        SetBrush("BorderBrushSoft", "#303744");
        SetBrush("CardBackgroundBrush", "#1A1D24");
        SetBrush("FieldBackgroundBrush", "#12151B");
        SetBrush("FieldHoverBrush", "#171B23");
        SetBrush("FieldPressedBrush", "#202634");
        SetBrush("FieldHoverBorderBrush", "#4A5568");
        SetBrush("SelectorGlyphBackgroundBrush", "#252B36");
        SetBrush("SelectorItemHoverBrush", "#263044");
        SetBrush("SelectorItemSelectedBrush", "#2B3854");
        SetBrush("SetupHelpBackgroundBrush", "#182236");
        SetBrush("SetupHelpBorderBrush", "#2C426A");
        SetBrush("SecondaryButtonHoverBrush", "#242A35");
        SetBrush("SecondaryButtonPressedBrush", "#2D3542");
    }

    private void SetBrush(string key, string hex)
    {
        Resources[key] = Brush(hex);
    }

    private static SolidColorBrush Brush(string hex)
    {
        return new SolidColorBrush(ColorFromHex(hex));
    }

    private static System.Windows.Media.Color ColorFromHex(string hex)
    {
        return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
    }

    private static bool UsesDarkAppTheme()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var key = Registry.CurrentUser.OpenSubKey(personalizeKey);
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }
}
