using System;
using System.Collections.Generic;
using System.IO;

namespace LaptopInputControl.Services
{
    public class AppSettings
    {
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
        public int TargetScope { get; set; }
        public List<int> BlockedVkCodes { get; set; }
        public Dictionary<int, int> Remappings { get; set; }
        public int AutoRevertSeconds { get; set; }
        public string Theme { get; set; }

        public AppSettings()
        {
            BlockedVkCodes = new List<int>();
            Remappings = new Dictionary<int, int>();
            AutoRevertSeconds = 0; // 0 = disabled
            Theme = "ObsidianDark";
        }
    }

    public class SettingsService
    {
        private readonly string _configPath;

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "LaptopInputControl");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            _configPath = Path.Combine(folder, "settings.cfg");
        }

        public AppSettings Load()
        {
            var settings = new AppSettings();
            try
            {
                if (File.Exists(_configPath))
                {
                    string[] lines = File.ReadAllLines(_configPath);
                    foreach (string rawLine in lines)
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                        int idx = line.IndexOf('=');
                        if (idx > 0)
                        {
                            string key = line.Substring(0, idx).Trim();
                            string val = line.Substring(idx + 1).Trim();

                            switch (key)
                            {
                                case "MasterKeyboardBlocked": settings.MasterKeyboardBlocked = bool.Parse(val); break;
                                case "MasterMouseBlocked": settings.MasterMouseBlocked = bool.Parse(val); break;
                                case "BlockLeftClick": settings.BlockLeftClick = bool.Parse(val); break;
                                case "BlockRightClick": settings.BlockRightClick = bool.Parse(val); break;
                                case "BlockMiddleClick": settings.BlockMiddleClick = bool.Parse(val); break;
                                case "BlockMouseMove": settings.BlockMouseMove = bool.Parse(val); break;
                                case "BlockScrollWheel": settings.BlockScrollWheel = bool.Parse(val); break;
                                case "BlockScrollUp": settings.BlockScrollUp = bool.Parse(val); break;
                                case "BlockScrollDown": settings.BlockScrollDown = bool.Parse(val); break;
                                case "BlockHScrollWheel": settings.BlockHScrollWheel = bool.Parse(val); break;
                                case "BlockSideButtons": settings.BlockSideButtons = bool.Parse(val); break;
                                case "TargetScope": settings.TargetScope = int.Parse(val); break;
                                case "Remappings":
                                    string[] pairs = val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (string pair in pairs)
                                    {
                                        string[] parts = pair.Split(':');
                                        if (parts.Length == 2)
                                        {
                                            int src, dst;
                                            if (int.TryParse(parts[0].Trim(), out src) && int.TryParse(parts[1].Trim(), out dst))
                                            {
                                                settings.Remappings[src] = dst;
                                            }
                                        }
                                    }
                                    break;
                                case "AutoRevertSeconds": settings.AutoRevertSeconds = int.Parse(val); break;
                                case "Theme": settings.Theme = val; break;
                                case "BlockedVkCodes":
                                    string[] vks = val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (string v in vks)
                                    {
                                        int parsed;
                                        if (int.TryParse(v.Trim(), out parsed))
                                            settings.BlockedVkCodes.Add(parsed);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fall back to default on error
            }
            return settings;
        }

        public void Save(AppSettings settings)
        {
            try
            {
                using (var sw = new StreamWriter(_configPath, false))
                {
                    sw.WriteLine("# Laptop Input Controller Config");
                    sw.WriteLine("MasterKeyboardBlocked=" + settings.MasterKeyboardBlocked);
                    sw.WriteLine("MasterMouseBlocked=" + settings.MasterMouseBlocked);
                    sw.WriteLine("BlockLeftClick=" + settings.BlockLeftClick);
                    sw.WriteLine("BlockRightClick=" + settings.BlockRightClick);
                    sw.WriteLine("BlockMiddleClick=" + settings.BlockMiddleClick);
                    sw.WriteLine("BlockMouseMove=" + settings.BlockMouseMove);
                    sw.WriteLine("BlockScrollWheel=" + settings.BlockScrollWheel);
                    sw.WriteLine("BlockScrollUp=" + settings.BlockScrollUp);
                    sw.WriteLine("BlockScrollDown=" + settings.BlockScrollDown);
                    sw.WriteLine("BlockHScrollWheel=" + settings.BlockHScrollWheel);
                    sw.WriteLine("BlockSideButtons=" + settings.BlockSideButtons);
                    sw.WriteLine("TargetScope=" + settings.TargetScope);
                    if (settings.Remappings != null && settings.Remappings.Count > 0)
                    {
                        var remapPairs = new List<string>();
                        foreach (var kvp in settings.Remappings)
                        {
                            remapPairs.Add(kvp.Key + ":" + kvp.Value);
                        }
                        sw.WriteLine("Remappings=" + string.Join(",", remapPairs));
                    }
                    else
                    {
                        sw.WriteLine("Remappings=");
                    }
                    sw.WriteLine("AutoRevertSeconds=" + settings.AutoRevertSeconds);
                    sw.WriteLine("Theme=" + settings.Theme);
                    sw.WriteLine("BlockedVkCodes=" + string.Join(",", settings.BlockedVkCodes));
                }
            }
            catch
            {
            }
        }
    }
}
