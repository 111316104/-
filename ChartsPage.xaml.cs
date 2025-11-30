using SkiaSharp;
using PD_app.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

namespace PD_app;

public partial class ChartsPage : ContentPage
{
    private SKTypeface font;

    public ChartsPage()
    {
        InitializeComponent();
        // 在 Record 的建構子或需要時
        FontManager.ApplyFontSizeToPage(this);
        font = SKTypeface.FromFamilyName("NotoSansTC");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadChartData(startDatePicker.Date, endDatePicker.Date);
    }

    private async void OnUpdateChartClicked(object sender, EventArgs e)
    {
        DateTime start = startDatePicker.Date;
        DateTime end = endDatePicker.Date;

        if (start > end)
        {
            await DisplayAlert("錯誤", "開始日期不能晚於結束日期", "確定");
            return;
        }

        await LoadChartData(start, end);
    }

    private async Task LoadChartData(DateTime startDate, DateTime endDate)
    {
        var records = await DatabaseService.GetRecordsInRangeAsync(startDate, endDate);
        records = records
            .Where(r => r.Systolic.HasValue && r.Diastolic.HasValue && r.Weight.HasValue)
            .Where(r => r.Systolic.Value > 0 && r.Diastolic.Value > 0 && r.Weight.Value > 0)
            .ToList();

        // ========== 體重圖 ==========
        var morningRecords = records
            .Where(r => r.Session == "早")
            .GroupBy(r => r.Date.Date)
            .Select(g => g.First())
            .OrderBy(r => r.Date)
            .ToList();

        var dates = morningRecords.Select(r => r.Date.ToString("MM/dd")).ToArray();
        var weightValues = morningRecords.Select(r => (double)(r.Weight ?? 0)).ToList();

        weightChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = weightValues,
                Name = "體重",
                Stroke = new SolidColorPaint(SKColors.Green, 2),
                GeometryStroke = new SolidColorPaint(SKColors.Green, 2)
            }
        };
        weightChart.XAxes = new[]
        {
            new Axis
            {
                Labels = dates,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font }
            }
        };
        weightChart.YAxes = new[]
        {
            new Axis
            {
                Name = "體重 (kg)",
                NamePaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font },
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font }
            }
        };
        weightChart.LegendTextPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font };

        // ========== 血壓圖 ==========
        var systolicValues = morningRecords.Select(r => (double)(r.Systolic ?? 0)).ToList();
        var diastolicValues = morningRecords.Select(r => (double)(r.Diastolic ?? 0)).ToList();

        bloodPressureChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = systolicValues,
                Name = "收縮壓",
                Stroke = new SolidColorPaint(SKColors.Red, 2),
                GeometryStroke = new SolidColorPaint(SKColors.Red, 2)
            },
            new LineSeries<double>
            {
                Values = diastolicValues,
                Name = "舒張壓",
                Stroke = new SolidColorPaint(SKColors.Blue, 2),
                GeometryStroke = new SolidColorPaint(SKColors.Blue, 2)
            }
        };
        bloodPressureChart.XAxes = new[]
        {
            new Axis
            {
                Labels = dates,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font }
            }
        };
        bloodPressureChart.YAxes = new[]
        {
            new Axis
            {
                Name = "血壓 (mmHg)",
                NamePaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font },
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font }
            }
        };
        bloodPressureChart.LegendTextPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font };

        // ========== 每日總脫水量圖 ==========
        var dailyTotals = await DatabaseService.GetDailyTotalVolumesAsync();
        dailyTotals = dailyTotals
            .Where(x => x.Date >= startDate && x.Date <= endDate)
            .ToList();

        var volumeDates = dailyTotals.Select(x => x.Date.ToString("MM/dd")).ToArray();
        var volumeValues = dailyTotals.Select(x => (double)x.TotalVolume).ToList();

        volumeChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = volumeValues,
                Name = "每日總脫水量",
                Stroke = new SolidColorPaint(SKColors.Purple, 2),
                GeometryStroke = new SolidColorPaint(SKColors.Purple, 2)
            }
        };
        volumeChart.XAxes = new[]
        {
            new Axis
            {
                Labels = volumeDates,
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font }
            }
        };
        volumeChart.YAxes = new[]
        {
            new Axis
            {
                Name = "脫水量 (ml)",
                NamePaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font },
                LabelsPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font }
            }
        };
        volumeChart.LegendTextPaint = new SolidColorPaint(SKColors.Black) { SKTypeface = font };
    }

    private void ChartPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        weightChart.IsVisible = false;
        bloodPressureChart.IsVisible = false;
        volumeChart.IsVisible = false;

        switch (chartPicker.SelectedIndex)
        {
            case 0: weightChart.IsVisible = true; break;
            case 1: bloodPressureChart.IsVisible = true; break;
            case 2: volumeChart.IsVisible = true; break;
        }
    }

    private async void QuickRangeButton_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string param)
        {
            DateTime today = DateTime.Today;

            switch (param)
            {
                case "today":
                    startDatePicker.Date = today;
                    endDatePicker.Date = today;
                    break;
                case "week":
                    startDatePicker.Date = today.AddDays(-6);
                    endDatePicker.Date = today;
                    break;
                case "month":
                    startDatePicker.Date = today.AddDays(-29);
                    endDatePicker.Date = today;
                    break;
            }

            await LoadChartData(startDatePicker.Date, endDatePicker.Date);
        }
    }

    private async void backButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
