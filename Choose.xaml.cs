namespace PD_app;

public partial class Choose : ContentPage
{
    private List<string> imageFiles { get; set; } = new();
    private int currentImageIndex = 0;
    private IDispatcherTimer timer;
    public Choose()
    {
        InitializeComponent();


        for (int i = 0; i < 234; i++)
        {
            string file = $"bgimg/frame_{i:D5}.png";

            imageFiles.Add(file);
        }

        Console.WriteLine($"圖片載入總數：{imageFiles.Count}");


        if (imageFiles.Count > 0)
        {
            ImageDisplay.Source = imageFiles[currentImageIndex];

            timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10); // 切換速度可調
            timer.Tick += (s, e) =>
            {
                currentImageIndex = (currentImageIndex + 1) % imageFiles.Count;
                ImageDisplay.Source = imageFiles[currentImageIndex];
            };
            timer.Start();
        }

    }
    private void OnTopic1Clicked(object sender, EventArgs e) {

    }
    private void OnTopic2Clicked(object sender, EventArgs e) {

        Application.Current.MainPage = new NavigationPage(new Knowledge());
    }
    private void OnTopic3Clicked(object sender, EventArgs e) {

        Application.Current.MainPage = new NavigationPage(new Record());
    }
    private void OnTopic4Clicked(object sender, EventArgs e) {

        Application.Current.MainPage = new NavigationPage(new SettingsPage());
    }
}