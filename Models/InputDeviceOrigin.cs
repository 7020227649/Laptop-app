using System;

namespace LaptopInputControl.Models
{
    public enum InputDeviceOrigin
    {
        BuiltInLaptop,
        ExternalBluetooth,
        ExternalUsb,
        Unknown
    }

    public enum DeviceTargetScope
    {
        AllDevices = 0,
        BuiltInOnly = 1,
        ExternalOnly = 2
    }
}
