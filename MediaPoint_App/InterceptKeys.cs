using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Windows.Input;
using MediaPoint.MVVM.Services;
using MediaPoint.VM.ViewInterfaces;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;

interface IInputTeller : IService
{
    bool IsInInputControl { get; }
}

class InterceptKeys
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
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

    static volatile bool _isCtrlDown;
    static volatile bool _isAltDown;
    static volatile bool _isShiftDown;

    private static IntPtr HookCallback(
    int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0) // The specs say if nCode is < 0 then we just pass straight on.
        {
            CallNextHookEx(_hookID, nCode, wParam, lParam);
            return IntPtr.Zero;
        }

        if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Key k = System.Windows.Input.KeyInterop.KeyFromVirtualKey(vkCode);
            if (k == Key.LeftCtrl || k == Key.RightCtrl)
            {
                _isCtrlDown = false;
            }
            if (k == Key.LeftAlt || k == Key.RightAlt)
            {
                _isAltDown = false;
            }
            if (k == Key.LeftShift || k == Key.RightShift)
            {
                _isShiftDown = false;
            }
        }
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            IntPtr hwnd = GetFocusedHandle();

            List<IntPtr> myHandles = new List<IntPtr>();
            myHandles.Add(Process.GetCurrentProcess().MainWindowHandle);

            var windows = Application.Current.MainWindow.OwnedWindows;

            foreach (Window w in windows)
            {
                myHandles.Add(new WindowInteropHelper(w).Handle);
            }
            
            bool inApp = myHandles.Contains(hwnd);

            int vkCode = Marshal.ReadInt32(lParam);
            Key k = System.Windows.Input.KeyInterop.KeyFromVirtualKey(vkCode);

            if (k == Key.LeftCtrl || k == Key.RightCtrl)
            {
                _isCtrlDown = true;
            }
            if (k == Key.LeftAlt || k == Key.RightAlt)
            {
                _isAltDown = true;
            }
            if (k == Key.LeftShift || k == Key.RightShift)
            {
                _isShiftDown = true;
            }

            if (k == Key.SelectMedia)
            {
                Application.Current.MainWindow.Dispatcher.BeginInvoke((Action)(()=>
                {
                    Application.Current.MainWindow.Activate();
                }), System.Windows.Threading.DispatcherPriority.SystemIdle);
                return IntPtr.Zero;
            }
            var s = ServiceLocator.GetService<IKeyboardHandler>();
            if (s != null)
            {
                bool isAlt = _isAltDown || System.Windows.Input.Keyboard.IsKeyDown(Key.LeftAlt) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightAlt);
                bool isCtrl = _isCtrlDown || System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);
                bool isShift = _isShiftDown || System.Windows.Input.Keyboard.IsKeyDown(Key.LeftShift) || System.Windows.Input.Keyboard.IsKeyDown(Key.RightShift);

                if (_inputTeller.IsInInputControl == false || !inApp)
                {
                    if (s.HandleKey(k, isCtrl, isAlt, isShift, !inApp))
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

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);


    public static void Start(IInputTeller inputTeller)
    {
        bool isDebuggerPresent = false;
        CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isDebuggerPresent);

        if (!isDebuggerPresent)
        {
            _inputTeller = inputTeller;
            _hookID = SetHook(_proc);
        }
    }
}