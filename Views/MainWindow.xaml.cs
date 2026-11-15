using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using LaptopInputControl.Models;
using LaptopInputControl.Services;

namespace LaptopInputControl.Views
{
    public partial class MainWindow : Window
    {
        private readonly InputHookManager _hookManager;
        private readonly HardwareDeviceManager _hardwareManager;
        private readonly SettingsService _settingsService;

        private readonly List<KeyItem> _allKeys;
        private readonly ObservableCollection<KeyItem> _filteredKeys;
        private readonly ObservableCollection<EventLogItem> _eventLogs;
        private readonly ObservableCollection<KeyRemapItem> _remapRules;

        private DispatcherTimer _autoRevertTimer;
        private int _remainingSeconds = 0;
        private int _configuredAutoRevertSeconds = 0;

        public MainWindow()
        {
            _hookManager = new InputHookManager();
            _hardwareManager = new HardwareDeviceManager();
            _settingsService = new SettingsService();

            _allKeys = KeyboardLayoutFactory.CreateFullKeyboardLayout();
            _filteredKeys = new ObservableCollection<KeyItem>(_allKeys);
            _eventLogs = new ObservableCollection<EventLogItem>();
            _remapRules = new ObservableCollection<KeyRemapItem>();

            InitializeComponent();

            KeyboardCanvasItems.ItemsSource = _allKeys;
            GridKeyItems.ItemsSource = _filteredKeys;
            GridEventLogs.ItemsSource = _eventLogs;
            GridRemapRules.ItemsSource = _remapRules;

            // Populate remap combo boxes
            var sortedKeys = _allKeys.OrderBy(k => k.Label).ToList();
            CmbRemapSource.ItemsSource = sortedKeys;
            CmbRemapSource.DisplayMemberPath = "Label";
            CmbRemapSource.SelectedValuePath = "VkCode";

            CmbRemapTarget.ItemsSource = sortedKeys;
            CmbRemapTarget.DisplayMemberPath = "Label";
            CmbRemapTarget.SelectedValuePath = "VkCode";

            // Default selections: Source '1' (0x31) -> Target 'C' (0x43)
            var key1 = sortedKeys.FirstOrDefault(k => k.VkCode == 0x31);
            if (key1 != null) CmbRemapSource.SelectedItem = key1;
            var keyC = sortedKeys.FirstOrDefault(k => k.VkCode == 0x43);
            if (keyC != null) CmbRemapTarget.SelectedItem = keyC;

            _hookManager.InputIntercepted += OnInputIntercepted;
            _hookManager.EmergencyResetTriggered += OnEmergencyResetTriggered;

            StateChanged += MainWindow_StateChanged;
            MainTabs.SelectionChanged += OnMainTabSelectionChanged;

            // Wire up visual diagram clicks
            DiagTouchSurface.Cursor = Cursors.Hand;
            DiagLeftButton.Cursor = Cursors.Hand;
            DiagRightButton.Cursor = Cursors.Hand;

            DiagTouchSurface.MouseDown += (s, e) =>
            {
                ChkBlockMouseMove.IsChecked = ChkBlockMouseMove.IsChecked != true;
                OnMouseActionToggleChanged(ChkBlockMouseMove, null);
            };
            DiagLeftButton.MouseDown += (s, e) =>
            {
                ChkBlockLeftClick.IsChecked = ChkBlockLeftClick.IsChecked != true;
                OnMouseActionToggleChanged(ChkBlockLeftClick, null);
            };
            DiagRightButton.MouseDown += (s, e) =>
            {
                ChkBlockRightClick.IsChecked = ChkBlockRightClick.IsChecked != true;
                OnMouseActionToggleChanged(ChkBlockRightClick, null);
            };

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        #region Window Control Buttons (Minimize, Maximize, Close)

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnThemeSelectorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbThemeSelector == null) return;
            AppThemeMode mode = AppThemeMode.ObsidianDark;
            switch (CmbThemeSelector.SelectedIndex)
            {
                case 1: mode = AppThemeMode.OledBlack; break;
                case 2: mode = AppThemeMode.MidnightBlue; break;
                case 3: mode = AppThemeMode.CleanLight; break;
                default: mode = AppThemeMode.ObsidianDark; break;
            }
            ThemeManager.ApplyTheme(mode);
        }

        private void OnMaximizeClicked(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MainOuterBorder.Margin = new Thickness(7);
                if (TxtMaximizeGlyph != null) TxtMaximizeGlyph.Text = "\uE923";
                BtnMaximize.ToolTip = "Restore Down";
            }
            else
            {
                MainOuterBorder.Margin = new Thickness(0);
                if (TxtMaximizeGlyph != null) TxtMaximizeGlyph.Text = "\uE922";
                BtnMaximize.ToolTip = "Maximize";
            }
        }

        #endregion

        #region Tab Navigation Handling

        private void OnMainTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != MainTabs) return;

            if (TabContentDashboard == null || TabContentKeyboard == null || 
                TabContentRemap == null || TabContentMouse == null || 
                TabContentHardware == null || TabContentTestZone == null) return;

            TabContentDashboard.Visibility = Visibility.Collapsed;
            TabContentKeyboard.Visibility = Visibility.Collapsed;
            TabContentRemap.Visibility = Visibility.Collapsed;
            TabContentMouse.Visibility = Visibility.Collapsed;
            TabContentHardware.Visibility = Visibility.Collapsed;
            TabContentTestZone.Visibility = Visibility.Collapsed;

            switch (MainTabs.SelectedIndex)
            {
                case 0:
                    TabContentDashboard.Visibility = Visibility.Visible;
                    break;
                case 1:
                    TabContentKeyboard.Visibility = Visibility.Visible;
                    break;
                case 2:
                    TabContentRemap.Visibility = Visibility.Visible;
                    break;
                case 3:
                    TabContentMouse.Visibility = Visibility.Visible;
                    break;
                case 4:
                    TabContentHardware.Visibility = Visibility.Visible;
                    break;
                case 5:
                    TabContentTestZone.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                _hookManager.DeviceTracker.Register(hwnd);
                HwndSource source = HwndSource.FromHwnd(hwnd);
                if (source != null)
                {
                    source.AddHook(WndProcHook);
                }

                _hookManager.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to install input hooks: " + ex.Message, "Hook Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Load saved settings
            LoadSettings();

            // Refresh hardware devices
            await RefreshHardwareDevices();

            UpdateUiState();
        }

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x00FF) // WM_INPUT
            {
                _hookManager.DeviceTracker.ProcessRawInputMessage(lParam);
            }
            return IntPtr.Zero;
        }

        private void OnTargetDeviceScopeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTargetDeviceScope == null || _hookManager == null || _settingsService == null) return;
            DeviceTargetScope scope = (DeviceTargetScope)CmbTargetDeviceScope.SelectedIndex;
            _hookManager.TargetScope = scope;
            if (CmbTargetMouseScope != null && CmbTargetMouseScope.SelectedIndex != CmbTargetDeviceScope.SelectedIndex)
            {
                CmbTargetMouseScope.SelectedIndex = CmbTargetDeviceScope.SelectedIndex;
            }
            UpdateScopeDescription();
            SaveSettings();
        }

        private void OnTargetMouseScopeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTargetMouseScope == null || _hookManager == null || _settingsService == null) return;
            DeviceTargetScope scope = (DeviceTargetScope)CmbTargetMouseScope.SelectedIndex;
            _hookManager.TargetScope = scope;
            if (CmbTargetDeviceScope != null && CmbTargetDeviceScope.SelectedIndex != CmbTargetMouseScope.SelectedIndex)
            {
                CmbTargetDeviceScope.SelectedIndex = CmbTargetMouseScope.SelectedIndex;
            }
            UpdateScopeDescription();
            SaveSettings();
        }

        private void UpdateScopeDescription()
        {
            string desc;
            switch (_hookManager.TargetScope)
            {
                case DeviceTargetScope.BuiltInOnly:
                    desc = "Locks apply ONLY to built-in laptop keyboard & trackpad. External USB & Bluetooth devices work 100% unrestricted!";
                    break;
                case DeviceTargetScope.ExternalOnly:
                    desc = "Locks apply ONLY to external USB & Bluetooth devices. Laptop built-in keyboard & trackpad remain unrestricted!";
                    break;
                default:
                    desc = "Locks apply globally across built-in laptop and all connected external devices.";
                    break;
            }
            if (TxtScopeDescription != null) TxtScopeDescription.Text = desc;
            if (TxtMouseScopeDescription != null) TxtMouseScopeDescription.Text = desc;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            _hookManager.Dispose();
        }

        #region Settings Management

        private void LoadSettings()
        {
            var s = _settingsService.Load();

            _hookManager.MasterKeyboardBlocked = s.MasterKeyboardBlocked;
            _hookManager.MasterMouseBlocked = s.MasterMouseBlocked;

            _hookManager.BlockLeftClick = s.BlockLeftClick;
            _hookManager.BlockRightClick = s.BlockRightClick;
            _hookManager.BlockMiddleClick = s.BlockMiddleClick;
            _hookManager.BlockMouseMove = s.BlockMouseMove;
            _hookManager.BlockScrollWheel = s.BlockScrollWheel;
            _hookManager.BlockScrollUp = s.BlockScrollUp;
            _hookManager.BlockScrollDown = s.BlockScrollDown;
            _hookManager.BlockHScrollWheel = s.BlockHScrollWheel;
            _hookManager.BlockSideButtons = s.BlockSideButtons;

            if (CmbTargetDeviceScope != null && s.TargetScope >= 0 && s.TargetScope <= 2)
            {
                CmbTargetDeviceScope.SelectedIndex = s.TargetScope;
            }
            if (CmbTargetMouseScope != null && s.TargetScope >= 0 && s.TargetScope <= 2)
            {
                CmbTargetMouseScope.SelectedIndex = s.TargetScope;
            }
            _hookManager.TargetScope = (DeviceTargetScope)s.TargetScope;
            UpdateScopeDescription();

            ChkMasterKeyboard.IsChecked = s.MasterKeyboardBlocked;
            ChkMasterMouse.IsChecked = s.MasterMouseBlocked;

            ChkBlockLeftClick.IsChecked = s.BlockLeftClick;
            ChkBlockRightClick.IsChecked = s.BlockRightClick;
            ChkBlockMiddleClick.IsChecked = s.BlockMiddleClick;
            ChkBlockMouseMove.IsChecked = s.BlockMouseMove;
            ChkBlockScrollUp.IsChecked = s.BlockScrollUp;
            ChkBlockScrollDown.IsChecked = s.BlockScrollDown;
            ChkBlockScrollWheel.IsChecked = s.BlockScrollWheel;
            ChkBlockSideButtons.IsChecked = s.BlockSideButtons;

            if (s.BlockedVkCodes != null && s.BlockedVkCodes.Count > 0)
            {
                var blockedSet = new HashSet<int>(s.BlockedVkCodes);
                foreach (var k in _allKeys)
                {
                    if (blockedSet.Contains(k.VkCode))
                    {
                        k.IsBlocked = true;
                        _hookManager.SetKeyBlocked(k.VkCode, true);
                        if (k.RelatedVkCodes != null)
                        {
                            foreach (int rel in k.RelatedVkCodes)
                                _hookManager.SetKeyBlocked(rel, true);
                        }
                    }
                }
            }

            _configuredAutoRevertSeconds = s.AutoRevertSeconds;
            switch (s.AutoRevertSeconds)
            {
                case 30: CmbAutoRevert.SelectedIndex = 1; break;
                case 60: CmbAutoRevert.SelectedIndex = 2; break;
                case 180: CmbAutoRevert.SelectedIndex = 3; break;
                case 300: CmbAutoRevert.SelectedIndex = 4; break;
                default: CmbAutoRevert.SelectedIndex = 0; break;
            }

            if (!string.IsNullOrEmpty(s.Theme))
            {
                if (s.Theme.Equals("OledBlack", StringComparison.OrdinalIgnoreCase))
                {
                    CmbThemeSelector.SelectedIndex = 1;
                    ThemeManager.ApplyTheme(AppThemeMode.OledBlack);
                }
                else if (s.Theme.Equals("MidnightBlue", StringComparison.OrdinalIgnoreCase))
                {
                    CmbThemeSelector.SelectedIndex = 2;
                    ThemeManager.ApplyTheme(AppThemeMode.MidnightBlue);
                }
                else if (s.Theme.Equals("CleanLight", StringComparison.OrdinalIgnoreCase))
                {
                    CmbThemeSelector.SelectedIndex = 3;
                    ThemeManager.ApplyTheme(AppThemeMode.CleanLight);
                }
                else
                {
                    CmbThemeSelector.SelectedIndex = 0;
                    ThemeManager.ApplyTheme(AppThemeMode.ObsidianDark);
                }
            }

            // Load Key Remappings
            _remapRules.Clear();
            _hookManager.ClearRemappings();
            if (s.Remappings != null && s.Remappings.Count > 0)
            {
                foreach (var kvp in s.Remappings)
                {
                    int srcVk = kvp.Key;
                    int dstVk = kvp.Value;
                    _hookManager.AddRemapping(srcVk, dstVk);
                    var srcKey = _allKeys.FirstOrDefault(k => k.VkCode == srcVk);
                    var dstKey = _allKeys.FirstOrDefault(k => k.VkCode == dstVk);
                    string srcName = srcKey != null ? srcKey.Label : string.Format("Key 0x{0:X2}", srcVk);
                    string dstName = dstKey != null ? dstKey.Label : string.Format("Key 0x{0:X2}", dstVk);
                    _remapRules.Add(new KeyRemapItem
                    {
                        SourceVk = srcVk,
                        SourceName = srcName,
                        TargetVk = dstVk,
                        TargetName = dstName,
                        Description = "Saved remapping rule"
                    });
                }
            }
            if (TxtRemapCount != null)
            {
                TxtRemapCount.Text = string.Format(" ({0} rules active)", _remapRules.Count);
            }
        }

        private void SaveSettings()
        {
            if (_allKeys == null || _settingsService == null || _hookManager == null) return;

            var s = new AppSettings
            {
                MasterKeyboardBlocked = _hookManager.MasterKeyboardBlocked,
                MasterMouseBlocked = _hookManager.MasterMouseBlocked,
                BlockLeftClick = _hookManager.BlockLeftClick,
                BlockRightClick = _hookManager.BlockRightClick,
                BlockMiddleClick = _hookManager.BlockMiddleClick,
                BlockMouseMove = _hookManager.BlockMouseMove,
                BlockScrollWheel = _hookManager.BlockScrollWheel,
                BlockScrollUp = _hookManager.BlockScrollUp,
                BlockScrollDown = _hookManager.BlockScrollDown,
                BlockHScrollWheel = _hookManager.BlockHScrollWheel,
                BlockSideButtons = _hookManager.BlockSideButtons,
                TargetScope = (int)_hookManager.TargetScope,
                AutoRevertSeconds = _configuredAutoRevertSeconds,
                Theme = ThemeManager.CurrentTheme.ToString(),
                BlockedVkCodes = _allKeys.Where(k => k.IsBlocked).Select(k => k.VkCode).ToList(),
                Remappings = _hookManager.GetRemappings()
            };
            _settingsService.Save(s);
        }

        #endregion

        #region Hardware PnP

        private async System.Threading.Tasks.Task RefreshHardwareDevices()
        {
            var list = await _hardwareManager.DetectHardwareDevicesAsync();
            GridDevices.ItemsSource = list;

            var dellTouchpad = list.FirstOrDefault(d => d.InstanceId.IndexOf("DLL05CA", StringComparison.OrdinalIgnoreCase) >= 0);
            if (dellTouchpad != null)
            {
                TxtPillHardware.Text = "💻 DELL TOUCHPAD: " + (dellTouchpad.IsEnabled ? "ACTIVE" : "HW DISABLED");
                TxtPillHardware.Foreground = dellTouchpad.IsEnabled ? new SolidColorBrush(Color.FromRgb(0x06, 0xB6, 0xD4)) : new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            }
        }

        private async void OnRefreshDevicesClicked(object sender, RoutedEventArgs e)
        {
            await RefreshHardwareDevices();
        }

        private async void OnToggleHardwareDeviceClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var dev = btn.Tag as HardwareDeviceInfo;
            if (dev == null) return;

            string action = dev.IsEnabled ? "Disable" : "Enable";
            var result = MessageBox.Show(
                string.Format("Are you sure you want to {0} the hardware device '{1}' at the Windows driver level?\n\nThis will request Administrator approval.", action, dev.FriendlyName),
                action + " Hardware Device",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                btn.IsEnabled = false;
                btn.Content = "Applying...";
                bool success = await _hardwareManager.ToggleDeviceStateAsync(dev.InstanceId, !dev.IsEnabled);
                btn.IsEnabled = true;
                await RefreshHardwareDevices();

                if (success)
                {
                    MessageBox.Show("Hardware state updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        #endregion

        #region Master Switches & Presets

        private void OnMasterKeyboardToggleChanged(object sender, RoutedEventArgs e)
        {
            bool isBlocked = ChkMasterKeyboard.IsChecked == true;
            _hookManager.MasterKeyboardBlocked = isBlocked;
            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void OnMasterMouseToggleChanged(object sender, RoutedEventArgs e)
        {
            bool isBlocked = ChkMasterMouse.IsChecked == true;
            _hookManager.MasterMouseBlocked = isBlocked;
            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void OnEmergencyRestoreClicked(object sender, RoutedEventArgs e)
        {
            ExecuteEmergencyRestore();
        }

        private void OnEmergencyResetTriggered()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ExecuteEmergencyRestore();
            }));
        }

        private void ExecuteEmergencyRestore()
        {
            _hookManager.EmergencyResetAll();

            foreach (var k in _allKeys)
                k.IsBlocked = false;

            ChkMasterKeyboard.IsChecked = false;
            ChkMasterMouse.IsChecked = false;
            ChkBlockLeftClick.IsChecked = false;
            ChkBlockRightClick.IsChecked = false;
            ChkBlockMiddleClick.IsChecked = false;
            ChkBlockMouseMove.IsChecked = false;
            ChkBlockScrollUp.IsChecked = false;
            ChkBlockScrollDown.IsChecked = false;
            ChkBlockScrollWheel.IsChecked = false;
            ChkBlockSideButtons.IsChecked = false;

            StopAutoRevertTimer();
            UpdateUiState();

            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch { }
        }

        private void OnPresetLockKeyboard(object sender, RoutedEventArgs e)
        {
            ChkMasterKeyboard.IsChecked = true;
            _hookManager.MasterKeyboardBlocked = true;
            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void OnPresetUnlockKeyboard(object sender, RoutedEventArgs e)
        {
            ChkMasterKeyboard.IsChecked = false;
            _hookManager.MasterKeyboardBlocked = false;
            foreach (var k in _allKeys)
            {
                k.IsBlocked = false;
                _hookManager.SetKeyBlocked(k.VkCode, false);
            }
            UpdateUiState();
        }

        private void OnPresetLockTouchpad(object sender, RoutedEventArgs e)
        {
            ChkMasterMouse.IsChecked = true;
            _hookManager.MasterMouseBlocked = true;
            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void OnPresetUnlockTouchpad(object sender, RoutedEventArgs e)
        {
            ChkMasterMouse.IsChecked = false;
            _hookManager.MasterMouseBlocked = false;
            ChkBlockLeftClick.IsChecked = false;
            ChkBlockRightClick.IsChecked = false;
            ChkBlockMiddleClick.IsChecked = false;
            ChkBlockMouseMove.IsChecked = false;
            ChkBlockScrollUp.IsChecked = false;
            ChkBlockScrollDown.IsChecked = false;
            ChkBlockScrollWheel.IsChecked = false;
            ChkBlockSideButtons.IsChecked = false;

            _hookManager.BlockLeftClick = false;
            _hookManager.BlockRightClick = false;
            _hookManager.BlockMiddleClick = false;
            _hookManager.BlockMouseMove = false;
            _hookManager.BlockScrollWheel = false;
            _hookManager.BlockScrollUp = false;
            _hookManager.BlockScrollDown = false;
            _hookManager.BlockHScrollWheel = false;
            _hookManager.BlockSideButtons = false;

            UpdateUiState();
        }

        private void OnPresetGameMode(object sender, RoutedEventArgs e)
        {
            // Block Windows Keys and Caps Lock
            foreach (var k in _allKeys.Where(x => x.Label.Contains("Win") || x.Label.Contains("Caps")))
            {
                k.IsBlocked = true;
                _hookManager.SetKeyBlocked(k.VkCode, true);
                if (k.RelatedVkCodes != null)
                {
                    foreach (int r in k.RelatedVkCodes)
                        _hookManager.SetKeyBlocked(r, true);
                }
            }
            UpdateUiState();
        }

        private void OnPresetCleanMode(object sender, RoutedEventArgs e)
        {
            // Lock all for 30s
            ChkMasterKeyboard.IsChecked = true;
            ChkMasterMouse.IsChecked = true;
            _hookManager.MasterKeyboardBlocked = true;
            _hookManager.MasterMouseBlocked = true;
            UpdateUiState();

            StartCountdown(30);
        }

        private void OnPresetTypingMode(object sender, RoutedEventArgs e)
        {
            // Palm rest mode: only disable cursor movement
            ChkBlockMouseMove.IsChecked = true;
            _hookManager.BlockMouseMove = true;
            UpdateUiState();
        }

        #endregion

        #region Precise Key Toggles

        private void OnKeyButtonClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var key = btn.Tag as KeyItem;
            if (key == null) return;

            ToggleKey(key);
        }

        private void OnGridKeyToggleClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var key = btn.Tag as KeyItem;
            if (key == null) return;

            ToggleKey(key);
        }

        private void ToggleKey(KeyItem key)
        {
            key.IsBlocked = !key.IsBlocked;
            _hookManager.SetKeyBlocked(key.VkCode, key.IsBlocked);

            if (key.RelatedVkCodes != null)
            {
                foreach (int rel in key.RelatedVkCodes)
                    _hookManager.SetKeyBlocked(rel, key.IsBlocked);
            }

            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void OnBlockAllKeysClicked(object sender, RoutedEventArgs e)
        {
            foreach (var k in _allKeys)
            {
                k.IsBlocked = true;
                _hookManager.SetKeyBlocked(k.VkCode, true);
                if (k.RelatedVkCodes != null)
                {
                    foreach (int r in k.RelatedVkCodes)
                        _hookManager.SetKeyBlocked(r, true);
                }
            }
            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void OnEnableAllKeysClicked(object sender, RoutedEventArgs e)
        {
            foreach (var k in _allKeys)
            {
                k.IsBlocked = false;
                _hookManager.SetKeyBlocked(k.VkCode, false);
                if (k.RelatedVkCodes != null)
                {
                    foreach (int r in k.RelatedVkCodes)
                        _hookManager.SetKeyBlocked(r, false);
                }
            }
            _hookManager.ClearAllBlockedKeys();
            UpdateUiState();
        }

        private void OnToggleCategoryFunction(object sender, RoutedEventArgs e)
        {
            ToggleCategoryKeys("Function");
        }

        private void OnToggleCategoryWin(object sender, RoutedEventArgs e)
        {
            ToggleMatchingKeys(k => k.Label.Contains("Win"));
        }

        private void OnToggleCategoryCaps(object sender, RoutedEventArgs e)
        {
            ToggleMatchingKeys(k => k.Label.Contains("Caps"));
        }

        private void OnToggleCategoryArrows(object sender, RoutedEventArgs e)
        {
            ToggleCategoryKeys("Navigation");
        }

        private void OnToggleCategoryNumpad(object sender, RoutedEventArgs e)
        {
            ToggleCategoryKeys("Numpad");
        }

        private void OnToggleCategoryAlpha(object sender, RoutedEventArgs e)
        {
            ToggleCategoryKeys("Alpha");
        }

        private void ToggleCategoryKeys(string category)
        {
            var keys = _allKeys.Where(k => k.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            bool anyUnblocked = keys.Any(k => !k.IsBlocked);
            bool targetState = anyUnblocked;

            foreach (var k in keys)
            {
                k.IsBlocked = targetState;
                _hookManager.SetKeyBlocked(k.VkCode, targetState);
                if (k.RelatedVkCodes != null)
                {
                    foreach (int r in k.RelatedVkCodes)
                        _hookManager.SetKeyBlocked(r, targetState);
                }
            }
            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void ToggleMatchingKeys(Func<KeyItem, bool> predicate)
        {
            var keys = _allKeys.Where(predicate).ToList();
            bool anyUnblocked = keys.Any(k => !k.IsBlocked);
            bool targetState = anyUnblocked;

            foreach (var k in keys)
            {
                k.IsBlocked = targetState;
                _hookManager.SetKeyBlocked(k.VkCode, targetState);
                if (k.RelatedVkCodes != null)
                {
                    foreach (int r in k.RelatedVkCodes)
                        _hookManager.SetKeyBlocked(r, targetState);
                }
            }
            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        private void OnKeySearchChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtKeySearch.Text.Trim();
            _filteredKeys.Clear();

            if (string.IsNullOrEmpty(query))
            {
                foreach (var k in _allKeys)
                    _filteredKeys.Add(k);
            }
            else
            {
                foreach (var k in _allKeys.Where(k => k.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     k.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     k.HexCode.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    _filteredKeys.Add(k);
                }
            }
        }

        #endregion

        #region Key Remapping (Broken Key Fix)

        private void OnAddRemapRuleClicked(object sender, RoutedEventArgs e)
        {
            var srcKey = CmbRemapSource.SelectedItem as KeyItem;
            var dstKey = CmbRemapTarget.SelectedItem as KeyItem;

            if (srcKey == null || dstKey == null)
            {
                MessageBox.Show("Please select both a source key (to press) and a target key (to type).", "Incomplete Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (srcKey.VkCode == dstKey.VkCode)
            {
                MessageBox.Show("Source key and target key cannot be the same key.", "Invalid Remapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddRemapRuleInternal(srcKey.VkCode, dstKey.VkCode, srcKey.Label, dstKey.Label, "Custom user remapping rule");
            SaveSettings();
        }

        private void AddRemapRuleInternal(int sourceVk, int targetVk, string sourceName, string targetName, string description)
        {
            _hookManager.AddRemapping(sourceVk, targetVk);

            // Remove existing rule for the same source key if any
            var existing = _remapRules.FirstOrDefault(r => r.SourceVk == sourceVk);
            if (existing != null)
            {
                _remapRules.Remove(existing);
            }

            _remapRules.Add(new KeyRemapItem
            {
                SourceVk = sourceVk,
                SourceName = sourceName,
                TargetVk = targetVk,
                TargetName = targetName,
                Description = description
            });

            if (TxtRemapCount != null)
            {
                TxtRemapCount.Text = string.Format(" ({0} rules active)", _remapRules.Count);
            }
        }

        private void OnDeleteRemapRuleClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var rule = btn.Tag as KeyRemapItem;
            if (rule == null) return;

            _hookManager.RemoveRemapping(rule.SourceVk);
            _remapRules.Remove(rule);

            if (TxtRemapCount != null)
            {
                TxtRemapCount.Text = string.Format(" ({0} rules active)", _remapRules.Count);
            }

            SaveSettings();
        }

        private void OnClearAllRemapsClicked(object sender, RoutedEventArgs e)
        {
            if (_remapRules.Count == 0) return;

            var res = MessageBox.Show("Are you sure you want to remove all active key remappings?", "Clear Remappings", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _hookManager.ClearRemappings();
                _remapRules.Clear();

                if (TxtRemapCount != null)
                {
                    TxtRemapCount.Text = " (0 rules active)";
                }

                SaveSettings();
            }
        }

        private void OnPresetRemap1ToC(object sender, RoutedEventArgs e)
        {
            AddRemapRuleInternal(0x31, 0x43, "1 / !", "C", "Emergency fix: broken 'C' key remapped to '1'");
            SaveSettings();
            MessageBox.Show("Preset Applied: Pressing '1' will now type 'C'!\nYou can test it right away in the Live Test Scratchpad below.", "Preset Activated", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnPresetRemapCtrlToEnter(object sender, RoutedEventArgs e)
        {
            AddRemapRuleInternal(0xA3, 0x0D, "Right Ctrl", "Enter", "Emergency fix: broken 'Enter' key remapped to 'Right Ctrl'");
            SaveSettings();
            MessageBox.Show("Preset Applied: Pressing 'Right Ctrl' will now act as 'Enter'!", "Preset Activated", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnPresetRemapAltToSpace(object sender, RoutedEventArgs e)
        {
            AddRemapRuleInternal(0xA5, 0x20, "Right Alt", "Space", "Emergency fix: broken 'Space' key remapped to 'Right Alt'");
            SaveSettings();
            MessageBox.Show("Preset Applied: Pressing 'Right Alt' will now act as 'Space'!", "Preset Activated", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnPresetRemapCapsToEsc(object sender, RoutedEventArgs e)
        {
            AddRemapRuleInternal(0x14, 0x1B, "Caps Lock", "Escape", "Developer preset: 'Caps Lock' remapped to 'Esc'");
            SaveSettings();
            MessageBox.Show("Preset Applied: Pressing 'Caps Lock' will now act as 'Escape'!", "Preset Activated", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnClearRemapTestBoxClicked(object sender, RoutedEventArgs e)
        {
            if (TxtRemapTestBox != null)
            {
                TxtRemapTestBox.Text = "";
            }
        }

        #endregion

        #region Precise Mouse Actions

        private void OnMouseActionToggleChanged(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null) return;
            string tag = chk.Tag as string;
            bool isBlocked = chk.IsChecked == true;

            switch (tag)
            {
                case "LeftClick":
                    _hookManager.BlockLeftClick = isBlocked;
                    break;
                case "RightClick":
                    _hookManager.BlockRightClick = isBlocked;
                    break;
                case "MiddleClick":
                    _hookManager.BlockMiddleClick = isBlocked;
                    break;
                case "MouseMove":
                    _hookManager.BlockMouseMove = isBlocked;
                    break;
                case "ScrollUp":
                    _hookManager.BlockScrollUp = isBlocked;
                    break;
                case "ScrollDown":
                    _hookManager.BlockScrollDown = isBlocked;
                    break;
                case "ScrollWheel":
                    _hookManager.BlockScrollWheel = isBlocked;
                    break;
                case "SideButtons":
                    _hookManager.BlockSideButtons = isBlocked;
                    _hookManager.BlockHScrollWheel = isBlocked;
                    break;
            }

            UpdateUiState();
            CheckStartAutoRevertTimer();
        }

        #endregion

        #region Live Test Pad & Logs

        private void OnInputIntercepted(EventLogItem log)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _eventLogs.Insert(0, log);
                if (_eventLogs.Count > 150)
                {
                    _eventLogs.RemoveAt(_eventLogs.Count - 1);
                }

                if (!log.IsBlocked && log.Source == "Keyboard")
                {
                    int current;
                    string countStr = TxtTestAllowedCount.Text.Replace("Keystrokes: ", "").Replace(" allowed", "");
                    if (int.TryParse(countStr, out current))
                    {
                        TxtTestAllowedCount.Text = "Keystrokes: " + (current + 1) + " allowed";
                    }
                }
            }));
        }

        private void OnPadMouseDown(object sender, MouseButtonEventArgs e)
        {
            string btnName = e.ChangedButton.ToString();
            TxtMouseTestFeedback.Text = "CLICK DETECTED: " + btnName.ToUpper();
            TxtMouseTestFeedback.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }

        private void OnPadMouseWheel(object sender, MouseWheelEventArgs e)
        {
            string dir = e.Delta > 0 ? "SCROLL UP 🔼" : "SCROLL DOWN 🔽";
            TxtMouseTestFeedback.Text = dir + " (" + (e.Delta > 0 ? "+" : "") + e.Delta + ")";
            TxtMouseTestFeedback.Foreground = new SolidColorBrush(Color.FromRgb(0x06, 0xB6, 0xD4));
        }

        private void OnClearTestBoxClicked(object sender, RoutedEventArgs e)
        {
            TxtTestInput.Text = "";
            TxtTestAllowedCount.Text = "Keystrokes: 0 allowed";
        }

        private void OnClearLogClicked(object sender, RoutedEventArgs e)
        {
            _eventLogs.Clear();
        }

        #endregion

        #region Auto-Revert Failsafe Timer

        private void OnAutoRevertChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (CmbAutoRevert.SelectedIndex)
            {
                case 1: _configuredAutoRevertSeconds = 30; break;
                case 2: _configuredAutoRevertSeconds = 60; break;
                case 3: _configuredAutoRevertSeconds = 180; break;
                case 4: _configuredAutoRevertSeconds = 300; break;
                default: _configuredAutoRevertSeconds = 0; break;
            }
        }

        private void CheckStartAutoRevertTimer()
        {
            bool hasLocks = _hookManager.MasterKeyboardBlocked ||
                            _hookManager.MasterMouseBlocked ||
                            _hookManager.BlockLeftClick ||
                            _allKeys.Any(k => k.IsBlocked);

            if (_configuredAutoRevertSeconds > 0 && hasLocks && _autoRevertTimer == null)
            {
                StartCountdown(_configuredAutoRevertSeconds);
            }
        }

        private void StartCountdown(int seconds)
        {
            _remainingSeconds = seconds;
            TxtCountdownTimer.Text = "⏳ Auto-restoring all inputs in: " + _remainingSeconds + "s";
            TxtCountdownTimer.Visibility = Visibility.Visible;
            BtnCancelCountdown.Visibility = Visibility.Visible;

            if (_autoRevertTimer != null)
            {
                _autoRevertTimer.Stop();
            }

            _autoRevertTimer = new DispatcherTimer();
            _autoRevertTimer.Interval = TimeSpan.FromSeconds(1);
            _autoRevertTimer.Tick += (s, e) =>
            {
                _remainingSeconds--;
                if (_remainingSeconds <= 0)
                {
                    StopAutoRevertTimer();
                    ExecuteEmergencyRestore();
                    MessageBox.Show("Safety Auto-Restore: All keyboard and mouse inputs have been re-enabled.", "Input Restored", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    TxtCountdownTimer.Text = "⏳ Auto-restoring all inputs in: " + _remainingSeconds + "s";
                }
            };
            _autoRevertTimer.Start();
        }

        private void StopAutoRevertTimer()
        {
            if (_autoRevertTimer != null)
            {
                _autoRevertTimer.Stop();
                _autoRevertTimer = null;
            }
            TxtCountdownTimer.Visibility = Visibility.Collapsed;
            BtnCancelCountdown.Visibility = Visibility.Collapsed;
        }

        private void OnCancelCountdownClicked(object sender, RoutedEventArgs e)
        {
            StopAutoRevertTimer();
        }

        #endregion

        #region UI State Synchronization

        private void UpdateUiState()
        {
            // Keyboard master status
            bool kbdBlocked = _hookManager.MasterKeyboardBlocked;
            TxtMasterKeyboardStatus.Text = kbdBlocked ? "Status: ALL KEYS BLOCKED" : "Status: KEYBOARD ACTIVE";
            TxtMasterKeyboardStatus.Foreground = kbdBlocked ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));

            int keysBlockedCount = _allKeys.Count(k => k.IsBlocked);
            TxtKeyboardBlockedCount.Text = "Individual keys blocked: " + keysBlockedCount + (kbdBlocked ? " (All Keys Overridden by Master Lock)" : "");

            // Mouse master status
            bool mouseBlocked = _hookManager.MasterMouseBlocked;
            TxtMasterMouseStatus.Text = mouseBlocked ? "Status: ALL MOUSE ACTIONS BLOCKED" : "Status: TOUCHPAD / MOUSE ACTIVE";
            TxtMasterMouseStatus.Foreground = mouseBlocked ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));

            int mouseBlockedCount = (_hookManager.BlockLeftClick ? 1 : 0) +
                                    (_hookManager.BlockRightClick ? 1 : 0) +
                                    (_hookManager.BlockMiddleClick ? 1 : 0) +
                                    (_hookManager.BlockMouseMove ? 1 : 0) +
                                    (_hookManager.BlockScrollUp ? 1 : 0) +
                                    (_hookManager.BlockScrollDown ? 1 : 0) +
                                    (_hookManager.BlockScrollWheel ? 1 : 0) +
                                    (_hookManager.BlockSideButtons ? 1 : 0);
            TxtMouseBlockedCount.Text = "Individual mouse actions blocked: " + mouseBlockedCount + (mouseBlocked ? " (All Overridden by Master Lock)" : "");

            // Top Status Badges
            PillKeyboard.Background = (kbdBlocked || keysBlockedCount > 0) ? new SolidColorBrush(Color.FromRgb(0x38, 0x12, 0x16)) : new SolidColorBrush(Color.FromRgb(0x13, 0x2E, 0x22));
            PillKeyboard.BorderBrush = (kbdBlocked || keysBlockedCount > 0) ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
            TxtPillKeyboard.Text = kbdBlocked ? "⌨️ KEYBOARD: MASTER BLOCKED" : (keysBlockedCount > 0 ? "⌨️ KEYBOARD: " + keysBlockedCount + " KEYS BLOCKED" : "⌨️ KEYBOARD: ACTIVE");
            TxtPillKeyboard.Foreground = (kbdBlocked || keysBlockedCount > 0) ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));

            PillMouse.Background = (mouseBlocked || mouseBlockedCount > 0) ? new SolidColorBrush(Color.FromRgb(0x38, 0x12, 0x16)) : new SolidColorBrush(Color.FromRgb(0x13, 0x2E, 0x22));
            PillMouse.BorderBrush = (mouseBlocked || mouseBlockedCount > 0) ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
            TxtPillMouse.Text = mouseBlocked ? "🖱️ MOUSE: MASTER BLOCKED" : (mouseBlockedCount > 0 ? "🖱️ MOUSE: " + mouseBlockedCount + " BLOCKED" : "🖱️ TOUCHPAD / MOUSE: ACTIVE");
            TxtPillMouse.Foreground = (mouseBlocked || mouseBlockedCount > 0) ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));

            // Mouse button statuses
            SetActionStatus(TxtLeftClickStatus, _hookManager.BlockLeftClick || mouseBlocked);
            SetActionStatus(TxtRightClickStatus, _hookManager.BlockRightClick || mouseBlocked);
            SetActionStatus(TxtMiddleClickStatus, _hookManager.BlockMiddleClick || mouseBlocked);
            SetActionStatus(TxtMouseMoveStatus, _hookManager.BlockMouseMove || mouseBlocked);
            SetActionStatus(TxtScrollUpStatus, _hookManager.BlockScrollUp || _hookManager.BlockScrollWheel || mouseBlocked);
            SetActionStatus(TxtScrollDownStatus, _hookManager.BlockScrollDown || _hookManager.BlockScrollWheel || mouseBlocked);
            SetActionStatus(TxtScrollWheelStatus, _hookManager.BlockScrollWheel || mouseBlocked);
            SetActionStatus(TxtSideButtonsStatus, _hookManager.BlockSideButtons || mouseBlocked);

            // Visual Touchpad Diagram updates
            DiagTouchSurface.Background = (_hookManager.BlockMouseMove || mouseBlocked) ? new SolidColorBrush(Color.FromRgb(0x38, 0x12, 0x16)) : new SolidColorBrush(Color.FromRgb(0x1E, 0x20, 0x2E));
            DiagLeftButton.Background = (_hookManager.BlockLeftClick || mouseBlocked) ? new SolidColorBrush(Color.FromRgb(0x38, 0x12, 0x16)) : new SolidColorBrush(Color.FromRgb(0x1E, 0x20, 0x2E));
            DiagRightButton.Background = (_hookManager.BlockRightClick || mouseBlocked) ? new SolidColorBrush(Color.FromRgb(0x38, 0x12, 0x16)) : new SolidColorBrush(Color.FromRgb(0x1E, 0x20, 0x2E));

            // Footer summary
            TxtFooterSummary.Text = "Active Locks: " + (kbdBlocked ? "All Keys" : keysBlockedCount + " Keys") + " | " + (mouseBlocked ? "All Mouse" : mouseBlockedCount + " Mouse Actions");
            TxtFooterSummary.Foreground = (kbdBlocked || keysBlockedCount > 0 || mouseBlocked || mouseBlockedCount > 0) ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }

        private static void SetActionStatus(TextBlock tb, bool blocked)
        {
            tb.Text = blocked ? "Status: BLOCKED" : "Status: ACTIVE";
            tb.Foreground = blocked ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)) : new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }

        #endregion
    }
}
