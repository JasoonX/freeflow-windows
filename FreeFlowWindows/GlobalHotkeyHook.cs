using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FreeFlowWindows;

internal sealed class GlobalHotkeyHook : IDisposable
{
    private IntPtr hookId;
    private bool isDown;
    private uint hotkeyVirtualKey;
    private readonly NativeMethods.LowLevelKeyboardProc proc;
    private static readonly IntPtr SuppressKey = new(1);

    public event EventHandler? HotkeyPressed;
    public event EventHandler? HotkeyReleased;

    public GlobalHotkeyHook(uint hotkeyVirtualKey)
    {
        this.hotkeyVirtualKey = hotkeyVirtualKey;
        proc = HookCallback;
    }

    public void UpdateHotkey(uint virtualKey)
    {
        hotkeyVirtualKey = virtualKey;
        isDown = false;
    }

    public void Start()
    {
        if (hookId != IntPtr.Zero)
        {
            return;
        }

        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        var moduleHandle = NativeMethods.GetModuleHandle(currentModule?.ModuleName);
        hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            proc,
            moduleHandle,
            0);

        if (hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to install keyboard hook.");
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var message = wParam.ToInt32();
            var data = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
            if (data.vkCode == hotkeyVirtualKey)
            {
                if ((message == NativeMethods.WM_KEYDOWN || message == NativeMethods.WM_SYSKEYDOWN) && !isDown)
                {
                    isDown = true;
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else if (message == NativeMethods.WM_KEYUP || message == NativeMethods.WM_SYSKEYUP)
                {
                    isDown = false;
                    HotkeyReleased?.Invoke(this, EventArgs.Empty);
                }

                return SuppressKey;
            }
        }

        return NativeMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(hookId);
            hookId = IntPtr.Zero;
        }
    }
}
