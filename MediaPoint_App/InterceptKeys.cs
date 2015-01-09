using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Windows.Input;
using MediaPoint.MVVM.Services;
using MediaPoint.VM.ViewInterfaces;

interface IInputTeller
{
    bool IsInInputControl { get; set; }
}

class InterceptKeys
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GuiThreadInfo
    {
        public int cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public Rect rcCaret;
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo lpgui);

    static IntPtr GetFocusedHandle()
    {
        var info = new GuiThreadInfo();
        info.cbSize = Marshal.SizeOf(info);
        if (!GetGUIThreadInfo(0, ref info))
            return IntPtr.Zero;
        return info.hwndFocus;
    }

    ~InterceptKeys()
    {
        UnhookWindowsHookEx(_hookID);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(
    int nCode, IntPtr wParam, IntPtr lParam);

    private static IntPtr HookCallback(
    int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            IntPtr hwnd = GetFocusedHandle();
            IntPtr thisHandle = Process.GetCurrentProcess().MainWindowHandle;

            int vkCode = Marshal.ReadInt32(lParam);
            Key k = System.Windows.Input.KeyInterop.KeyFromVirtualKey(vkCode);
            var s = ServiceLocator.GetService<IKeyboardHandler>();
            if (s != null)
            {
                bool isAlt = System.Windows.Input.Keyboard.IsKeyDown(Key.LeftAlt) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightAlt);
                bool isCtrl = System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);
                bool isShift = System.Windows.Input.Keyboard.IsKeyDown(Key.LeftShift) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightShift);

                if (_inputTeller.IsInInputControl == false || thisHandle != hwnd)
                {
                    if (s.HandleKey(k, isCtrl, isAlt, isShift, thisHandle != hwnd))
                    {
                        return IntPtr.Zero;
                    }
                }
            }

        }
         
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
    LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
     IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private static IInputTeller _inputTeller;

    public static void Start(IInputTeller inputTeller)
    {
        _inputTeller = inputTeller;
        _hookID = SetHook(_proc);
    }
}