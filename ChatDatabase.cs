using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PD_app
{
    public class ChatDatabase
    {
        private readonly SQLiteAsyncConnection _database;

        public ChatDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<ChatMessage>().Wait();
        }

        public Task<List<ChatMessage>> GetAllMessagesAsync()
        {
            return _database.Table<ChatMessage>().OrderBy(m => m.Timestamp).ToListAsync();
        }

        public Task<int> SaveMessageAsync(ChatMessage message)
        {
            return _database.InsertAsync(message);
        }
    }
}
