# 🛡️ Laptop Input Shield & Controller (v1.5 - Key Remapper & Installer)

A high-performance Windows desktop application designed to enable, disable, reassign, and granularly control laptop built-in keyboards and trackpads/mice.

---

## 🚀 Running & Installing on PC

You have two easy ways to use this app:

### 1. 📦 Install on PC (Full Windows Integration)
Run the setup wizard:
```
Setup.exe
```
*(or run `LaptopInputControl_Setup.exe` / `Install.bat`)*

**What the Installer does:**
- Installs the app to `%LOCALAPPDATA%\Programs\LaptopInputControl`.
- Creates a **Desktop Shortcut** (`Laptop Input Shield`).
- Creates a **Start Menu Shortcut** (searchable in Windows Start).
- Registers in Windows **Settings > Apps > Installed apps** (and Control Panel) so it can be managed and uninstalled like any official Windows software.
- Optional: Add to Windows Startup.
- Bundles a clean uninstaller (`Uninstall.exe`).

### 2. ⚡ Portable Mode (Zero Installation)
Simply double-click:
```
LaptopInputControl.exe
```
*Runs immediately without installation. Fully portable.*

---

## 🔄 Key Remapping Engine (Broken Key Fix)

Have one or more keys that stopped working on your laptop keyboard (e.g. physical `C` key failed, or `Enter` or `Space` key dead)? You can now **reassign any functional key to type that character instead** with zero latency!

### 🌟 How Broken Key Reassignment Works:
1. **Source Key (What You Press)**: Select any convenient working key (for example, the `1` key or `Right Ctrl` or `Right Alt`).
2. **Target Key (What Is Typed)**: Select the key that stopped working on your laptop (for example, `C`).
3. Click **➕ Add / Update Remap Rule**.
4. **Instant Action**: Whenever you press the source key (`1`), the app silently drops the original keypress and injects the target character (`C`) using high-speed Win32 `SendInput` with recursion guards.
5. **Real-Time Scratchpad**: Immediately test your remapped keys right on the same tab in the **Live Test Scratchpad** to verify that pressing `1` outputs `C`.

### ⚡ One-Click Emergency Presets:
- 🔧 **Broken 'C' Key**: Remaps `1` ➔ Types `C`.
- 🔧 **Broken 'Enter' Key**: Remaps `Right Ctrl` ➔ Acts as `Enter`.
- 🔧 **Broken 'Space' Key**: Remaps `Right Alt` ➔ Acts as `Space`.
- 🔧 **Vim / Coding Preset**: Remaps `Caps Lock` ➔ Acts as `Escape`.

### 🎯 Device Scope Aware:
- If you set Target Scope to **Built-in Laptop Only**, the remap rule only activates when pressing the key on your **laptop keyboard**, keeping your external USB or Bluetooth keyboard completely standard!

---

## 🗑️ How to Uninstall
If installed via `Setup.exe`, you can uninstall cleanly anytime:
1. **Via Windows Settings:**
   Open **Settings > Apps > Installed apps > Laptop Input Shield & Controller > Uninstall**.
2. **Via Uninstaller directly:**
   Run `Uninstall.exe` inside `%LOCALAPPDATA%\Programs\LaptopInputControl`.

---

## 🌙 Dark Mode Overhaul & Theme Engine

### 🎨 4 Selectable Visual Themes
Switch between themes dynamically right from the titlebar dropdown (selection is remembered across app restarts):
1. **🌑 Obsidian Dark (Default)**: Balanced, neutral dark palette (`#0D0E13` background, `#161722` cards, `#262738` borders).
2. **🖤 OLED Pure Black**: True pitch-black palette (`#000000` background, `#0C0C10` cards) for maximum contrast and **battery saving** on laptop OLED/AMOLED screens.
3. **🌌 Midnight Cyber Blue**: Deep navy / night sky aesthetic (`#080C16` background, `#131C30` cards, `#1E2D4A` borders, `#38BDF8` cyan accents).
4. **☀️ Clean Slate Light**: Crisp daylight palette (`#F1F3F9` background, `#FFFFFF` cards, `#D5D9E6` borders) for outdoor sunlight visibility.

---

## 🪟 Window Management Controls
- **Minimize (`—`)**: Smooth taskbar minimization.
- **Maximize / Restore (`□` / `❐`)**: Toggles between windowed mode and full-screen layout with dynamic icon adaptation.
- **Close (`✕`)**: Closes the application with a signature Windows red hover state.
- **Aero Snap & Drag**: Full support for native Windows titlebar dragging, double-clicking to maximize, and Aero edge snapping.

---

## 🌟 Core Functionality

### 1. ⚡ Master Toggles & Presets
- **Master Laptop Keyboard Switch**: Instantly block or allow all keyboard keystrokes across the entire system.
- **Master Touchpad & Mouse Switch**: Instantly block or allow all pointer clicks, cursor movement, and scrolling.
- **Quick Profiles**:
  - **Gamer Mode**: Locks only the Windows key (`Win`) and `Caps Lock` to prevent accidental desktop switching during games.
  - **Cleaning Mode**: Temporarily locks all input for 30 seconds so you can wipe your keyboard and trackpad.
  - **Palm Rest Mode**: Disables only cursor movement so resting your palms while typing doesn't accidentally move the mouse.

### 2. 🎯 Target Device Scope (Built-in Laptop vs External USB & Bluetooth)
You can isolate exactly which devices your locks apply to using the **Target Device Scope** selector:
- 💻 **Built-in Laptop Only (Recommended for External Keyboard/Mouse users)**:
  - Locks apply **strictly** to the laptop's built-in keyboard and touchpad.
  - Connected external USB, wired, 2.4GHz wireless dongle, and Bluetooth keyboards/mice operate **100% unrestricted with zero blocked keys**!
- 🔌 **External Devices Only**:
  - Locks apply **strictly** to connected external USB, wired, and Bluetooth devices.
  - The laptop's built-in keyboard and touchpad remain unaffected.
- 🌐 **All Devices (Global Lock)**:
  - Locks apply globally across built-in and external devices simultaneously.

### 3. ⌨️ Precise Keyboard Matrix (Per-Button Toggles)
- **Visual Interactive Keyboard**: Click any individual key on the full 104-key canvas to toggle it between **ENABLED** and **DISABLED**.
- **Searchable Key Grid**: Filter keys by name or hex code (e.g. search `Space`, `Enter`, `F5`, `Ctrl`) and toggle directly.
- **Batch Category Toggles**: Function Keys (`F1`–`F12`), Windows Keys (`LWin`, `RWin`), `Caps Lock`, Arrow Keys, Numpad, and Alphabet Keys.

### 3. 🖱️ Precise Mouse & Trackpad Matrix (Per-Action Toggles)
- **Left Click**: Blocks primary button clicks and single-finger trackpad taps.
- **Right Click**: Blocks right clicks and two-finger context menu taps.
- **Middle Click**: Blocks scroll wheel clicks and three-finger taps.
- **Cursor Movement**: Freezes the cursor pointer (blocks touchpad surface tracking; palm-rest protection).
- **🔼 Scroll Up**: Independently blocks only upward scrolling (wheel forward / two-finger swipe down); allows scrolling down without jumping back.
- **🔽 Scroll Down**: Independently blocks only downward scrolling (wheel backward / two-finger swipe up); pins reading position.
- **📜 Master Vertical Scroll**: Blocks both Scroll Up and Scroll Down simultaneously.
- **↔️ Horizontal Scroll & Side Buttons**: Blocks tilt wheel, horizontal gestures, and browser Back/Forward thumb buttons (`XButton1` and `XButton2`).
- **Interactive Visual Trackpad Diagram**: Click directly on the diagram surface or buttons to toggle them.
- **💡 Keyboard-Driven Scrolling Fallback**: When mouse/trackpad scrolling is blocked, you can seamlessly scroll using `Page Down` / `Page Up`, `Down Arrow (↓)` / `Up Arrow (↑)`, or `Space` / `Shift + Space`.

### 4. 💻 Laptop Built-in Hardware Control (PnP Device Manager)
- Directly communicates with the Windows Plug and Play (PnP) subsystem.
- Specifically detects:
  - **Dell Touchpad** (`ACPI\DLL05CA\4&D90DFE7&0`)
  - **Standard PS/2 Laptop Keyboard** (`ACPI\DLLK05CA\4&D90DFE7&0`)
  - External USB / Bluetooth mice and keyboards.
- Allows disabling/enabling the hardware device at the Windows driver level.

### 5. 🚨 Safety Failsafes & Zero-Lockout Protection
1. **Emergency Rescue Hotkey**: `Ctrl + Shift + Alt + F12` is hardcoded to **bypass all hooks**. Pressing this combo instantly unlocks all inputs.
2. **Hold Escape Failsafe**: Holding the physical `Escape` key for 2.5 seconds triggers an emergency reset.
3. **Safety Auto-Restore Countdown**: Configurable timer (30s, 1m, 3m, 5m, or Off) that automatically restores input if not confirmed.
4. **Persistent Emergency Restore Button**: A prominent red `🚨 EMERGENCY RESTORE` button is always visible in the top header.

### 6. 🎯 Live Test Zone & Event Monitor
- **Keyboard Test Pad**: Type inside the text area to immediately verify that blocked keys do not produce text.
- **Mouse Test Pad**: Click or scroll on the test surface to verify click, middle click, and wheel blocks.
- **Real-Time Event Stream**: Displays live logs showing timestamp, input source, key/action, and whether it was `ALLOWED` or `BLOCKED`.

---

## 🛠️ Rebuilding from Source
Double-click `build.bat` to rebuild the entire suite (Application, Uninstaller, and Setup Installer):
```bat
build.bat
```
