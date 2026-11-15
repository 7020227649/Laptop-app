using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LaptopInputControl.Models
{
    public class KeyRemapItem : INotifyPropertyChanged
    {
        public int SourceVk { get; set; }
        public string SourceName { get; set; }
        public int TargetVk { get; set; }
        public string TargetName { get; set; }
        public string Description { get; set; }

        public string MappingArrow
        {
            get { return "➔"; }
        }

        public string DisplayText
        {
            get { return string.Format("{0}  ➔  {1}", SourceName, TargetName); }
        }

        public KeyRemapItem()
        {
            Description = "Custom Remap";
        }

        public KeyRemapItem(int sourceVk, string sourceName, int targetVk, string targetName, string description = "")
        {
            SourceVk = sourceVk;
            SourceName = sourceName;
            TargetVk = targetVk;
            TargetName = targetName;
            Description = string.IsNullOrEmpty(description) ? "Custom Remap" : description;
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
