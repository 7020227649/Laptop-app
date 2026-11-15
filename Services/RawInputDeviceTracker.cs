using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LaptopInputControl.Models;

namespace LaptopInputControl.Services
{
    public class DeviceClassification
    {
        public IntPtr Handle { get; set; }
        public string Path { get; set; }
        public string DisplayName { get; set; }
        public InputDeviceOrigin Origin { get; set; }
    }

    public class RawInputDeviceTracker
    {
        private const int WM_INPUT = 0x00FF;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIDI_DEVICENAME = 0x20000007;

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, StringBuilder pData, ref uint pcbSize);

        private readonly ConcurrentDictionary<IntPtr, DeviceClassification> _deviceCache = new ConcurrentDictionary<IntPtr, DeviceClassification>();

        public InputDeviceOrigin CurrentOrigin { get; private set; }
        public string CurrentDeviceName { get; private set; }
        public long LastRawInputTick { get; private set; }
        public bool IsRegistered { get; private set; }

        public RawInputDeviceTracker()
        {
            CurrentOrigin = InputDeviceOrigin.BuiltInLaptop;
            CurrentDeviceName = "💻 Built-in Laptop";
            LastRawInputTick = Stopwatch.GetTimestamp();
        }

        public bool Register(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;

            try
            {
                RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[2];

                // Keyboard: UsagePage 1, Usage 6
                rid[0].usUsagePage = 0x01;
                rid[0].usUsage = 0x06;
                rid[0].dwFlags = 0x00000100; // RIDEV_INPUTSINK (receive input even when not in foreground)
                rid[0].hwndTarget = hwnd;

                // Mouse: UsagePage 1, Usage 2
                rid[1].usUsagePage = 0x01;
                rid[1].usUsage = 0x02;
                rid[1].dwFlags = 0x00000100; // RIDEV_INPUTSINK
                rid[1].hwndTarget = hwnd;

                bool ok = RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
                IsRegistered = ok;
                return ok;
            }
            catch
            {
                return false;
            }
        }

        public void ProcessRawInputMessage(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero) return;

            uint dwSize = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            if (dwSize == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                {
                    RAWINPUTHEADER header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
                    var dev = GetClassification(header.hDevice);
                    CurrentOrigin = dev.Origin;
                    CurrentDeviceName = dev.DisplayName;
                    LastRawInputTick = Stopwatch.GetTimestamp();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public DeviceClassification GetClassification(IntPtr hDevice)
        {
            if (hDevice == IntPtr.Zero)
            {
                return new DeviceClassification
                {
                    Handle = IntPtr.Zero,
                    DisplayName = "💻 Built-in Laptop",
                    Origin = InputDeviceOrigin.BuiltInLaptop,
                    Path = ""
                };
            }

            DeviceClassification cached;
            if (_deviceCache.TryGetValue(hDevice, out cached))
            {
                return cached;
            }

            string path = "";
            uint pcbSize = 0;
            GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, null, ref pcbSize);
            if (pcbSize > 0)
            {
                StringBuilder sb = new StringBuilder((int)pcbSize);
                if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, sb, ref pcbSize) > 0)
                {
                    path = sb.ToString();
                }
            }

            var item = ClassifyPath(hDevice, path);
            _deviceCache[hDevice] = item;
            return item;
        }

        private static DeviceClassification ClassifyPath(IntPtr handle, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new DeviceClassification
                {
                    Handle = handle,
                    Path = "",
                    Origin = InputDeviceOrigin.BuiltInLaptop,
                    DisplayName = "💻 Built-in Laptop"
                };
            }

            string upper = path.ToUpperInvariant();

            // 1. Built-in Laptop (Dell, PS/2, ACPI, Synaptics, Alps, Elan)
            if (upper.Contains("ACPI#") || upper.Contains("DLLK") || upper.Contains("DLL0") ||
                upper.Contains("I8042PRT") || upper.Contains("PS/2") || upper.Contains("SYNAPTICS") ||
                upper.Contains("ALPS") || upper.Contains("ELAN"))
            {
                return new DeviceClassification
                {
                    Handle = handle,
                    Path = path,
                    Origin = InputDeviceOrigin.BuiltInLaptop,
                    DisplayName = "💻 Built-in Laptop"
                };
            }

            // 2. Bluetooth Wireless Devices (BLE GATT UUID 00001812, Bluetooth HID 00001124, BTHENUM, BTH)
            if (upper.Contains("00001812") || upper.Contains("00001124") || upper.Contains("BTHENUM") || upper.Contains("BTH#") || upper.Contains("BLUETOOTH"))
            {
                return new DeviceClassification
                {
                    Handle = handle,
                    Path = path,
                    Origin = InputDeviceOrigin.ExternalBluetooth,
                    DisplayName = "📶 External Bluetooth"
                };
            }

            // 3. External USB / Wired / 2.4GHz Dongle Devices
            return new DeviceClassification
            {
                Handle = handle,
                Path = path,
                Origin = InputDeviceOrigin.ExternalUsb,
                DisplayName = "🔌 External USB/Wired"
            };
        }
    }
}
