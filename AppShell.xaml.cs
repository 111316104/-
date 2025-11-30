namespace PD_app;

public partial class AppShell : Shell
{
    // 影片列表：Tuple<影片標題, 影片網址>
    private readonly (string Title, string Url)[] videos = new[]
    {
        ("正確導管出口護理", "https://www.youtube.com/watch?v=kVTRitcohik&t=6s"),
        ("腹膜透析換液步驟", "https://www.youtube.com/watch?v=Z7rw7DwEyPM&t=10s"),
        ("正確洗手步驟", "https://www.youtube.com/watch?v=lqsT826Mv04&t=9s")
        // 之後可以直接新增影片在這裡
    };

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(Talking), typeof(Talking));
        Routing.RegisterRoute(nameof(Knowledge), typeof(Knowledge));
        Routing.RegisterRoute(nameof(Record), typeof(Record));
        Routing.RegisterRoute(nameof(Choose), typeof(Choose));
        Routing.RegisterRoute(nameof(ChartsPage), typeof(ChartsPage));
        Routing.RegisterRoute(nameof(ChatHistoryPage), typeof(ChatHistoryPage));

        LoadVideos();
    }

    private void LoadVideos()
    {
        foreach (var video in videos)
        {
            // 取得影片 ID，生成縮圖 URL
            string videoId = GetYouTubeId(video.Url);
            string thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

            // 縮圖
            var image = new Image
            {
                Source = thumbnailUrl,
                HeightRequest = 100,
                WidthRequest = 150,
                Aspect = Aspect.AspectFill
            };

            // 點擊事件
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                await Launcher.Default.OpenAsync(video.Url);
            };
            image.GestureRecognizers.Add(tap);

            // 標題
            var titleLabel = new Label
            {
                Text = video.Title,
                FontSize = 17,
                VerticalOptions = LayoutOptions.Center
            };

            // 出處
            var sourceLabel = new Label
            {
                Text = "來源：花蓮慈濟醫院",
                FontSize = 12,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Center
            };

            // 標題 + 出處垂直排列
            var titleStack = new StackLayout
            {
                Orientation = StackOrientation.Vertical,
                Children = { titleLabel, sourceLabel }
            };

            // 縮圖 + 標題/出處水平排列
            var horizontalStack = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 10,
                Children = { image, titleStack }
            };

            // 加入影片列表 StackLayout
            VideoStack.Children.Add(horizontalStack);
        }
    }


    // 取得 YouTube 影片 ID
    private string GetYouTubeId(string url)
    {
        var uri = new Uri(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (query.AllKeys.Contains("v"))
            return query["v"]!;
        // fallback
        return uri.Segments.Last();
    }

}
