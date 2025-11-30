using PD_app.Models;
using PD_app.Services;
using System.Globalization;

namespace PD_app;

public partial class Record : ContentPage
{
    List<DehydrationRecord> todayRecords = new();

    public Record() : this(DateTime.Today)
    {
    }

    public Record(DateTime? date = null)
    {
        InitializeComponent();

        FontManager.ApplyFontSizeToPage(this);

        sessionPicker.ItemsSource = new List<string> { "早", "午", "晚", "睡前" };
        datePicker.Date = date ?? DateTime.Today;

        datePicker.DateSelected += DatePicker_DateSelected;
        sessionPicker.SelectedIndexChanged += SessionPicker_SelectedIndexChanged;

        Init();
    }

    private void UnlockInputs()
    {
        fillEntry.IsEnabled = true;
        drainEntry.IsEnabled = true;
        weightEntry.IsEnabled = true;
        systolicEntry.IsEnabled = true;
        diastolicEntry.IsEnabled = true;
    }

    private void SessionPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        UnlockInputs();

        string selected = sessionPicker.SelectedItem?.ToString();

        bool isMorning = selected == "早";

        weightLabel.IsVisible = isMorning;
        weightEntry.IsVisible = isMorning;

        bpLabel.IsVisible = isMorning;
        sdLabel.IsVisible = isMorning;
        systolicEntry.IsVisible = isMorning;
        diastolicEntry.IsVisible = isMorning;
    }

    private async void Init()
    {
        await DatabaseService.InitAsync();
        await LoadTodayRecords();
    }

    private async Task LoadTodayRecords()
    {
        DateTime date = datePicker.Date;
        var sessions = new[] { "早", "午", "晚", "睡前" };

        todayRecords.Clear();

        foreach (var s in sessions)
        {
            var r = await DatabaseService.GetRecordByDateAndSessionAsync(date, s);
            if (r != null) todayRecords.Add(r);
        }

        recordsListView.ItemsSource = todayRecords;
        int total = todayRecords.Sum(r => r.Volume);
        todayTotalLabel.Text = $"今日總脫水量：{total} ml";
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        bool ok = true;

        if (!int.TryParse(fillEntry.Text, out int fill) || fill <= 0 || fill >= 5000)
        { fillError.IsVisible = true; ok = false; }
        else fillError.IsVisible = false;

        if (!int.TryParse(drainEntry.Text, out int drain) || drain < 0 || drain >= 5000)
        { drainError.IsVisible = true; ok = false; }
        else drainError.IsVisible = false;

        if (sessionPicker.SelectedItem == null)
        {
            await DisplayAlert("錯誤", "請選擇時段", "確定");
            return;
        }

        string session = sessionPicker.SelectedItem.ToString();

        float? weight = null;
        int? sys = null;
        int? dia = null;

        if (session == "早")
        {
            if (!float.TryParse(weightEntry.Text, out float w) || w <= 0)
            { weightError.IsVisible = true; ok = false; }
            else { weightError.IsVisible = false; weight = w; }

            if (!int.TryParse(systolicEntry.Text, out int s1) || s1 < 50 || s1 > 250)
            { systolicError.IsVisible = true; ok = false; }
            else { systolicError.IsVisible = false; sys = s1; }

            if (!int.TryParse(diastolicEntry.Text, out int d1) || d1 < 30 || d1 > 150)
            { diastolicError.IsVisible = true; ok = false; }
            else { diastolicError.IsVisible = false; dia = d1; }
        }

        if (!ok)
        {
            await DisplayAlert("錯誤", "請確認所有輸入欄位", "確定");
            return;
        }

        var date = datePicker.Date;
        var existing = await DatabaseService.GetRecordByDateAndSessionAsync(date, session);

        if (existing != null)
        {
            existing.FillVolume = fill;
            existing.DrainVolume = drain;
            existing.Weight = weight;
            existing.Systolic = sys;
            existing.Diastolic = dia;
            existing.Volume = drain - fill;

            await DatabaseService.UpdateRecordAsync(existing);
        }
        else
        {
            var r = new DehydrationRecord
            {
                Date = date,
                Session = session,
                FillVolume = fill,
                DrainVolume = drain,
                Weight = weight,
                Systolic = sys,
                Diastolic = dia,
                Volume = drain - fill
            };
            await DatabaseService.InsertRecordAsync(r);
        }

        await LoadTodayRecords();

        fillEntry.IsEnabled = false;
        drainEntry.IsEnabled = false;
        if (session == "早")
        {
            weightEntry.IsEnabled = false;
            systolicEntry.IsEnabled = false;
            diastolicEntry.IsEnabled = false;
        }
    }

    public class EqualsToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            return value.ToString() == (string)parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    private async void DatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        UnlockInputs();
        await LoadTodayRecords();
    }

    private async void backButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void ChartClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ChartsPage));
    }
}
