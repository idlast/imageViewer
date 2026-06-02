using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ImgViewer.Services;

internal static class WindowActivation
{
    private const int ShowNormal = 1;
    private const int ShowMaximized = 3;
    private const int ShowRestore = 9;

    public static void AllowForegroundActivation()
    {
        _ = AllowSetForegroundWindow(-1);
    }

    public static void BringExistingInstanceToFront(string processName)
    {
        var currentProcessId = Environment.ProcessId;

        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                if (process.Id == currentProcessId)
                {
                    continue;
                }

                var handle = GetMainWindowHandle(process);
                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                BringHandleToFront(handle);
                return;
            }
        }
    }

    public static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
        {
            var showCommand = window.WindowState == WindowState.Maximized ? ShowMaximized : ShowNormal;
            BringHandleToFront(handle, showCommand);
        }

        window.Activate();
        window.Focus();

        if (!window.Topmost)
        {
            window.SetCurrentValue(Window.TopmostProperty, true);
            window.SetCurrentValue(Window.TopmostProperty, false);
        }
    }

    private static IntPtr GetMainWindowHandle(Process process)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            Thread.Sleep(50);
        }

        return IntPtr.Zero;
    }

    private static void BringHandleToFront(IntPtr handle, int showCommand = ShowNormal)
    {
        _ = ShowWindow(handle, IsIconic(handle) ? ShowRestore : showCommand);

        var foregroundWindow = GetForegroundWindow();
        var foregroundThreadId = foregroundWindow == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foregroundWindow, out _);
        var targetThreadId = GetWindowThreadProcessId(handle, out _);
        var currentThreadId = GetCurrentThreadId();

        try
        {
            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
            {
                _ = AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            if (targetThreadId != 0 && targetThreadId != currentThreadId)
            {
                _ = AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            _ = SetForegroundWindow(handle);
            _ = BringWindowToTop(handle);
            _ = SetFocus(handle);
        }
        finally
        {
            if (targetThreadId != 0 && targetThreadId != currentThreadId)
            {
                _ = AttachThreadInput(currentThreadId, targetThreadId, false);
            }

            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
            {
                _ = AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
}
