using CommunityToolkit.Maui.Views;

namespace PD_app;

public partial class EducationPage : ContentPage
{
    private CancellationTokenSource _cts;

    public EducationPage(string title, string[] faqItems)
    {
        InitializeComponent();
        Title = title;
        BackgroundColor = Color.FromArgb("#ccd5ae");

        foreach (string item in faqItems)
        {
            // 取出編號（假設字串前面有 "1. XXX:" 這種格式）
            string header = item.Split('\n')[0]; // 第一行當作標題
            string content = item.Substring(header.Length).Trim(); // 其餘當內容

            // 喇叭按鈕
            var speakButton = new ImageButton
            {
                Source = "speaker.png",
                WidthRequest = 28,
                HeightRequest = 28,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center
            };

            speakButton.Clicked += async (s, e) =>
            {
                try
                {
                    _cts?.Cancel();
                    _cts = new CancellationTokenSource();

                    var locales = await TextToSpeech.GetLocalesAsync();
                    var locale = locales.FirstOrDefault(l => l.Language.StartsWith("zh")); // 中文

                    var options = new SpeechOptions
                    {
                        Pitch = 1.05f,
                        Volume = 1.0f,
                        Locale = locale
                    };

                    await TextToSpeech.SpeakAsync(item, options, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 使用者取消朗讀
                }
            };

            // 標題文字
            var headerLabel = new Label
            {
                Text = header,
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                TextColor = Colors.DarkOliveGreen,
                VerticalOptions = LayoutOptions.Center
            };

            // 下拉箭頭
            var arrowLabel = new Label
            {
                Text = "▼",
                FontSize = 18,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            };

            // Header = 喇叭 + 標題 + 箭頭
            var headerLayout = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 30 },           // 喇叭
                    new ColumnDefinition { Width = GridLength.Star }, // 標題
                    new ColumnDefinition { Width = 30 }            // 箭頭
                }
            };
            headerLayout.Add(speakButton, 0, 0);
            headerLayout.Add(headerLabel, 1, 0);
            headerLayout.Add(arrowLabel, 2, 0);

            // FAQ 內容
            var textLabel = new Label
            {
                Text = content,
                FontSize = 18,
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var contentFrame = new Frame
            {
                Content = textLabel,
                BackgroundColor = Color.FromArgb("#fdfdfd"),
                BorderColor = Color.FromArgb("#A18276"),
                CornerRadius = 12,
                Padding = new Thickness(12),
                Margin = new Thickness(0, 6, 0, 0)
            };

            var expander = new Expander
            {
                Header = headerLayout,
                Content = contentFrame
            };

            // 外框美化
            var outerFrame = new Frame
            {
                Content = expander,
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#A18276"),
                CornerRadius = 14,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 8),
                Shadow = new Shadow
                {
                    Brush = Brush.Black,
                    Offset = new Point(0, 2),
                    Opacity = 0.12f,
                    Radius = 6
                }
            };

            // 箭頭更新
            expander.ExpandedChanged += (s, e) =>
            {
                arrowLabel.Text = expander.IsExpanded ? "▲" : "▼";
            };

            faqStack.Children.Add(outerFrame);
        }

        // 字體大小初始化
        int fontSize = Preferences.Get("AppFontSize", 18);
        ApplyFontSize(fontSize);

        // 訂閱字體大小變化事件
        MessagingCenter.Subscribe<ContentPage, int>(this, "FontSizeChanged", (sender, size) =>
        {
            ApplyFontSize(size);
        });
    }

    private void ApplyFontSize(int size)
    {
        foreach (var child in faqStack.Children)
        {
            if (child is Frame outer && outer.Content is Expander expander &&
                expander.Content is Frame frame && frame.Content is Label label)
            {
                label.FontSize = size;
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
    }
}
