using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaptopInputControl.Services
{
    public class HardwareDeviceInfo
    {
        public string InstanceId { get; set; }
        public string FriendlyName { get; set; }
        public string DeviceClass { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsBuiltIn { get; set; }
        public string ConnectionType { get; set; }
        public string ConnectionBadge { get; set; }
        public string StatusSummary { get; set; }

        public string StatusColor
        {
            get { return IsEnabled ? "#50FA7B" : "#FF5555"; }
        }

        public string ConnectionBadgeColor
        {
            get
            {
                if (IsBuiltIn) return "#38BDF8";
                if (ConnectionType != null && ConnectionType.Contains("Bluetooth")) return "#A78BFA";
                return "#34D399";
            }
        }

        public string ToggleButtonText
        {
            get { return IsEnabled ? "Disable Hardware" : "Enable Hardware"; }
        }
    }

    public class HardwareDeviceManager
    {
        public async Task<List<HardwareDeviceInfo>> DetectHardwareDevicesAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<HardwareDeviceInfo>();
                try
                {
                    string script = @"Get-PnpDevice -Class 'Mouse', 'Keyboard' | Select-Object InstanceId, FriendlyName, Class, Status | ConvertTo-Csv -NoTypeInformation";
                    string output = RunPowerShell(script, false);

                    string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1)
                    {
                        for (int i = 1; i < lines.Length; i++)
                        {
                            string line = lines[i];
                            var parts = ParseCsvLine(line);
                            if (parts.Count >= 4)
                            {
                                string instanceId = parts[0];
                                string friendlyName = parts[1];
                                string devClass = parts[2];
                                string status = parts[3];

                                bool isBuiltIn = instanceId.StartsWith("ACPI", StringComparison.OrdinalIgnoreCase) ||
                                                 instanceId.IndexOf("DLL05CA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                 instanceId.IndexOf("DLLK", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                 instanceId.IndexOf("PS2", StringComparison.OrdinalIgnoreCase) >= 0;

                                string connType;
                                string connBadge;
                                if (isBuiltIn)
                                {
                                    connType = "Built-in Laptop (Internal Bus)";
                                    connBadge = "💻 BUILT-IN";
                                }
                                else if (instanceId.IndexOf("00001812", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         instanceId.IndexOf("00001124", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                         instanceId.IndexOf("BTH", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    connType = "External Bluetooth (Wireless)";
                                    connBadge = "📶 BLUETOOTH";
                                }
                                else
                                {
                                    connType = "External USB / Wired";
                                    connBadge = "🔌 USB / WIRED";
                                }

                                bool isEnabled = status.Equals("OK", StringComparison.OrdinalIgnoreCase);

                                list.Add(new HardwareDeviceInfo
                                {
                                    InstanceId = instanceId,
                                    FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? instanceId : friendlyName,
                                    DeviceClass = devClass,
                                    IsEnabled = isEnabled,
                                    IsBuiltIn = isBuiltIn,
                                    ConnectionType = connType,
                                    ConnectionBadge = connBadge,
                                    StatusSummary = isEnabled ? "Hardware Active (Normal)" : "Hardware Disabled"
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error detecting devices: " + ex.Message);
                }

                // Ensure Dell Touchpad and Built-in Keyboard are prominently at the top if present
                list.Sort((a, b) =>
                {
                    if (a.IsBuiltIn && !b.IsBuiltIn) return -1;
                    if (!a.IsBuiltIn && b.IsBuiltIn) return 1;
                    return string.Compare(a.FriendlyName, b.FriendlyName, StringComparison.OrdinalIgnoreCase);
                });

                return list;
            });
        }

        public async Task<bool> ToggleDeviceStateAsync(string instanceId, bool enable)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string action = enable ? "Enable-PnpDevice" : "Disable-PnpDevice";
                    string script = string.Format("{0} -InstanceId '{1}' -Confirm:$false", action, instanceId);

                    // PnP modification requires administrative privileges; run elevated
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"",
                        Verb = "runas",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit(15000);
                            return proc.ExitCode == 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error toggling device state: " + ex.Message);
                }
                return false;
            });
        }

        private static string RunPowerShell(string script, bool elevated)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script + "\"",
                RedirectStandardOutput = !elevated,
                UseShellExecute = elevated,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var proc = Process.Start(psi))
            {
                if (proc != null)
                {
                    string output = elevated ? "" : proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(8000);
                    return output;
                }
            }
            return "";
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var regex = new Regex("(?:^|,)(\"(?:[^\"]+|\"\")*\"|[^,]*)");
            foreach (Match m in regex.Matches(line))
            {
                string val = m.Value.TrimStart(',');
                if (val.StartsWith("\"") && val.EndsWith("\"") && val.Length >= 2)
                {
                    val = val.Substring(1, val.Length - 2).Replace("\"\"", "\"");
                }
                result.Add(val);
            }
            return result;
        }
    }
}
