using System.Collections.Generic;
using LaptopInputControl.Models;

namespace LaptopInputControl.Services
{
    public static class KeyboardLayoutFactory
    {
        public static List<KeyItem> CreateFullKeyboardLayout()
        {
            var keys = new List<KeyItem>();

            // ROW 0 - Function Row (Y = 0)
            keys.Add(new KeyItem(0x1B, "Esc", "Control", 0, 0, 42, 32, "", null, 0, 0));
            // F1 - F4
            keys.Add(new KeyItem(0x70, "F1", "Function", 0, 1, 38, 32, "", null, 64, 0));
            keys.Add(new KeyItem(0x71, "F2", "Function", 0, 2, 38, 32, "", null, 106, 0));
            keys.Add(new KeyItem(0x72, "F3", "Function", 0, 3, 38, 32, "", null, 148, 0));
            keys.Add(new KeyItem(0x73, "F4", "Function", 0, 4, 38, 32, "", null, 190, 0));
            // F5 - F8
            keys.Add(new KeyItem(0x74, "F5", "Function", 0, 5, 38, 32, "", null, 244, 0));
            keys.Add(new KeyItem(0x75, "F6", "Function", 0, 6, 38, 32, "", null, 286, 0));
            keys.Add(new KeyItem(0x76, "F7", "Function", 0, 7, 38, 32, "", null, 328, 0));
            keys.Add(new KeyItem(0x77, "F8", "Function", 0, 8, 38, 32, "", null, 370, 0));
            // F9 - F12
            keys.Add(new KeyItem(0x78, "F9", "Function", 0, 9, 38, 32, "", null, 424, 0));
            keys.Add(new KeyItem(0x79, "F10", "Function", 0, 10, 38, 32, "", null, 466, 0));
            keys.Add(new KeyItem(0x7A, "F11", "Function", 0, 11, 38, 32, "", null, 508, 0));
            keys.Add(new KeyItem(0x7B, "F12", "Function", 0, 12, 38, 32, "", null, 550, 0));
            // System
            keys.Add(new KeyItem(0x2C, "PrtSc", "System", 0, 13, 40, 32, "", null, 660, 0));
            keys.Add(new KeyItem(0x91, "ScrLk", "System", 0, 14, 40, 32, "", null, 704, 0));
            keys.Add(new KeyItem(0x13, "Pause", "System", 0, 15, 40, 32, "", null, 748, 0));

            // ROW 1 - Number Row (Y = 38)
            keys.Add(new KeyItem(0xC0, "`", "Symbol", 1, 0, 40, 38, "~", null, 0, 38));
            keys.Add(new KeyItem(0x31, "1", "Number", 1, 1, 40, 38, "!", null, 44, 38));
            keys.Add(new KeyItem(0x32, "2", "Number", 1, 2, 40, 38, "@", null, 88, 38));
            keys.Add(new KeyItem(0x33, "3", "Number", 1, 3, 40, 38, "#", null, 132, 38));
            keys.Add(new KeyItem(0x34, "4", "Number", 1, 4, 40, 38, "$", null, 176, 38));
            keys.Add(new KeyItem(0x35, "5", "Number", 1, 5, 40, 38, "%", null, 220, 38));
            keys.Add(new KeyItem(0x36, "6", "Number", 1, 6, 40, 38, "^", null, 264, 38));
            keys.Add(new KeyItem(0x37, "7", "Number", 1, 7, 40, 38, "&", null, 308, 38));
            keys.Add(new KeyItem(0x38, "8", "Number", 1, 8, 40, 38, "*", null, 352, 38));
            keys.Add(new KeyItem(0x39, "9", "Number", 1, 9, 40, 38, "(", null, 396, 38));
            keys.Add(new KeyItem(0x30, "0", "Number", 1, 10, 40, 38, ")", null, 440, 38));
            keys.Add(new KeyItem(0xBD, "-", "Symbol", 1, 11, 40, 38, "_", null, 484, 38));
            keys.Add(new KeyItem(0xBB, "=", "Symbol", 1, 12, 40, 38, "+", null, 528, 38));
            keys.Add(new KeyItem(0x08, "Backspace", "Control", 1, 13, 76, 38, "", null, 572, 38));
            // Nav 1
            keys.Add(new KeyItem(0x2D, "Ins", "Navigation", 1, 14, 40, 38, "", null, 660, 38));
            keys.Add(new KeyItem(0x24, "Home", "Navigation", 1, 15, 40, 38, "", null, 704, 38));
            keys.Add(new KeyItem(0x21, "PgUp", "Navigation", 1, 16, 40, 38, "", null, 748, 38));
            // Numpad 1
            keys.Add(new KeyItem(0x90, "Num", "Numpad", 1, 17, 40, 38, "", null, 804, 38));
            keys.Add(new KeyItem(0x6F, "/", "Numpad", 1, 18, 40, 38, "", null, 848, 38));
            keys.Add(new KeyItem(0x6A, "*", "Numpad", 1, 19, 40, 38, "", null, 892, 38));
            keys.Add(new KeyItem(0x6D, "-", "Numpad", 1, 20, 40, 38, "", null, 936, 38));

            // ROW 2 - QWERTY Row (Y = 80)
            keys.Add(new KeyItem(0x09, "Tab", "Control", 2, 0, 60, 38, "", null, 0, 80));
            keys.Add(new KeyItem(0x51, "Q", "Alpha", 2, 1, 40, 38, "", null, 64, 80));
            keys.Add(new KeyItem(0x57, "W", "Alpha", 2, 2, 40, 38, "", null, 108, 80));
            keys.Add(new KeyItem(0x45, "E", "Alpha", 2, 3, 40, 38, "", null, 152, 80));
            keys.Add(new KeyItem(0x52, "R", "Alpha", 2, 4, 40, 38, "", null, 196, 80));
            keys.Add(new KeyItem(0x54, "T", "Alpha", 2, 5, 40, 38, "", null, 240, 80));
            keys.Add(new KeyItem(0x59, "Y", "Alpha", 2, 6, 40, 38, "", null, 284, 80));
            keys.Add(new KeyItem(0x55, "U", "Alpha", 2, 7, 40, 38, "", null, 328, 80));
            keys.Add(new KeyItem(0x49, "I", "Alpha", 2, 8, 40, 38, "", null, 372, 80));
            keys.Add(new KeyItem(0x4F, "O", "Alpha", 2, 9, 40, 38, "", null, 416, 80));
            keys.Add(new KeyItem(0x50, "P", "Alpha", 2, 10, 40, 38, "", null, 460, 80));
            keys.Add(new KeyItem(0xDB, "[", "Symbol", 2, 11, 40, 38, "{", null, 504, 80));
            keys.Add(new KeyItem(0xDD, "]", "Symbol", 2, 12, 40, 38, "}", null, 548, 80));
            keys.Add(new KeyItem(0xDC, @"\", "Symbol", 2, 13, 56, 38, "|", null, 592, 80));
            // Nav 2
            keys.Add(new KeyItem(0x2E, "Del", "Navigation", 2, 14, 40, 38, "", null, 660, 80));
            keys.Add(new KeyItem(0x23, "End", "Navigation", 2, 15, 40, 38, "", null, 704, 80));
            keys.Add(new KeyItem(0x22, "PgDn", "Navigation", 2, 16, 40, 38, "", null, 748, 80));
            // Numpad 2
            keys.Add(new KeyItem(0x67, "7", "Numpad", 2, 17, 40, 38, "", null, 804, 80));
            keys.Add(new KeyItem(0x68, "8", "Numpad", 2, 18, 40, 38, "", null, 848, 80));
            keys.Add(new KeyItem(0x69, "9", "Numpad", 2, 19, 40, 38, "", null, 892, 80));
            keys.Add(new KeyItem(0x6B, "+", "Numpad", 2, 20, 40, 80, "", null, 936, 80));

            // ROW 3 - ASDF Row (Y = 122)
            keys.Add(new KeyItem(0x14, "Caps", "Control", 3, 0, 72, 38, "", null, 0, 122));
            keys.Add(new KeyItem(0x41, "A", "Alpha", 3, 1, 40, 38, "", null, 76, 122));
            keys.Add(new KeyItem(0x53, "S", "Alpha", 3, 2, 40, 38, "", null, 120, 122));
            keys.Add(new KeyItem(0x44, "D", "Alpha", 3, 3, 40, 38, "", null, 164, 122));
            keys.Add(new KeyItem(0x46, "F", "Alpha", 3, 4, 40, 38, "", null, 208, 122));
            keys.Add(new KeyItem(0x47, "G", "Alpha", 3, 5, 40, 38, "", null, 252, 122));
            keys.Add(new KeyItem(0x48, "H", "Alpha", 3, 6, 40, 38, "", null, 296, 122));
            keys.Add(new KeyItem(0x4A, "J", "Alpha", 3, 7, 40, 38, "", null, 340, 122));
            keys.Add(new KeyItem(0x4B, "K", "Alpha", 3, 8, 40, 38, "", null, 384, 122));
            keys.Add(new KeyItem(0x4C, "L", "Alpha", 3, 9, 40, 38, "", null, 428, 122));
            keys.Add(new KeyItem(0xBA, ";", "Symbol", 3, 10, 40, 38, ":", null, 472, 122));
            keys.Add(new KeyItem(0xDE, "'", "Symbol", 3, 11, 40, 38, "\"", null, 516, 122));
            keys.Add(new KeyItem(0x0D, "Enter", "Control", 3, 12, 88, 38, "", null, 560, 122));
            // Numpad 3
            keys.Add(new KeyItem(0x64, "4", "Numpad", 3, 17, 40, 38, "", null, 804, 122));
            keys.Add(new KeyItem(0x65, "5", "Numpad", 3, 18, 40, 38, "", null, 848, 122));
            keys.Add(new KeyItem(0x66, "6", "Numpad", 3, 19, 40, 38, "", null, 892, 122));

            // ROW 4 - ZXCV Row (Y = 164)
            keys.Add(new KeyItem(0xA0, "Shift", "Modifier", 4, 0, 94, 38, "L", new int[] { 0x10, 0xA0 }, 0, 164));
            keys.Add(new KeyItem(0x5A, "Z", "Alpha", 4, 1, 40, 38, "", null, 98, 164));
            keys.Add(new KeyItem(0x58, "X", "Alpha", 4, 2, 40, 38, "", null, 142, 164));
            keys.Add(new KeyItem(0x43, "C", "Alpha", 4, 3, 40, 38, "", null, 186, 164));
            keys.Add(new KeyItem(0x56, "V", "Alpha", 4, 4, 40, 38, "", null, 230, 164));
            keys.Add(new KeyItem(0x42, "B", "Alpha", 4, 5, 40, 38, "", null, 274, 164));
            keys.Add(new KeyItem(0x4E, "N", "Alpha", 4, 6, 40, 38, "", null, 318, 164));
            keys.Add(new KeyItem(0x4D, "M", "Alpha", 4, 7, 40, 38, "", null, 362, 164));
            keys.Add(new KeyItem(0xBC, ",", "Symbol", 4, 8, 40, 38, "<", null, 406, 164));
            keys.Add(new KeyItem(0xBE, ".", "Symbol", 4, 9, 40, 38, ">", null, 450, 164));
            keys.Add(new KeyItem(0xBF, "/", "Symbol", 4, 10, 40, 38, "?", null, 494, 164));
            keys.Add(new KeyItem(0xA1, "Shift", "Modifier", 4, 11, 110, 38, "R", new int[] { 0x10, 0xA1 }, 538, 164));
            // Up Arrow
            keys.Add(new KeyItem(0x26, "▲", "Navigation", 4, 15, 40, 38, "Up", null, 704, 164));
            // Numpad 4
            keys.Add(new KeyItem(0x61, "1", "Numpad", 4, 17, 40, 38, "", null, 804, 164));
            keys.Add(new KeyItem(0x62, "2", "Numpad", 4, 18, 40, 38, "", null, 848, 164));
            keys.Add(new KeyItem(0x63, "3", "Numpad", 4, 19, 40, 38, "", null, 892, 164));
            keys.Add(new KeyItem(0x0D, "Enter", "Numpad", 4, 20, 40, 80, "", null, 936, 164));

            // ROW 5 - Bottom Modifiers & Spacebar (Y = 206)
            keys.Add(new KeyItem(0xA2, "Ctrl", "Modifier", 5, 0, 50, 38, "L", new int[] { 0x11, 0xA2 }, 0, 206));
            keys.Add(new KeyItem(0x5B, "Win", "Modifier", 5, 1, 46, 38, "L", new int[] { 0x5B }, 54, 206));
            keys.Add(new KeyItem(0xA4, "Alt", "Modifier", 5, 2, 46, 38, "L", new int[] { 0x12, 0xA4 }, 104, 206));
            keys.Add(new KeyItem(0x20, "Spacebar", "Alpha", 5, 3, 250, 38, "", null, 154, 206));
            keys.Add(new KeyItem(0xA5, "Alt", "Modifier", 5, 4, 46, 38, "R", new int[] { 0x12, 0xA5 }, 408, 206));
            keys.Add(new KeyItem(0x5C, "Win", "Modifier", 5, 5, 46, 38, "R", new int[] { 0x5C }, 458, 206));
            keys.Add(new KeyItem(0x5D, "Menu", "Control", 5, 6, 46, 38, "", null, 508, 206));
            keys.Add(new KeyItem(0xA3, "Ctrl", "Modifier", 5, 7, 86, 38, "R", new int[] { 0x11, 0xA3 }, 558, 206));
            // Arrows Left, Down, Right
            keys.Add(new KeyItem(0x25, "◄", "Navigation", 5, 14, 40, 38, "Left", null, 660, 206));
            keys.Add(new KeyItem(0x28, "▼", "Navigation", 5, 15, 40, 38, "Down", null, 704, 206));
            keys.Add(new KeyItem(0x27, "►", "Navigation", 5, 16, 40, 38, "Right", null, 748, 206));
            // Numpad 5
            keys.Add(new KeyItem(0x60, "0", "Numpad", 5, 17, 84, 38, "", null, 804, 206));
            keys.Add(new KeyItem(0x6E, ".", "Numpad", 5, 19, 40, 38, "", null, 892, 206));

            return keys;
        }
    }
}
