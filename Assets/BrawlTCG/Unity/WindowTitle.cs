using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Shows the version in the window title ("BrawlTCG-Beta-v1.0"). Unity titles the window with the Product Name,
    /// which has to stay fixed because it also names the save folder, so the title is replaced at startup instead.
    /// </summary>
    static class WindowTitle
    {
        public static string Text => $"BrawlTCG-Beta-v{Application.version}";

        public static void Apply()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                _pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                _window = IntPtr.Zero;
                EnumWindows(FindGameWindow, IntPtr.Zero);
                if (_window != IntPtr.Zero) SetWindowText(_window, Text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not set the window title: " + e.Message);
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        static uint _pid;
        static IntPtr _window;

        /// <summary>Finds this process's Unity window. Static with MonoPInvokeCallback so it also works under IL2CPP.</summary>
        [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
        static bool FindGameWindow(IntPtr hWnd, IntPtr lParam)
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != _pid) return true;
            var name = new StringBuilder(64);
            GetClassName(hWnd, name, name.Capacity);
            if (name.ToString() != "UnityWndClass") return true;
            _window = hWnd;
            return false; // found it, stop
        }

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool SetWindowText(IntPtr hWnd, string text);
#endif
    }
}
