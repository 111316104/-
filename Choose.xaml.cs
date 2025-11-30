namespace PD_app;

public partial class Choose : ContentPage
{
    private List<string> imageFiles { get; set; } = new();
    private int currentImageIndex = 0;
    private IDispatcherTimer timer;
    public Choose()
    {
        InitializeComponent();
        FontManager.ApplyFontSizeToPage(this);
        LoadAndPlayAnimation();


        //for (int i = 0; i < 234; i++)
        //{
        //    string file = $"frame_{i:D5}.png";
        //    //string file = $"PD_app/Resources/Image/bgimg/frame_{i:D5}.png";

        //    imageFiles.Add(file);
        //}

        //Console.WriteLine($"圖片載入總數：{imageFiles.Count}");


        //if (imageFiles.Count > 0)
        //{
        //    ImageDisplay.Source = imageFiles[currentImageIndex];

        //    timer = Dispatcher.CreateTimer();
        //    timer.Interval = TimeSpan.FromMilliseconds(10); // 切換速度可調
        //    timer.Tick += (s, e) =>
        //    {
        //        currentImageIndex = (currentImageIndex + 1) % imageFiles.Count;
        //        ImageDisplay.Source = imageFiles[currentImageIndex];
        //    };
        //    timer.Start();
        //}

    }
    private void LoadAndPlayAnimation()
    {
        // 1️ 載入 bgimg 資料夾的所有圖片
        for (int i = 0; i < 234; i++)
        {
            string file = $"bgimg/frame_{i:D5}.png";
            imageFiles.Add(file);
        }

        if (imageFiles.Count == 0)
            return;

        // 2️ 顯示第一張
        ImageDisplay.Source = ImageSource.FromFile(imageFiles[currentImageIndex]);

        // 3️ 啟動動畫
        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(33); // 大約30fps
        timer.Tick += (s, e) =>
        {
            currentImageIndex = (currentImageIndex + 1) % imageFiles.Count;
            ImageDisplay.Source = ImageSource.FromFile(imageFiles[currentImageIndex]);
        };
        timer.Start();
    }
    private async void OnTopic1Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Talking));
    }
    private async void OnTopic2Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Knowledge));
    }
    private async void OnTopic3Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Record));
    }
    private async void OnTopic4Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}