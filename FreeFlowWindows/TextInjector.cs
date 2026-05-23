using System.Runtime.InteropServices;

namespace FreeFlowWindows;

internal static class TextInjector
{
    public static async Task<string> PasteTextAsync(string text, IntPtr targetWindow = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "empty";
        }

        FocusTargetWindow(targetWindow);
        await SetClipboardTextWithRetryAsync(text);
        await Task.Delay(120);
        FocusTargetWindow(targetWindow);
        if (!SendCtrlV())
        {
            FocusTargetWindow(targetWindow);
            TypeUnicodeText(text);
            return "typed fallback";
        }

        await Task.Delay(200);
        return "clipboard paste";
    }

    private static void FocusTargetWindow(IntPtr targetWindow)
    {
        if (targetWindow == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.SetForegroundWindow(targetWindow);
        Thread.Sleep(80);
    }

    private static async Task SetClipboardTextWithRetryAsync(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.Clear();
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                return;
            }
            catch (ExternalException) when (attempt < 4)
            {
                await Task.Delay(50 + attempt * 50);
            }
        }
    }

    private static bool SendCtrlV()
    {
        var inputs = new[]
        {
            KeyDown(NativeMethods.VK_CONTROL),
            KeyDown(NativeMethods.VK_V),
            KeyUp(NativeMethods.VK_V),
            KeyUp(NativeMethods.VK_CONTROL)
        };
        return NativeMethods.SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<NativeMethods.Input>()) == inputs.Length;
    }

    private static void TypeUnicodeText(string text)
    {
        foreach (var unit in text)
        {
            var inputs = new[]
            {
                UnicodeKey(unit, keyUp: false),
                UnicodeKey(unit, keyUp: true)
            };
            NativeMethods.SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<NativeMethods.Input>());
        }
    }

    private static NativeMethods.Input KeyDown(ushort key) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KeyboardInput { wVk = key }
        }
    };

    private static NativeMethods.Input KeyUp(ushort key) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KeyboardInput
            {
                wVk = key,
                dwFlags = NativeMethods.KEYEVENTF_KEYUP
            }
        }
    };

    private static NativeMethods.Input UnicodeKey(char character, bool keyUp) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KeyboardInput
            {
                wScan = character,
                dwFlags = NativeMethods.KEYEVENTF_UNICODE
                    | (keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0)
            }
        }
    };
}
