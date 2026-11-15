using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LaptopInputControl.Models
{
    public class KeyItem : INotifyPropertyChanged
    {
        private bool _isBlocked;

        public int VkCode { get; set; }
        public int[] RelatedVkCodes { get; set; }
        public string Label { get; set; }
        public string SubLabel { get; set; }
        public string Category { get; set; }
        public double KeyWidth { get; set; }
        public double KeyHeight { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public bool IsBlocked
        {
            get { return _isBlocked; }
            set
            {
                if (_isBlocked != value)
                {
                    _isBlocked = value;
                    OnPropertyChanged("IsBlocked");
                    OnPropertyChanged("StatusText");
                    OnPropertyChanged("StatusColor");
                    OnPropertyChanged("KeyBackground");
                    OnPropertyChanged("KeyBorder");
                    OnPropertyChanged("KeyTextColor");
                }
            }
        }

        public string StatusText
        {
            get { return _isBlocked ? "DISABLED" : "ENABLED"; }
        }

        public string StatusColor
        {
            get { return _isBlocked ? "#EF4444" : "#10B981"; }
        }

        public string KeyTextColor
        {
            get { return _isBlocked ? "#FCA5A5" : "#E2E8F0"; }
        }

        public string KeyBackground
        {
            get { return _isBlocked ? "#381216" : "#1A1C28"; }
        }

        public string KeyBorder
        {
            get { return _isBlocked ? "#EF4444" : "#32384C"; }
        }

        public string HexCode
        {
            get { return "0x" + VkCode.ToString("X2"); }
        }

        public KeyItem()
        {
            KeyWidth = 44;
            KeyHeight = 44;
            RelatedVkCodes = new int[0];
        }

        public KeyItem(int vk, string label, string category, int row = 0, int col = 0, double width = 44, double height = 44, string subLabel = "", int[] related = null, double x = 0, double y = 0)
        {
            VkCode = vk;
            Label = label;
            Category = category;
            Row = row;
            Col = col;
            KeyWidth = width;
            KeyHeight = height;
            SubLabel = subLabel;
            RelatedVkCodes = related ?? new int[] { vk };
            X = x;
            Y = y;
            _isBlocked = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
