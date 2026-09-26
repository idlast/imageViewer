using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ImgViewer.Services;

internal static class MonitorBounds
{
    private const uint MonitorDefaultToNearest = 2;

    /// <summary>
    /// 最大化したまま別モニタへ移すと RestoreBounds が移動前のモニタに残るため、
    /// 実際に表示されているモニタの作業領域内へ移した位置を返す。
    /// </summary>
    public static Rect MoveOntoCurrentMonitor(Window window, Rect bounds)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var target = PresentationSource.FromVisual(window)?.CompositionTarget;
        if (handle == IntPtr.Zero || target is null || bounds.IsEmpty)
        {
            return bounds;
        }

        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, MonitorDefaultToNearest), ref info))
        {
            return bounds;
        }

        var fromDevice = target.TransformFromDevice;
        var work = new Rect(
            fromDevice.Transform(new Point(info.Work.Left, info.Work.Top)),
            fromDevice.Transform(new Point(info.Work.Right, info.Work.Bottom)));

        var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        if (work.Contains(center))
        {
            return bounds;
        }

        var width = Math.Min(bounds.Width, work.Width);
        var height = Math.Min(bounds.Height, work.Height);
        return new Rect(
            work.Left + (work.Width - width) / 2,
            work.Top + (work.Height - height) / 2,
            width,
            height);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
}
