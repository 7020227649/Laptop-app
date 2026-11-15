$code = @'
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class DeviceHookTest : Form
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_INPUT = 0x00FF;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIDI_DEVICENAME = 0x20000007;

    private static IntPtr _hookId = IntPtr.Zero;
    private static LowLevelKeyboardProc _proc = HookCallback;
    public static IntPtr LastRawDevice = IntPtr.Zero;
    public static string LastDeviceType = "Unknown";
    public static long LastRawTick = 0;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [DllImport("user32.dll")]
    public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    public static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [StructLayout(LayoutKind.Explicit)]
    public struct RAWINPUT
    {
        [FieldOffset(0)] public RAWINPUTHEADER header;
        [FieldOffset(16)] public RAWKEYBOARD keyboard;
        [FieldOffset(16)] public RAWMOUSE mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWMOUSE
    {
        public ushort usFlags;
        public uint ulButtons;
        public ushort usButtonFlags;
        public ushort usButtonData;
        public uint ulRawButtons;
        public int lLastX;
        public int lLastY;
        public uint ulExtraInformation;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, StringBuilder pData, ref uint pcbSize);

    public DeviceHookTest()
    {
        RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[2];
        rid[0].usUsagePage = 0x01;
        rid[0].usUsage = 0x06; // Keyboard
        rid[0].dwFlags = 0x00000100; // RIDEV_INPUTSINK
        rid[0].hwndTarget = this.Handle;

        rid[1].usUsagePage = 0x01;
        rid[1].usUsage = 0x02; // Mouse
        rid[1].dwFlags = 0x00000100; // RIDEV_INPUTSINK
        rid[1].hwndTarget = this.Handle;

        RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));

        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_INPUT)
        {
            uint dwSize = 0;
            GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(m.LParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));
                    LastRawDevice = raw.header.hDevice;
                    LastRawTick = Stopwatch.GetTimestamp();

                    uint pcbSize = 0;
                    GetRawInputDeviceInfo(raw.header.hDevice, RIDI_DEVICENAME, null, ref pcbSize);
                    StringBuilder sb = new StringBuilder((int)pcbSize);
                    GetRawInputDeviceInfo(raw.header.hDevice, RIDI_DEVICENAME, sb, ref pcbSize);
                    string name = sb.ToString();

                    if (name.IndexOf("ACPI", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("DLLK", StringComparison.OrdinalIgnoreCase) >= 0)
                        LastDeviceType = "INBUILT";
                    else if (name.IndexOf("00001812", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("BTH", StringComparison.OrdinalIgnoreCase) >= 0)
                        LastDeviceType = "BLUETOOTH";
                    else
                        LastDeviceType = "EXTERNAL_USB";

                    Console.WriteLine("RAW_INPUT: " + LastDeviceType + " | Handle=" + raw.header.hDevice + " | Path=" + name);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        base.WndProc(ref m);
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            Console.WriteLine("HOOK_CALLBACK: Last detected device was: " + LastDeviceType + " (" + LastRawDevice + ")");
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    protected override void Dispose(bool disposing)
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        base.Dispose(disposing);
    }
}
'@

Write-Output "Code compiled check"
