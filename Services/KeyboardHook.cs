using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ShareShot.Interfaces;

namespace ShareShot.Services
{
    public class KeyboardHook : IKeyboardHook
    {
        #region Constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        #endregion

        #region Fields
        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookID;
        private bool _ctrlPressed;
        private bool _f9Pressed;
        #endregion

        public event EventHandler? KeyPressed;

        public KeyboardHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, 
                GetModuleHandle(curModule?.ModuleName ?? ""), 0);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                HandleKeyEvent((int)wParam, vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void HandleKeyEvent(int wParam, int vkCode)
        {
            if (wParam == WM_KEYDOWN)
            {
                HandleKeyDown(vkCode);
            }
            else if (wParam == WM_KEYUP)
            {
                HandleKeyUp(vkCode);
            }
        }

        private void HandleKeyDown(int vkCode)
        {
            if (vkCode == (int)Keys.LControlKey || vkCode == (int)Keys.RControlKey)
                _ctrlPressed = true;
            else if (vkCode == (int)Keys.F9)
                _f9Pressed = true;

            if (_ctrlPressed && _f9Pressed)
            {
                _ctrlPressed = false;
                _f9Pressed = false;
                KeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void HandleKeyUp(int vkCode)
        {
            if (vkCode == (int)Keys.LControlKey || vkCode == (int)Keys.RControlKey)
                _ctrlPressed = false;
            else if (vkCode == (int)Keys.F9)
                _f9Pressed = false;
        }

        #region Native Methods
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
    }
} 