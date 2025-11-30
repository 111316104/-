using System;
using System.Collections.Generic;
using System.Linq;
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
            await InitAsync();
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

        public static async Task<List<DehydrationRecord>> GetAllRecordsAsync()
        {
            await InitAsync();
            return await database.Table<DehydrationRecord>().ToListAsync();
        }

        public static async Task<List<DehydrationRecord>> GetRecordsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            await InitAsync();
            return await database.Table<DehydrationRecord>()
                                 .Where(r => r.Date >= startDate && r.Date <= endDate)
                                 .OrderBy(r => r.Date)
                                 .ToListAsync();
        }

        // ✅ 新增：取得每日總脫水量 (供圖表用)
        public static async Task<List<(DateTime Date, int TotalVolume)>> GetDailyTotalVolumesAsync()
        {
            await InitAsync();
            var records = await database.Table<DehydrationRecord>().ToListAsync();

            // Group by 日期，加總 Volume
            var grouped = records
                .GroupBy(r => r.Date.Date)
                .Select(g => (Date: g.Key, TotalVolume: g.Sum(r => r.Volume)))
                .OrderBy(g => g.Date)
                .ToList();

            return grouped;
        }
    }
}
