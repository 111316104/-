namespace PD_app;

public partial class Knowledge : ContentPage
{
	public Knowledge()
	{
		InitializeComponent();

        FontManager.ApplyFontSizeToPage(this);

    }
    private async void CategoryButton_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            string categoryName = btn.Text;

            // 跳轉到新頁面並帶分類名稱
            await Navigation.PushAsync(new CategoryPage(categoryName));
        }
    }

    private async void backButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}