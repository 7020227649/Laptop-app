using System;
using System.Windows;
using System.Windows.Media;

namespace LaptopInputControl.Services
{
    public enum AppThemeMode
    {
        ObsidianDark,
        OledBlack,
        MidnightBlue,
        CleanLight
    }

    public static class ThemeManager
    {
        private static AppThemeMode _currentTheme = AppThemeMode.ObsidianDark;
        public static AppThemeMode CurrentTheme
        {
            get { return _currentTheme; }
            private set { _currentTheme = value; }
        }

        public static void ApplyTheme(AppThemeMode theme)
        {
            CurrentTheme = theme;
            if (Application.Current == null) return;
            var res = Application.Current.Resources;
            if (res == null) return;

            switch (theme)
            {
                case AppThemeMode.OledBlack:
                    // Pure OLED Black (Maximum battery saving & contrast)
                    SetBrush(res, "ThemeBg", "#000000");
                    SetBrush(res, "ThemeHeaderBg", "#08080A");
                    SetBrush(res, "ThemeSurface", "#0C0C10");
                    SetBrush(res, "ThemeSurfaceNested", "#050508");
                    SetBrush(res, "ThemeCardBg", "#0C0C10");
                    SetBrush(res, "ThemeInputBg", "#050508");
                    SetBrush(res, "ThemeBorder", "#1E1E28");
                    SetBrush(res, "ThemeBorderSubtle", "#14141E");
                    SetBrush(res, "ThemeTextPrimary", "#FFFFFF");
                    SetBrush(res, "ThemeTextSecondary", "#8E8EA0");
                    SetBrush(res, "ThemeAccent", "#3B82F6");
                    SetBrush(res, "ThemeAccentGlow", "#1D4ED8");
                    SetBrush(res, "ThemeActiveGreen", "#10B981");
                    SetBrush(res, "ThemeActiveGreenBg", "#0F2E20");
                    SetBrush(res, "ThemeBlockedRed", "#EF4444");
                    SetBrush(res, "ThemeBlockedRedBg", "#381014");
                    SetBrush(res, "ThemeKeyNormalBg", "#12131A");
                    SetBrush(res, "ThemeKeyNormalBorder", "#242636");
                    break;

                case AppThemeMode.MidnightBlue:
                    // Deep Midnight Navy / Cyber Blue
                    SetBrush(res, "ThemeBg", "#080C16");
                    SetBrush(res, "ThemeHeaderBg", "#0E1526");
                    SetBrush(res, "ThemeSurface", "#131C30");
                    SetBrush(res, "ThemeSurfaceNested", "#0B1220");
                    SetBrush(res, "ThemeCardBg", "#131C30");
                    SetBrush(res, "ThemeInputBg", "#0B1220");
                    SetBrush(res, "ThemeBorder", "#1E2D4A");
                    SetBrush(res, "ThemeBorderSubtle", "#16233B");
                    SetBrush(res, "ThemeTextPrimary", "#F8FAFC");
                    SetBrush(res, "ThemeTextSecondary", "#94A3B8");
                    SetBrush(res, "ThemeAccent", "#38BDF8");
                    SetBrush(res, "ThemeAccentGlow", "#0284C7");
                    SetBrush(res, "ThemeActiveGreen", "#34D399");
                    SetBrush(res, "ThemeActiveGreenBg", "#064E3B");
                    SetBrush(res, "ThemeBlockedRed", "#F87171");
                    SetBrush(res, "ThemeBlockedRedBg", "#450A0A");
                    SetBrush(res, "ThemeKeyNormalBg", "#17233D");
                    SetBrush(res, "ThemeKeyNormalBorder", "#2A3D66");
                    break;

                case AppThemeMode.CleanLight:
                    // High-visibility daylight mode
                    SetBrush(res, "ThemeBg", "#F1F3F9");
                    SetBrush(res, "ThemeHeaderBg", "#E2E6F0");
                    SetBrush(res, "ThemeSurface", "#FFFFFF");
                    SetBrush(res, "ThemeSurfaceNested", "#F8F9FD");
                    SetBrush(res, "ThemeCardBg", "#FFFFFF");
                    SetBrush(res, "ThemeInputBg", "#FFFFFF");
                    SetBrush(res, "ThemeBorder", "#D5D9E6");
                    SetBrush(res, "ThemeBorderSubtle", "#E5E7EB");
                    SetBrush(res, "ThemeTextPrimary", "#0F172A");
                    SetBrush(res, "ThemeTextSecondary", "#475569");
                    SetBrush(res, "ThemeAccent", "#2563EB");
                    SetBrush(res, "ThemeAccentGlow", "#93C5FD");
                    SetBrush(res, "ThemeActiveGreen", "#059669");
                    SetBrush(res, "ThemeActiveGreenBg", "#D1FAE5");
                    SetBrush(res, "ThemeBlockedRed", "#DC2626");
                    SetBrush(res, "ThemeBlockedRedBg", "#FEE2E2");
                    SetBrush(res, "ThemeKeyNormalBg", "#F1F5F9");
                    SetBrush(res, "ThemeKeyNormalBorder", "#CBD5E1");
                    break;

                case AppThemeMode.ObsidianDark:
                default:
                    // Neutral Obsidian Dark (Default Balanced Dark Mode)
                    SetBrush(res, "ThemeBg", "#0D0E13");
                    SetBrush(res, "ThemeHeaderBg", "#141520");
                    SetBrush(res, "ThemeSurface", "#161722");
                    SetBrush(res, "ThemeSurfaceNested", "#11121B");
                    SetBrush(res, "ThemeCardBg", "#161722");
                    SetBrush(res, "ThemeInputBg", "#11121B");
                    SetBrush(res, "ThemeBorder", "#262738");
                    SetBrush(res, "ThemeBorderSubtle", "#1E1F2C");
                    SetBrush(res, "ThemeTextPrimary", "#F4F4F8");
                    SetBrush(res, "ThemeTextSecondary", "#8E8EA6");
                    SetBrush(res, "ThemeAccent", "#3B82F6");
                    SetBrush(res, "ThemeAccentGlow", "#1D4ED8");
                    SetBrush(res, "ThemeActiveGreen", "#10B981");
                    SetBrush(res, "ThemeActiveGreenBg", "#132E22");
                    SetBrush(res, "ThemeBlockedRed", "#EF4444");
                    SetBrush(res, "ThemeBlockedRedBg", "#381216");
                    SetBrush(res, "ThemeKeyNormalBg", "#1A1C28");
                    SetBrush(res, "ThemeKeyNormalBorder", "#32384C");
                    break;
            }
        }

        private static void SetBrush(ResourceDictionary res, string key, string hexColor)
        {
            var brush = new BrushConverter().ConvertFromString(hexColor) as SolidColorBrush;
            if (brush != null)
            {
                if (brush.CanFreeze) brush.Freeze();
                res[key] = brush;
            }
        }
    }
}
