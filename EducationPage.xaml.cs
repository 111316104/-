using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using System.Threading;

namespace PD_app;

public partial class EducationPage : ContentPage
{
    private CancellationTokenSource _ttsCts;
    public EducationPage(string title, string[] faqItems)
    {
        InitializeComponent();
        FontManager.ApplyFontSizeToPage(this);
        Title = title;
        BackgroundColor = Color.FromArgb("#ccd5ae");

        foreach (var item in faqItems)
        {
            faqStack.Children.Add(CreateFaqFrame(item));
        }

        AddStyledButton("返回上一頁", backButton_Clicked);
    }

    private Frame CreateFaqFrame(string item)
    {
        // 分割標題與內容
        string header = item.Split('\n')[0];
        string content = item.Substring(header.Length).Trim();

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
                _ttsCts?.Cancel();
                _ttsCts = new CancellationTokenSource();

                var locales = await TextToSpeech.GetLocalesAsync();
                var locale = locales.FirstOrDefault(l => l.Language.StartsWith("zh"));

                var options = new SpeechOptions
                {
                    Pitch = 1.0f,
                    Volume = 1.0f,
                    Locale = locale,
                };

                // 只朗讀內容
                await TextToSpeech.SpeakAsync(content, options, _ttsCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 使用者取消朗讀
            }
        };

        // 標題 Label
        var headerLabel = new Label
        {
            Text = header,
            FontAttributes = FontAttributes.Bold,
            FontSize = 20,
            TextColor = Colors.DarkOliveGreen,
            VerticalOptions = LayoutOptions.Center
        };

        // 箭頭 Label
        var arrowLabel = new Label
        {
            Text = "▼",
            FontSize = 18,
            TextColor = Colors.Gray,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };

        // Header Layout
        var headerLayout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = 30 },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = 30 }
            }
        };
        headerLayout.Add(speakButton, 0, 0);
        headerLayout.Add(headerLabel, 1, 0);
        headerLayout.Add(arrowLabel, 2, 0);

        // 內容 Label
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

        // 箭頭旋轉動畫
        expander.ExpandedChanged += (s, e) =>
        {
            arrowLabel.Text = expander.IsExpanded ? "▲" : "▼";
            arrowLabel.RotateTo(expander.IsExpanded ? 180 : 0, 150);
        };

        // 外框
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

        return outerFrame;
    }

    private async void backButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void AddStyledButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkOliveGreen,
            BackgroundColor = Color.FromArgb("#fdfdfd"),
            CornerRadius = 12,
            Padding = new Thickness(15),
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalOptions = LayoutOptions.FillAndExpand
        };

        button.Clicked += handler;

        var frame = new Frame
        {
            Content = button,
            CornerRadius = 16,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 5),
            HasShadow = true,
            BackgroundColor = Colors.Transparent,
            BorderColor = Color.FromArgb("#A18276")
        };

        faqStack.Children.Add(frame);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ttsCts?.Cancel();
    }
}
