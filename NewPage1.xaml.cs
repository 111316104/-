namespace PD_app;
using System;
using PD_app.Models;

public partial class NewPage1 : ContentPage
{
    private readonly UserDatabase _userDatabase = new UserDatabase();
    public NewPage1()
	{
		InitializeComponent();
        // 在 Record 的建構子或需要時
        FontManager.ApplyFontSizeToPage(this);
    }
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        
        DisplayAlert("登入", "返回登入頁面", "OK");
        Application.Current.MainPage = new NavigationPage(new MainPage());
    }


    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string idNumber = usernameEntry.Text;
        string birthdate = passwordEntry.Text;

        if (string.IsNullOrWhiteSpace(idNumber) || string.IsNullOrWhiteSpace(birthdate))
        {
            await DisplayAlert("錯誤", "請輸入身分證和生日", "OK");
            return;
        }

        if (!IsValidIdNumber(idNumber))
        {
            await DisplayAlert("錯誤", "身分證格式錯誤", "OK");
            return;
        }

        if (!IsValidBirthdate(birthdate))
        {
            await DisplayAlert("錯誤", "生日格式錯誤 (YYYYMMDD)", "OK");
            return;
        }

        var existingUser = await _userDatabase.GetUserAsync(idNumber);
        if (existingUser != null)
        {
            await DisplayAlert("錯誤", "此帳號已存在", "OK");
            return;
        }

        var newUser = new User
        {
            Id = idNumber,
            password = birthdate
        };

        int result = await _userDatabase.AddUserAsync(newUser);
        if (result > 0)
        {
            await DisplayAlert("成功", "註冊成功", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("錯誤", "註冊失敗", "OK");
        }
    }
    private bool IsValidIdNumber(string id) => id.Length == 10;

    private bool IsValidBirthdate(string birth)
    {
        if (birth.Length != 8) return false;
        return DateTime.TryParseExact(birth, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _);
    }
}