using Microsoft.Maui.Controls;

namespace PD_app;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void SmallFont_Clicked(object sender, EventArgs e)
    {
        SetFontSize(14);  // 小字體
    }

    private void MediumFont_Clicked(object sender, EventArgs e)
    {
        SetFontSize(18);  // 中字體
    }

    private void LargeFont_Clicked(object sender, EventArgs e)
    {
        SetFontSize(22);  // 大字體
    }

    private void SetFontSize(int size)
    {
        Preferences.Set("AppFontSize", size);
        MessagingCenter.Send(this, "FontSizeChanged", size);
    }

    private async void LogoutButton_Clicked(object sender, EventArgs e)
    {

    }
    private async void backButton_Clicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new Choose());
    }
}