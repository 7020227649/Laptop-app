using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LaptopInputControl.Models
{
    public class MouseActionItem : INotifyPropertyChanged
    {
        private bool _isBlocked;

        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconKind { get; set; }

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
                }
            }
        }

        public string StatusText
        {
            get { return _isBlocked ? "BLOCKED" : "ACTIVE"; }
        }

        public string StatusColor
        {
            get { return _isBlocked ? "#FF5555" : "#50FA7B"; }
        }

        public MouseActionItem(string id, string title, string description, string iconKind = "")
        {
            Id = id;
            Title = title;
            Description = description;
            IconKind = iconKind;
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
