using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LaptopInputControl.Installer
{
    public partial class SetupWindow : Window
    {
        private string _targetDirectory;

        public SetupWindow()
        {
            InitializeComponent();

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _targetDirectory = Path.Combine(localAppData, "Programs", "LaptopInputControl");
            TxtInstallPath.Text = _targetDirectory;
        }

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnBrowseClicked(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Installation Directory for Laptop Input Shield";
                dialog.SelectedPath = _targetDirectory;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _targetDirectory = dialog.SelectedPath;
                    TxtInstallPath.Text = _targetDirectory;
                }
            }
        }

        private async void OnInstallClicked(object sender, RoutedEventArgs e)
        {
            _targetDirectory = TxtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(_targetDirectory))
            {
                System.Windows.MessageBox.Show("Please choose a valid destination folder.", "Invalid Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool createDesktop = ChkDesktopShortcut.IsChecked == true;
            bool createStartMenu = ChkStartMenuShortcut.IsChecked == true;
            bool startWithWindows = ChkStartWithWindows.IsChecked == true;

            StepConfig.Visibility = Visibility.Collapsed;
            StepInstalling.Visibility = Visibility.Visible;
            BtnInstall.Visibility = Visibility.Collapsed;
            BtnCancel.IsEnabled = false;

            try
            {
                await Task.Run(() => PerformInstallation(_targetDirectory, createDesktop, createStartMenu, startWithWindows));

                StepInstalling.Visibility = Visibility.Collapsed;
                StepFinished.Visibility = Visibility.Visible;
                BtnCancel.Visibility = Visibility.Collapsed;
                BtnFinish.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("An error occurred during installation: " + ex.Message, "Installation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StepInstalling.Visibility = Visibility.Collapsed;
                StepConfig.Visibility = Visibility.Visible;
                BtnInstall.Visibility = Visibility.Visible;
                BtnCancel.IsEnabled = true;
            }
        }

        private void PerformInstallation(string targetDir, bool createDesktop, bool createStartMenu, bool startWithWindows)
        {
            // 1. Ensure directory exists
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 2. Kill existing process if running
            var runningProcs = Process.GetProcessesByName("LaptopInputControl");
            foreach (var p in runningProcs)
            {
                try { p.Kill(); } catch { }
            }

            // 3. Extract Embedded Payloads
            string appExe = Path.Combine(targetDir, "LaptopInputControl.exe");
            string uninstallerExe = Path.Combine(targetDir, "Uninstall.exe");

            ExtractResource("LaptopInputControl.exe", appExe);
            ExtractResource("Uninstall.exe", uninstallerExe);

            // 4. Create Shortcuts
            if (createDesktop)
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string desktopShortcut = Path.Combine(desktopDir, "Laptop Input Shield.lnk");
                CreateShortcut(appExe, desktopShortcut, "Laptop Input Shield & Controller");
            }

            if (createStartMenu)
            {
                string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                if (!Directory.Exists(startMenuDir))
                {
                    Directory.CreateDirectory(startMenuDir);
                }
                string startMenuShortcut = Path.Combine(startMenuDir, "Laptop Input Shield.lnk");
                CreateShortcut(appExe, startMenuShortcut, "Laptop Input Shield & Controller");
            }

            // 5. Windows Startup Registry (Optional)
            if (startWithWindows)
            {
                try
                {
                    using (var runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (runKey != null)
                        {
                            runKey.SetValue("LaptopInputControl", "\"" + appExe + "\"");
                        }
                    }
                }
                catch { }
            }

            // 6. Register with Windows "Add or Remove Programs" (Control Panel & Settings)
            try
            {
                using (var uninstallKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\LaptopInputControl"))
                {
                    if (uninstallKey != null)
                    {
                        uninstallKey.SetValue("DisplayName", "Laptop Input Shield & Controller");
                        uninstallKey.SetValue("DisplayVersion", "1.3.0");
                        uninstallKey.SetValue("Publisher", "Laptop Input Tools");
                        uninstallKey.SetValue("InstallLocation", targetDir);
                        uninstallKey.SetValue("UninstallString", "\"" + uninstallerExe + "\"");
                        uninstallKey.SetValue("DisplayIcon", appExe + ",0");
                        uninstallKey.SetValue("EstimatedSize", 120, RegistryValueKind.DWord);
                        uninstallKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        uninstallKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        private static void ExtractResource(string resourceName, string targetPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            // Search case-insensitively for resource ending with resourceName
            string matched = null;
            foreach (var name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    matched = name;
                    break;
                }
            }

            if (matched == null)
            {
                // Fallback: If running in folder where payload files already exist
                string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceName);
                if (File.Exists(localFile))
                {
                    File.Copy(localFile, targetPath, true);
                    return;
                }
                throw new FileNotFoundException("Cannot locate payload resource: " + resourceName);
            }

            using (Stream stream = assembly.GetManifestResourceStream(matched))
            using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fs);
            }
        }

        private static void CreateShortcut(string targetExePath, string shortcutPath, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetExePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(targetExePath);
                    shortcut.Description = description;
                    shortcut.Save();
                }
            }
            catch { }
        }

        private void OnFinishClicked(object sender, RoutedEventArgs e)
        {
            if (ChkLaunchApp.IsChecked == true)
            {
                string appExe = Path.Combine(_targetDirectory, "LaptopInputControl.exe");
                if (File.Exists(appExe))
                {
                    try { Process.Start(appExe); } catch { }
                }
            }
            Close();
        }
    }
}
