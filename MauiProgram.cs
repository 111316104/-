using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microcharts.Maui;
using PD_app.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;
#if IOS
using PD_app.Platforms.iOS;
#endif
namespace PD_app
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMicrocharts() // 註冊 Microcharts 的 Handler
                .UseMauiCommunityToolkit()  // <-- 再呼叫 Toolkit
                .UseLiveCharts()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
#if IOS
            builder.Services.AddSingleton<ISpeechToTextService, SpeechToTextService>();
#endif

            return builder.Build();
        }
    }
}
