using SQLite;
using System;
using System.Threading.Tasks;
using PD_app.Models;


namespace PD_app
{
    public class UserDatabase
    {
        private readonly SQLiteAsyncConnection _database;

        public UserDatabase()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "User.db");
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<User>().Wait();
        }

        public Task<User> GetUserAsync(string id) =>
            _database.Table<User>().Where(u => u.Id == id).FirstOrDefaultAsync();

        public Task<int> AddUserAsync(User user) =>
            _database.InsertAsync(user);
    }
}
