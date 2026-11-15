using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LaptopInputControl.Models;

namespace LaptopInputControl.Services
{
    public class InputHookManager : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        // Window messages
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_RBUTTONDBLCLK = 0x0206;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MBUTTONDBLCLK = 0x0209;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int WM_XBUTTONDBLCLK = 0x020D;
        private const int WM_MOUSEHWHEEL = 0x020E;

        // Delegates
        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelProc _keyboardProc;
        private LowLevelProc _mouseProc;

        private IntPtr _keyboardHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;

        // Thread sync
        private readonly object _lock = new object();

        // Control flags
        public bool MasterKeyboardBlocked { get; set; }
        public bool MasterMouseBlocked { get; set; }

        public bool BlockLeftClick { get; set; }
        public bool BlockRightClick { get; set; }
        public bool BlockMiddleClick { get; set; }
        public bool BlockMouseMove { get; set; }
        public bool BlockScrollWheel { get; set; }
        public bool BlockScrollUp { get; set; }
        public bool BlockScrollDown { get; set; }
        public bool BlockHScrollWheel { get; set; }
        public bool BlockSideButtons { get; set; }
        public DeviceTargetScope TargetScope { get; set; }
        public RawInputDeviceTracker DeviceTracker { get; private set; }

        private readonly HashSet<int> _blockedVkCodes = new HashSet<int>();

        // Emergency hotkey detection
        private DateTime _escKeyDownTime = DateTime.MinValue;
        private long _lastMouseMoveLogTick = 0;

        // Events
        public event Action<EventLogItem> InputIntercepted;
        public event Action EmergencyResetTriggered;

        #region Native Structs & Methods

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static readonly IntPtr REMAP_SIGNATURE = (IntPtr)0x414759; // "AGY"
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, int> _keyRemappings = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();

        public void AddRemapping(int sourceVk, int targetVk)
        {
            _keyRemappings[sourceVk] = targetVk;
        }

        public void RemoveRemapping(int sourceVk)
        {
            int removed;
            _keyRemappings.TryRemove(sourceVk, out removed);
        }

        public void ClearRemappings()
        {
            _keyRemappings.Clear();
        }

        public Dictionary<int, int> GetRemappings()
        {
            return new Dictionary<int, int>(_keyRemappings);
        }

        public static void SendSynthesizedKey(int vk, bool isKeyUp)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)vk;
            inputs[0].u.ki.wScan = (ushort)MapVirtualKey((uint)vk, 0);
            inputs[0].u.ki.dwFlags = isKeyUp ? KEYEVENTF_KEYUP : 0;
            if (IsExtendedKey(vk))
            {
                inputs[0].u.ki.dwFlags |= KEYEVENTF_EXTENDEDKEY;
            }
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = REMAP_SIGNATURE;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static bool IsExtendedKey(int vk)
        {
            return vk == 0x21 || vk == 0x22 || vk == 0x23 || vk == 0x24 || // PgUp, PgDn, End, Home
                   vk == 0x25 || vk == 0x26 || vk == 0x27 || vk == 0x28 || // Left, Up, Right, Down
                   vk == 0x2D || vk == 0x2E ||                              // Insert, Delete
                   vk == 0x5B || vk == 0x5C ||                              // LWin, RWin
                   vk == 0xA3 || vk == 0xA5;                                // RCtrl, RAlt
        }

        #endregion

        public InputHookManager()
        {
            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;
            TargetScope = DeviceTargetScope.AllDevices;
            DeviceTracker = new RawInputDeviceTracker();
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_keyboardHookId == IntPtr.Zero)
                {
                    using (Process curProcess = Process.GetCurrentProcess())
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        IntPtr hMod = GetModuleHandle(curModule.ModuleName);
                        _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
                        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
                    }
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_keyboardHookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_keyboardHookId);
                    _keyboardHookId = IntPtr.Zero;
                }
                if (_mouseHookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHookId);
                    _mouseHookId = IntPtr.Zero;
                }
            }
        }

        public void SetKeyBlocked(int vkCode, bool blocked)
        {
            lock (_lock)
            {
                if (blocked)
                    _blockedVkCodes.Add(vkCode);
                else
                    _blockedVkCodes.Remove(vkCode);
            }
        }

        public void SetKeysBlocked(IEnumerable<int> vkCodes, bool blocked)
        {
            lock (_lock)
            {
                foreach (var vk in vkCodes)
                {
                    if (blocked)
                        _blockedVkCodes.Add(vk);
                    else
                        _blockedVkCodes.Remove(vk);
                }
            }
        }

        public bool IsKeyBlocked(int vkCode)
        {
            lock (_lock)
            {
                return _blockedVkCodes.Contains(vkCode);
            }
        }

        public void ClearAllBlockedKeys()
        {
            lock (_lock)
            {
                _blockedVkCodes.Clear();
            }
        }

        public void EmergencyResetAll()
        {
            lock (_lock)
            {
                MasterKeyboardBlocked = false;
                MasterMouseBlocked = false;
                BlockLeftClick = false;
                BlockRightClick = false;
                BlockMiddleClick = false;
                BlockMouseMove = false;
                BlockScrollWheel = false;
                BlockScrollUp = false;
                BlockScrollDown = false;
                BlockHScrollWheel = false;
                BlockSideButtons = false;
                _blockedVkCodes.Clear();
            }

            var handler = EmergencyResetTriggered;
            if (handler != null)
            {
                handler();
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kbd = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                int vk = (int)kbd.vkCode;
                int msg = wParam.ToInt32();
                bool isKeyDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool isKeyUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);

                // RECURSION GUARD: Pass through synthesized remap keys immediately!
                if (kbd.dwExtraInfo == REMAP_SIGNATURE)
                {
                    return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
                }

                // FAILSAFE 1: Emergency combo: Ctrl + Shift + Alt + F12
                if (vk == 0x7B) // VK_F12
                {
                    bool ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                    bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0;
                    bool alt = (GetAsyncKeyState(0x12) & 0x8000) != 0;
                    if (ctrl && shift && alt)
                    {
                        EmergencyResetAll();
                        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
                    }
                }

                // FAILSAFE 2: Hold ESC for 2.5 seconds
                if (vk == 0x1B) // VK_ESCAPE
                {
                    if (isKeyDown)
                    {
                        if (_escKeyDownTime == DateTime.MinValue)
                        {
                            _escKeyDownTime = DateTime.UtcNow;
                        }
                        else if ((DateTime.UtcNow - _escKeyDownTime).TotalSeconds >= 2.5)
                        {
                            EmergencyResetAll();
                            _escKeyDownTime = DateTime.MinValue;
                            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
                        }
                    }
                    else
                    {
                        _escKeyDownTime = DateTime.MinValue;
                    }
                }

                // KEY REMAPPING ENGINE (e.g. Broken 'C' key replaced with '1')
                int targetVk;
                if (_keyRemappings.TryGetValue(vk, out targetVk))
                {
                    bool appliesToDevice = true;
                    if (TargetScope == DeviceTargetScope.BuiltInOnly)
                    {
                        appliesToDevice = (DeviceTracker.CurrentOrigin == InputDeviceOrigin.BuiltInLaptop);
                    }
                    else if (TargetScope == DeviceTargetScope.ExternalOnly)
                    {
                        appliesToDevice = (DeviceTracker.CurrentOrigin == InputDeviceOrigin.ExternalBluetooth ||
                                           DeviceTracker.CurrentOrigin == InputDeviceOrigin.ExternalUsb);
                    }

                    if (appliesToDevice)
                    {
                        // Synthesize the remapped target key!
                        SendSynthesizedKey(targetVk, isKeyUp);

                        if (isKeyDown)
                        {
                            string srcName = GetKeyFriendlyName(vk);
                            string dstName = GetKeyFriendlyName(targetVk);
                            RaiseInputLog("Remap", srcName + " ➔ " + dstName, false, DeviceTracker.CurrentDeviceName);
                        }

                        // Drop original physical key event
                        return (IntPtr)1;
                    }
                }

                bool shouldBlock = false;
                lock (_lock)
                {
                    if (MasterKeyboardBlocked)
                    {
                        shouldBlock = true;
                    }
                    else if (_blockedVkCodes.Contains(vk))
                    {
                        shouldBlock = true;
                    }
                }

                // Check Device Target Scope (Built-in only vs External only vs All)
                if (shouldBlock && TargetScope != DeviceTargetScope.AllDevices)
                {
                    if (TargetScope == DeviceTargetScope.BuiltInOnly)
                    {
                        // Allow external USB and Bluetooth devices to pass through!
                        if (DeviceTracker.CurrentOrigin != InputDeviceOrigin.BuiltInLaptop)
                        {
                            shouldBlock = false;
                        }
                    }
                    else if (TargetScope == DeviceTargetScope.ExternalOnly)
                    {
                        // Allow built-in laptop keyboard to pass through!
                        if (DeviceTracker.CurrentOrigin == InputDeviceOrigin.BuiltInLaptop)
                        {
                            shouldBlock = false;
                        }
                    }
                }

                if (isKeyDown)
                {
                    string keyName = GetKeyFriendlyName(vk);
                    string devOrigin = DeviceTracker.CurrentDeviceName;
                    RaiseInputLog("Keyboard", keyName + " (" + "0x" + vk.ToString("X2") + ")", shouldBlock, devOrigin);
                }

                if (shouldBlock)
                {
                    return (IntPtr)1; // Drop key event
                }
            }

            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                bool shouldBlock = false;
                string actionName = null;

                lock (_lock)
                {
                    if (MasterMouseBlocked)
                    {
                        shouldBlock = true;
                        actionName = "All Mouse Actions";
                    }
                    else
                    {
                        switch (msg)
                        {
                            case WM_MOUSEMOVE:
                                if (BlockMouseMove)
                                {
                                    shouldBlock = true;
                                    actionName = "Cursor Movement";
                                }
                                break;
                            case WM_LBUTTONDOWN:
                            case WM_LBUTTONUP:
                            case WM_LBUTTONDBLCLK:
                                if (BlockLeftClick)
                                {
                                    shouldBlock = true;
                                    actionName = "Left Click";
                                }
                                break;
                            case WM_RBUTTONDOWN:
                            case WM_RBUTTONUP:
                            case WM_RBUTTONDBLCLK:
                                if (BlockRightClick)
                                {
                                    shouldBlock = true;
                                    actionName = "Right Click";
                                }
                                break;
                            case WM_MBUTTONDOWN:
                            case WM_MBUTTONUP:
                            case WM_MBUTTONDBLCLK:
                                if (BlockMiddleClick)
                                {
                                    shouldBlock = true;
                                    actionName = "Middle Click";
                                }
                                break;
                            case WM_MOUSEWHEEL:
                                {
                                    MSLLHOOKSTRUCT msStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                                    short delta = (short)((msStruct.mouseData >> 16) & 0xffff);
                                    if (delta > 0)
                                    {
                                        actionName = "Scroll Up";
                                        if (BlockScrollWheel || BlockScrollUp)
                                        {
                                            shouldBlock = true;
                                        }
                                    }
                                    else if (delta < 0)
                                    {
                                        actionName = "Scroll Down";
                                        if (BlockScrollWheel || BlockScrollDown)
                                        {
                                            shouldBlock = true;
                                        }
                                    }
                                    else
                                    {
                                        actionName = "Vertical Scroll";
                                        if (BlockScrollWheel)
                                        {
                                            shouldBlock = true;
                                        }
                                    }
                                }
                                break;
                            case WM_MOUSEHWHEEL:
                                {
                                    MSLLHOOKSTRUCT msHStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                                    short hdelta = (short)((msHStruct.mouseData >> 16) & 0xffff);
                                    actionName = hdelta > 0 ? "Scroll Right" : "Scroll Left";
                                    if (BlockHScrollWheel)
                                    {
                                        shouldBlock = true;
                                    }
                                }
                                break;
                            case WM_XBUTTONDOWN:
                            case WM_XBUTTONUP:
                            case WM_XBUTTONDBLCLK:
                                if (BlockSideButtons)
                                {
                                    shouldBlock = true;
                                    actionName = "Side Buttons (Back/Forward)";
                                }
                                break;
                        }
                    }
                }

                // Check Device Target Scope (Built-in only vs External only vs All)
                if (shouldBlock && TargetScope != DeviceTargetScope.AllDevices)
                {
                    if (TargetScope == DeviceTargetScope.BuiltInOnly)
                    {
                        // Allow external USB and Bluetooth mice to pass through!
                        if (DeviceTracker.CurrentOrigin != InputDeviceOrigin.BuiltInLaptop)
                        {
                            shouldBlock = false;
                        }
                    }
                    else if (TargetScope == DeviceTargetScope.ExternalOnly)
                    {
                        // Allow built-in laptop trackpad to pass through!
                        if (DeviceTracker.CurrentOrigin == InputDeviceOrigin.BuiltInLaptop)
                        {
                            shouldBlock = false;
                        }
                    }
                }

                string devName = DeviceTracker.CurrentDeviceName;

                if (shouldBlock)
                {
                    // Rate-limit mouse move logging to avoid saturating UI
                    if (msg == WM_MOUSEMOVE)
                    {
                        long now = Environment.TickCount;
                        if (now - _lastMouseMoveLogTick > 400)
                        {
                            _lastMouseMoveLogTick = now;
                            RaiseInputLog("Mouse", actionName ?? "Movement", true, devName);
                        }
                    }
                    else
                    {
                        RaiseInputLog("Mouse", actionName ?? "Action", true, devName);
                    }

                    return (IntPtr)1; // Drop mouse message
                }
                else
                {
                    if (msg != WM_MOUSEMOVE && msg != 0x0200)
                    {
                        // Log allowed button clicks or scroll actions
                        string allowedAction = actionName ?? GetMouseMessageName(msg);
                        if (!string.IsNullOrEmpty(allowedAction))
                        {
                            RaiseInputLog("Mouse", allowedAction, false, devName);
                        }
                    }
                }
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private void RaiseInputLog(string source, string target, bool blocked, string deviceOrigin = "Global")
        {
            var handler = InputIntercepted;
            if (handler != null)
            {
                handler(new EventLogItem(source, target, blocked, deviceOrigin));
            }
        }

        private static string GetMouseMessageName(int msg)
        {
            switch (msg)
            {
                case WM_LBUTTONDOWN: return "Left Click Down";
                case WM_RBUTTONDOWN: return "Right Click Down";
                case WM_MBUTTONDOWN: return "Middle Click Down";
                case WM_MOUSEWHEEL: return "Vertical Scroll";
                case WM_MOUSEHWHEEL: return "Horizontal Scroll";
                case WM_XBUTTONDOWN: return "Side Button Down";
                default: return null;
            }
        }

        private static string GetKeyFriendlyName(int vk)
        {
            if (vk >= 0x41 && vk <= 0x5A)
                return ((char)vk).ToString();
            if (vk >= 0x30 && vk <= 0x39)
                return ((char)vk).ToString();
            if (vk >= 0x70 && vk <= 0x87)
                return "F" + (vk - 0x6F);
            if (vk >= 0x60 && vk <= 0x69)
                return "Num " + (vk - 0x60);

            switch (vk)
            {
                case 0x08: return "Backspace";
                case 0x09: return "Tab";
                case 0x0D: return "Enter";
                case 0x10: return "Shift";
                case 0x11: return "Control";
                case 0x12: return "Alt";
                case 0x13: return "Pause";
                case 0x14: return "Caps Lock";
                case 0x1B: return "Escape";
                case 0x20: return "Space";
                case 0x21: return "Page Up";
                case 0x22: return "Page Down";
                case 0x23: return "End";
                case 0x24: return "Home";
                case 0x25: return "Left Arrow";
                case 0x26: return "Up Arrow";
                case 0x27: return "Right Arrow";
                case 0x28: return "Down Arrow";
                case 0x2C: return "Print Screen";
                case 0x2D: return "Insert";
                case 0x2E: return "Delete";
                case 0x5B: return "Left Windows";
                case 0x5C: return "Right Windows";
                case 0x5D: return "Context Menu";
                case 0x6A: return "Num *";
                case 0x6B: return "Num +";
                case 0x6D: return "Num -";
                case 0x6E: return "Num .";
                case 0x6F: return "Num /";
                case 0x90: return "Num Lock";
                case 0x91: return "Scroll Lock";
                case 0xA0: return "Left Shift";
                case 0xA1: return "Right Shift";
                case 0xA2: return "Left Ctrl";
                case 0xA3: return "Right Ctrl";
                case 0xA4: return "Left Alt";
                case 0xA5: return "Right Alt";
                case 0xBA: return "; (Semicolon)";
                case 0xBB: return "= (Equals)";
                case 0xBC: return ", (Comma)";
                case 0xBD: return "- (Minus)";
                case 0xBE: return ". (Period)";
                case 0xBF: return "/ (Slash)";
                case 0xC0: return "~ (Tilde)";
                case 0xDB: return "[ (L-Bracket)";
                case 0xDC: return @"\ (Backslash)";
                case 0xDD: return "] (R-Bracket)";
                case 0xDE: return "' (Quote)";
                default: return "Key 0x" + vk.ToString("X2");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
