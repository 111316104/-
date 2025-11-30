using System;
using System.ComponentModel;
using SQLite;
using Microsoft.Maui.Graphics;

namespace PD_app
{
    public class ChatMessage : INotifyPropertyChanged
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Sender { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        private double fontSize;
        [Ignore]
        public double FontSize
        {
            get => fontSize;
            set
            {
                if (fontSize != value)
                {
                    fontSize = value;
                    OnPropertyChanged(nameof(FontSize));
                }
            }
        }

        private Color senderColor;
        [Ignore]
        public Color SenderColor
        {
            get => senderColor;
            set
            {
                if (senderColor != value)
                {
                    senderColor = value;
                    OnPropertyChanged(nameof(SenderColor));
                }
            }
        }

        [Ignore]
        public string TimestampString => Timestamp.ToString("yyyy/MM/dd HH:mm");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
