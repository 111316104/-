using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PD_app
{
    public partial class ChatHistoryPage : ContentPage
    {
        private ChatDatabase _chatDb;
        private ObservableCollection<ChatMessage> _messages;

        public ChatHistoryPage()
        {
            InitializeComponent();

            // 設定頁面字體
            FontManager.ApplyFontSizeToPage(this);

            var dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "chat.db3");
            _chatDb = new ChatDatabase(dbPath);

            _messages = new ObservableCollection<ChatMessage>();
            HistoryCollection.ItemsSource = _messages;

            LoadHistory();
        }

        private async void LoadHistory()
        {
            var allMessages = await _chatDb.GetAllMessagesAsync();

            // 最近 2 天
            var recentMessages = allMessages
                .Where(m => m.Timestamp >= DateTime.Now.AddDays(-2))
                .OrderBy(m => m.Timestamp)
                .ToList();

            // 設定顏色與字型
            var fontSize = Preferences.Get("AppFontSize", 22);
            foreach (var msg in recentMessages)
            {
                msg.FontSize = fontSize;
                msg.SenderColor = msg.Sender == "AI" ? Colors.LightBlue : Colors.LightGray;
                _messages.Add(msg);
            }
        }

        public void UpdateFontSize(int newSize)
        {
            Preferences.Set("AppFontSize", newSize);
            foreach (var msg in _messages)
            {
                msg.FontSize = newSize; // CollectionView 會自動刷新
            }

            // 也更新頁面上其他 Label 或 Button
            FontManager.ApplyFontSizeToPage(this);
        }

    }
}
