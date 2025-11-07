namespace PD_app;

public partial class Knowledge : ContentPage
{
	public Knowledge()
	{
		InitializeComponent();

        // 1. 讀取已儲存的字體大小並套用
        int fontSize = Preferences.Get("AppFontSize", 18);
        ApplyFontSize(fontSize);

        // 2. 訂閱字體大小變化事件
        MessagingCenter.Subscribe<ContentPage, int>(this, "FontSizeChanged", (sender, size) =>
        {
            ApplyFontSize(size);
        });
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

    private void ApplyFontSize(int size)
    {
        // 根據你 Knowledge 頁面上的元件設定字體大小
        // 例如你有標題 Label 或多個 Button：
        // titleLabel.FontSize = size;
        // 如果按鈕是透過 XAML 設定好名稱：
        // categoryButton1.FontSize = size;
        // categoryButton2.FontSize = size;

        // 如果你沒有個別命名按鈕，而是用 XAML 樣式自動產生，也可以在 CategoryButton_Clicked 中取得 sender 為 Button 時改大小，
        // 但建議是直接在此針對主要 Label/Title 做設定即可。

        // 範例：
        // 如果 Knowledge.xaml 有一個名稱為 knowledgeTitle 的 Label：
        // knowledgeTitle.FontSize = size;
    }

    private async void backButton_Clicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new Choose());
    }
}