using static System.Runtime.InteropServices.JavaScript.JSType;
using PD_app.Models;
using PD_app.Services;

namespace PD_app;

public partial class Record : ContentPage
{
    List<DehydrationRecord> todayRecords = new();

    public Record() : this(DateTime.Today) // 如果不傳遞參數，則預設使用今天的日期
    {
    }
    public Record(DateTime? date = null)
	{
		InitializeComponent();
        sessionPicker.ItemsSource = new List<string> { "早", "午", "晚", "睡前" };
        datePicker.Date = date ?? DateTime.Today;
        datePicker.DateSelected += DatePicker_DateSelected;

        // 1. 讀取並套用字體大小
        int fontSize = Preferences.Get("AppFontSize", 18);
        ApplyFontSize(fontSize);

        // 2. 訂閱字體大小變化
        MessagingCenter.Subscribe<ContentPage, int>(this, "FontSizeChanged", (sender, size) =>
        {
            ApplyFontSize(size);
        });

        Init();
    }
    private async void Init()
    {
        await DatabaseService.InitAsync();
        await LoadTodayRecords();
    }

    private async Task LoadTodayRecords()
    {
        DateTime today = datePicker.Date;
        var sessions = new[] { "早", "午", "晚", "睡前" };
        todayRecords.Clear();

        foreach (var session in sessions)
        {
            var record = await DatabaseService.GetRecordByDateAndSessionAsync(today, session);
            if (record != null)
            {
                todayRecords.Add(record);
            }
        }


        recordsListView.ItemsSource = null;
        recordsListView.ItemsSource = todayRecords;

        int totalToday = todayRecords.Sum(r => r.Volume);
        todayTotalLabel.Text = $"今日總脫水量：{totalToday} ml";
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (sessionPicker.SelectedItem == null ||
            !int.TryParse(fillEntry.Text, out int fill) ||
            !int.TryParse(drainEntry.Text, out int drain))
        {
            await DisplayAlert("錯誤", "請確認所有欄位皆已填寫", "確定");
            return;
        }

        var selectedDate = datePicker.Date;
        string session = sessionPicker.SelectedItem.ToString();

        var existing = await DatabaseService.GetRecordByDateAndSessionAsync(selectedDate, session);

        if (existing != null)
        {
            existing.FillVolume = fill;
            existing.DrainVolume = drain;
            await DatabaseService.UpdateRecordAsync(existing);
        }
        else
        {
            var newRecord = new DehydrationRecord
            {
                Date = selectedDate,
                Session = session,
                FillVolume = fill,
                DrainVolume = drain
            };
            await DatabaseService.InsertRecordAsync(newRecord);
        }

        await LoadTodayRecords();
    }

    private async void DatePicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        await LoadTodayRecords(); // 日期選擇後載入該日紀錄
    }

    // 3. 這裡是套用字體大小的方法
    private void ApplyFontSize(int size)
    {
        // 你可以根據頁面內所有文字元件一一設定 FontSize
        datePicker.FontSize = size;
        sessionPicker.FontSize = size;
        fillEntry.FontSize = size;
        drainEntry.FontSize = size;
        todayTotalLabel.FontSize = size;

        // recordsListView 是列表，如果是 Template 內的 Label，
        // 你需要在 XAML 裡面把字體大小綁定到 ViewModel 或使用 DataTemplate，
        // 這裡示範基本的設置，如果想要更動態，請告訴我。

        // 假設你頁面還有其他 Label 也一併設定
        // 例如
        // someLabel.FontSize = size;
    }


    private async void backButton_Clicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new Choose());
    }
}
