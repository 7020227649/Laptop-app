using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LaptopInputControl.Installer
{
    static class UninstallerProgram
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var confirm = MessageBox.Show(
                "Are you sure you want to completely uninstall Laptop Input Shield & Controller from your PC?",
                "Uninstall Laptop Input Shield",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                // 1. Terminate running process if any
                var procs = Process.GetProcessesByName("LaptopInputControl");
                foreach (var p in procs)
                {
                    try { p.Kill(); } catch { }
                }

                // 2. Remove Shortcuts
                string desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Laptop Input Shield.lnk");
                if (File.Exists(desktopShortcut))
                {
                    try { File.Delete(desktopShortcut); } catch { }
                }

                string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                string startMenuShortcut = Path.Combine(startMenuDir, "Laptop Input Shield.lnk");
                if (File.Exists(startMenuShortcut))
                {
                    try { File.Delete(startMenuShortcut); } catch { }
                }

                // 3. Remove Windows Registry Uninstall Key
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true))
                    {
                        if (key != null)
                        {
                            key.DeleteSubKeyTree("LaptopInputControl", false);
                        }
                    }
                }
                catch { }

                // 4. Remove Run at Startup Registry Key
                try
                {
                    using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (runKey != null)
                        {
                            runKey.DeleteValue("LaptopInputControl", false);
                        }
                    }
                }
                catch { }

                // 5. Schedule directory deletion via background detached cmd
                string installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
                string cmdArgs = string.Format("/c timeout /t 1 /nobreak >nul & rmdir /s /q \"{0}\"", installDir);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArgs,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);

                MessageBox.Show(
                    "Laptop Input Shield & Controller has been successfully uninstalled from your PC.",
                    "Uninstall Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "An error occurred during uninstallation: " + ex.Message,
                    "Uninstall Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
