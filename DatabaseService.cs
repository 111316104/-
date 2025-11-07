using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using PD_app.Models;

namespace PD_app.Services
{
    public static class DatabaseService
    {
        static SQLiteAsyncConnection database;

        public static async Task InitAsync()
        {
            if (database != null)
                return;

            var path = Path.Combine(FileSystem.AppDataDirectory, "records.db");
            database = new SQLiteAsyncConnection(path);
            await database.CreateTableAsync<DehydrationRecord>();
        }

        public static async Task<DehydrationRecord> GetRecordByDateAndSessionAsync(DateTime date, string session)
        {
            var allRecords = await database.Table<DehydrationRecord>().Where(r => r.Session == session).ToListAsync();
            return allRecords.FirstOrDefault(r => r.Date.Date == date.Date);
        }


        public static Task<int> InsertRecordAsync(DehydrationRecord record)
        {
            return database.InsertAsync(record);
        }

        public static Task<int> UpdateRecordAsync(DehydrationRecord record)
        {
            return database.UpdateAsync(record);
        }

    }
}

