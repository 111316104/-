using PD_app.Services;

namespace PD_app;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        FontManager.ApplyFontSizeToPage(this);
    }

    private void SmallFont_Clicked(object sender, EventArgs e)
    {
        ChangeFontSize(18); // 小
    }

    private void MediumFont_Clicked(object sender, EventArgs e)
    {
        ChangeFontSize(22); // 中
    }

    private void LargeFont_Clicked(object sender, EventArgs e)
    {
        ChangeFontSize(26); // 大
    }


    private void ChangeFontSize(int newSize)
    {
        // 儲存偏好
        Preferences.Set("AppFontSize", newSize);

        // 更新 FontManager
        FontManager.CurrentFontSize = newSize;

        // 遍歷 NavigationStack 找到需要更新的頁面
        foreach (var page in Application.Current.MainPage.Navigation.NavigationStack)
        {
            if (page is ContentPage cp)
            {
                FontManager.ApplyFontSizeToPage(cp);
            }

            // 如果頁面有 ObservableCollection 的 CollectionView (例如 ChatHistoryPage)
            if (page is ChatHistoryPage chatPage)
            {
                chatPage.UpdateFontSize(newSize);
            }
        }
    }

    private async void LogoutButton_Clicked(object sender, EventArgs e)
    {
        Preferences.Set("IsLoggedIn", false);
        Application.Current.MainPage = new NavigationPage(new MainPage());
        await Task.Delay(200);
    }

    private async void backButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
