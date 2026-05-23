using System.IO;

namespace FreeFlowWindows;

internal sealed class FreeFlowApplicationContext : ApplicationContext
{
    private readonly NotifyIcon trayIcon;
    private readonly HudOverlayForm hud = new();
    private readonly SettingsStore settingsStore = new();
    private readonly DictationController controller;
    private readonly GlobalHotkeyHook hotkeyHook;

    public FreeFlowApplicationContext()
    {
        var settings = settingsStore.Load();
        StartupManager.SetEnabled(settings.OpenAtStartup);
        controller = new DictationController(settingsStore, settings, ShowStatus);
        hotkeyHook = new GlobalHotkeyHook(settings.DictationHotkeyVirtualKey);
        hotkeyHook.HotkeyPressed += (_, _) => _ = controller.BeginDictationAsync();
        hotkeyHook.HotkeyReleased += (_, _) => _ = controller.FinishDictationAsync();

        trayIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "FreeFlow for Windows",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        trayIcon.DoubleClick += (_, _) => ShowSettings();

        hotkeyHook.Start();

        if (!settings.IsConfigured)
        {
            ShowSettings(apiKeyOnly: true);
        }
        else
        {
            ShowStatus(DictationStatus.Info, "Ready");
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        menu.Items.Add("Copy last transcript", null, (_, _) => controller.CopyLastTranscript());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void ShowSettings(bool apiKeyOnly = false)
    {
        var window = new SettingsWindow(settingsStore.Load(), apiKeyOnly);
        if (window.ShowDialog() == true)
        {
            settingsStore.Save(window.Settings);
            StartupManager.SetEnabled(window.Settings.OpenAtStartup);
            controller.UpdateSettings(window.Settings);
            hotkeyHook.UpdateHotkey(window.Settings.DictationHotkeyVirtualKey);
            ShowStatus(DictationStatus.Info, "Saved");
        }
    }

    private void ShowStatus(DictationStatus status, string message)
    {
        if (status is DictationStatus.Error or DictationStatus.Info)
        {
            hud.ShowStatus(status, message);
            if (status == DictationStatus.Error && trayIcon.Visible)
            {
                trayIcon.ShowBalloonTip(3000, "FreeFlow for Windows", message, ToolTipIcon.Warning);
            }
            return;
        }

        hud.ShowStatus(status, message);
    }

    private static Icon LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "AppLogo.ico");
        return File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;
    }

    protected override void ExitThreadCore()
    {
        hotkeyHook.Dispose();
        controller.Dispose();
        hud.Dispose();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        base.ExitThreadCore();
    }
}
