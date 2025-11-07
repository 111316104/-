using SQLite;
using System;
using System.Threading.Tasks;
using PD_app.Models;

namespace PD_app
{
    public partial class MainPage : ContentPage
    {
        private SQLiteAsyncConnection _connection;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string username = usernameEntry.Text?.Trim();
            string password = passwordEntry.Text?.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("提示", "請輸入帳號與密碼", "OK");
                return;
            }

            bool isValid = await CheckUserCredentialsAsync(username, password);

            if (isValid)
            {
                Preferences.Set("IsLoggedIn", true); // ✅ 記住登入狀態
                Preferences.Set("CurrentUser", username); // 可選：記住當前帳號

                await DisplayAlert("成功", "登入成功", "OK");
                Application.Current.MainPage = new NavigationPage(new Choose());
            }
            else
            {
                await DisplayAlert("錯誤", "帳號或密碼錯誤", "OK");
            }
        }

        private void OnRegisterClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new NavigationPage(new NewPage1());
        }

        private async Task<bool> CheckUserCredentialsAsync(string username, string password)
        {
            try
            {
                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "User.db");
                _connection = new SQLiteAsyncConnection(dbPath);

                // 確保 User 表已建立
                await _connection.CreateTableAsync<User>();

                // 查詢使用者帳密
                var count = await _connection.Table<User>()
                    .Where(u => u.Id == username && u.password == password)
                    .CountAsync();

                return count > 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("錯誤", $"登入過程中發生錯誤：{ex.Message}", "OK");
                return false;
            }
        }
    }
}
