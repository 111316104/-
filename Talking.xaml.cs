using System;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using static System.Net.WebRequestMethods;
using System.Speech.Synthesis;
using System.Collections.ObjectModel;


#if ANDROID
using PD_app.Platforms.Android;  // 請替換成你的命名空間
#endif
namespace PD_app
{
    public partial class Talking : ContentPage
    {
        public Talking()
        {
            InitializeComponent();
            // 在 Record 的建構子或需要時
            FontManager.ApplyFontSizeToPage(this);

            
        }
        HttpClient httpClient = new HttpClient();
        string apiUrl2 = "http://192.168.137.1:5000/chat"; //電腦開熱點給手機的api
        //string apiUrl2 = "http://192.168.47.36:5000/chat";//手機開熱點給電腦的api 
        //string apiUrl2 = "http://127.0.0.1:5000/chat";//windows本機連線的api

        private List<string> ImageSources = new();
        private int _currentIndex = 0;
        private IDispatcherTimer _timer;
        private ChatDatabase _chatDb;
        private CancellationTokenSource _ttsCts;
        private bool _isSpeaking = false;

        // 避免語音回呼連續觸發
        private bool _isVoiceProcessing = false;

        // 避免語音辨識重複送出相同文字
        private string _lastVoiceText = "";

        private CancellationTokenSource _typingCts;

        private string _lastAIReply = "";   // 用來記錄上一輪 AI 的完整回答

        private void SetHintButtonsEnabled(bool enabled)
        {
            HintBtn1.IsEnabled = enabled;
            HintBtn2.IsEnabled = enabled;
            HintBtn3.IsEnabled = enabled;
            HintBtn4.IsEnabled = enabled;
            // 語音按鈕
            MicButton.IsEnabled = enabled;
            UserInput.IsEnabled = enabled;
        }
        private void LockAllButtons()
        {
            HintBtn1.IsEnabled = false;
            HintBtn2.IsEnabled = false;
            HintBtn3.IsEnabled = false;
            MicButton.IsEnabled = false;
        }

        private void UnlockAllButtons()
        {
            HintBtn1.IsEnabled = true;
            HintBtn2.IsEnabled = true;
            HintBtn3.IsEnabled = true;
            MicButton.IsEnabled = true;
        }


        //private readonly string[] HintQuestions = new[]
        //{
        //    "我該如何正確洗手？",
        //    "腹膜透析後需要注意什麼？",
        //    "每天飲水量要多少？",
        //    "透析液混濁了怎麼辦？",
        //    "我可以吃哪些食物？"
        //};

        //private void ShowHintQuestions()
        //{
        //    foreach (var hint in HintQuestions)
        //    {
        //        var label = new Label
        //        {
        //            Text = hint,
        //            TextColor = Colors.DarkSlateGray,
        //            Margin = new Thickness(5),
        //            Padding = new Thickness(10)
        //        };

        //        var frame = new Frame
        //        {
        //            Content = label,
        //            BackgroundColor = Color.FromArgb("#F7DCB9"),
        //            CornerRadius = 10,    // 圓角
        //            Padding = 0,
        //            HasShadow = false
        //        };

        //        // 點擊可將文字填入輸入框
        //        var tapGesture = new TapGestureRecognizer();
        //        tapGesture.Tapped += (s, e) =>
        //        {
        //            UserInput.Text = hint; // 假設 Entry 名稱是 UserInput
        //        };
        //        frame.GestureRecognizers.Add(tapGesture);

        //        ChatContainer.Children.Add(frame); // 假設聊天容器是 ChatContainer
        //    }
        //}

        private void OnHintClicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                string hint = btn.Text;

                // 直接送出提示給聊天邏輯
                UserInput.Text = hint;
                OnSendClicked(UserInput, EventArgs.Empty);

                // 清空輸入框，避免顯示在聊天容器
                UserInput.Text = "";
            }
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();

            // 初始化本地資料庫
            if (_chatDb == null)
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "chat.db3");
                _chatDb = new ChatDatabase(dbPath);
            }

            // 載入歷史訊息
            //_ = LoadChatHistoryAsync();

            ApplyTheme();
            Application.Current.RequestedThemeChanged += OnThemeChanged;

            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => UserInput.Focus());
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(300), () => box());
        }

        //private async Task LoadChatHistoryAsync()
        //{
        //    if (_chatDb == null) return;

        //    var messages = await _chatDb.GetAllMessagesAsync();
        //    ChatHistory.Text = "";
        //    foreach (var msg in messages)
        //    {
        //        ChatHistory.Text += $"{msg.Sender}：{msg.Message}\n";
        //    }
        //}


        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // 離開頁面時解除訂閱，避免記憶體洩漏
            Application.Current.RequestedThemeChanged -= OnThemeChanged;

            // 停止正在播放的語音
            _ttsCts?.Cancel();
        }

        private void OnThemeChanged(object sender, AppThemeChangedEventArgs e)
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            var theme = Application.Current.RequestedTheme;

            if (theme == AppTheme.Dark)
            {
                this.BackgroundColor = Colors.White;
                DialogText.TextColor = Colors.Black;
                ChatHistory.TextColor = Colors.Black;
                FloatingDialog.BorderColor = Colors.White;
                UserInput.TextColor = Colors.White;
                UserInput.BackgroundColor = Colors.DarkGray;
            }
            else
            {
                this.BackgroundColor = Colors.White;
                DialogText.TextColor = Colors.Black;
                ChatHistory.TextColor = Colors.Black;
                FloatingDialog.BorderColor = Colors.Black;
                UserInput.TextColor = Colors.Black;
                UserInput.BackgroundColor = Colors.LightGray;
            }
        }
        // 顯示自訂訊息，但不存進 DB
        private async void ShowMenuMessage(string message)
        {
            // 停止前一段打字
            _typingCts?.Cancel();
            _typingCts = new CancellationTokenSource();

            // 清空舊訊息
            ChatHistory.Text = "";

            // 啟動動畫
            action();

            // 語音立即開始（完整句子）
            var speakTask = SpeakTextAsync(message, () =>
            {
                MainThread.BeginInvokeOnMainThread(() => standby());
            });

            // UI 逐字效果 + 可中止
            var typingTask = ShowTextWithTypingEffect(ChatHistory, message, _typingCts.Token);

            // 等語音和打字一起完成
            await Task.WhenAll(speakTask, typingTask);
        }



        private async Task ShowTextWithTypingEffect(Label label, string text, CancellationToken token, int delay = 120)
        {
            // 每次新回答，先清空舊文字
            label.Text = "";

            foreach (char c in text)
            {
                if (token.IsCancellationRequested)
                    return; // 如果被取消，停止逐字輸出

                label.Text += c;

                // 滾動到底部
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ChatScrollView.ScrollToAsync(0, label.Height, true);
                });

                await Task.Delay(delay);
            }
        }




        private async Task box()
        {
            DialogText.Text = "你好！我是AI助理。\n";

#if ANDROID
            SpeechToTextService.OnSpeechRecognized += async (text) =>
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // 防止連續觸發
                    if (_isVoiceProcessing)
                        return;

                    _isVoiceProcessing = true;

                    // 避免相同語句重複送
                    if (text == _lastVoiceText)
                    {
                        _isVoiceProcessing = false;
                        return;
                    }

                    _lastVoiceText = text;

                    // 更新 UI
                    UserInput.Text = text;

                    // 只會送一次
                    await SendMessageAsync(text);

                    // 等待語音系統停止回呼
                    await Task.Delay(700);

                    _isVoiceProcessing = false;
                });
            };


#endif
            await standby();
        }
        private (string folder, int totalImages) GetRandomwait()
        {
            var random = new Random();
            int roll = random.Next(0, 100); // 0 ~ 99

            if (roll < 30)
            {
                return ("wait_one", 131); // wait_one 30%
            }
            else if (roll < 60)
            {
                return ("wait_two", 131); // wait_two 30%
            }
            else
            {
                return ("myimages", 74); // myimages 40%
            }
        }
        private async Task standby()
        {
            _timer?.Stop();
            _timer = null;
            ImageSources.Clear();
            _currentIndex = 0;

            // 🔹 每次呼叫 standby 都重新亂數選擇
            var (folder, totalImages) = GetRandomwait();

            for (int i = 0; i < totalImages; i++)
            {
                string fileName = $"{folder}_{i:D5}.png";
                ImageSources.Add(fileName);
            }

            if (ImageSources.Count > 0)
            {
                AnimatedImage.Source = ImageSources[_currentIndex];

                _timer = Dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(50);
                _timer.Tick += async (s, e) =>
                {
                    _currentIndex++;

                    if (_currentIndex >= ImageSources.Count)
                    {
                        // 🔹 播放完一輪 → 停止，重新進入 standby() 抽新動作
                        _timer.Stop();
                        await standby();
                        return;
                    }

                    AnimatedImage.Source = ImageSources[_currentIndex];
                };
                _timer.Start();
            }
        }
        private (string folder, int totalImages) GetRandomtalk()
        {
            var random = new Random();
            int roll = random.Next(0, 100); // 0 ~ 99

            if (roll < 30)
            {
                return ("talk_one", 131); // wait_one 30%
            }
            else if (roll < 60)
            {
                return ("talk_two", 131); // wait_two 30%
            }
            else
            {
                return ("speak", 131); // myimages 40%
            }
        }
        private async void action()
        {
            _timer?.Stop();
            _timer = null;
            ImageSources.Clear();
            _currentIndex = 0;
            var (folder, totalImages) = GetRandomtalk();

            for (int i = 0; i < totalImages; i++)
            {
                string fileName = $"{folder}_{i:D5}.png";
                ImageSources.Add(fileName);
            }

            if (ImageSources.Count > 0)
            {
                AnimatedImage.Source = ImageSources[_currentIndex];

                _timer = Dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(50);
                _timer.Tick += async (s, e) =>
                {
                    _currentIndex++;

                    if (_currentIndex >= ImageSources.Count)
                    {
                        if (_isSpeaking)
                        {
                            // 語音還在播 → 循環回頭繼續動
                            _currentIndex = 0;
                        }
                        else
                        {
                            // 語音結束 → 回待機
                            _timer.Stop();
                            await standby();
                            return;
                        }
                    }

                    AnimatedImage.Source = ImageSources[_currentIndex];
                };
                _timer.Start();
            }
        }

        private async void OnMicButtonClicked(object sender, EventArgs e)
        {
#if ANDROID
            SpeechToTextService.StartListening();
#else
            await DisplayAlert("提醒", "此功能只支援 Android", "OK");
#endif
        }

        public async Task SpeakTextAsync(string textToSpeak, Action onSpeakCompleted = null)
        {
            try
            {
                // 如果已經在講話 → 取消舊語音
                if (_isSpeaking)
                {
                    _ttsCts?.Cancel();
                    _isSpeaking = false;
                }

                _ttsCts = new CancellationTokenSource();
                _isSpeaking = true;

                // ─── 語音開始：切成說話動畫 ───
                MainThread.BeginInvokeOnMainThread(() => action());

                // 找中文語系
                var locales = await TextToSpeech.Default.GetLocalesAsync();
                var selectedLocale = locales.FirstOrDefault(l => l.Language.StartsWith("zh"));

                var options = new SpeechOptions
                {
                    Locale = selectedLocale,
                    Pitch = 1.0f,
                    Volume = 1.0f
                };

                // ⭐ 使用 token 直接控制中斷
                await TextToSpeech.Default.SpeakAsync(textToSpeak, options, _ttsCts.Token);

                // ─── 語音完成：切回待機動畫 ───
                _isSpeaking = false;
                MainThread.BeginInvokeOnMainThread(() => standby());

                onSpeakCompleted?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // ⭐ 被取消時 → 不切動畫、不報錯
                _isSpeaking = false;
            }
            catch (Exception ex)
            {
                _isSpeaking = false;
                await DisplayAlert("語音錯誤", ex.Message, "OK");
                MainThread.BeginInvokeOnMainThread(() => standby());
            }
        }



        private async void OnSendClicked(object sender, EventArgs e)
        {
            var message = UserInput.Text;
            if (string.IsNullOrWhiteSpace(message)) return;

            await SendMessageAsync(message);
        }
        private async void ChooseClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
        private async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // 🔸 先停用提示按鈕
            SetHintButtonsEnabled(false);

            try
            {
                ChatHistory.Text = $"你：{message}\n";

                var requestBody = new { message = message };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response2 = await httpClient.PostAsync(apiUrl2, content);

                if (response2.IsSuccessStatusCode)
                {
                    var resultJson = await response2.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resultJson);

                    string reply = "";
                    if (doc.RootElement.TryGetProperty("reply", out var replyElement))
                        reply = replyElement.GetString();

                    // 儲存歷史紀錄
                    if (_chatDb != null &&
                        reply != "抱歉，這個問答已超出我的回答範圍")
                    {
                        await _chatDb.SaveMessageAsync(new ChatMessage
                        {
                            Sender = "你",
                            Message = message,
                            Timestamp = DateTime.Now
                        });

                        await _chatDb.SaveMessageAsync(new ChatMessage
                        {
                            Sender = "AI",
                            Message = reply,
                            Timestamp = DateTime.Now
                        });
                    }

                    // 中斷舊動畫、開始新動畫
                    _typingCts?.Cancel();
                    _typingCts = new CancellationTokenSource();

                    action();
                    ChatHistory.Text += "AI：";

                    var typingTask = ShowTextWithTypingEffect(ChatHistory, reply, _typingCts.Token);
                    var speakTask = SpeakTextAsync(reply);

                    await Task.WhenAll(typingTask, speakTask);
                }
                else
                {
                    ChatHistory.Text += "❌ 錯誤：伺服器沒有回應成功\n";
                    standby();
                }
            }
            catch (Exception ex)
            {
                ChatHistory.Text += $"❌ 發生錯誤：{ex.Message}\n";
                standby();
            }

            UserInput.Text = "";

            // 🔹 回復提示按鈕可用
            SetHintButtonsEnabled(true);
        }


        private async void OnHistoryClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage.Navigation.PushAsync(new ChatHistoryPage());
        }


        // 右上角主選單按鈕
        private void OnMenuToggleClicked(object sender, EventArgs e)
        {
            MenuToggleButton.IsVisible = false;
            NestedMenu.IsVisible = true;
        }
        
        // 收合整個選單
        private void close()
        {
            NestedMenu.IsVisible = false;
            button1.IsVisible = false;
            button2.IsVisible = false;
            button3.IsVisible = false;
            button4.IsVisible = false;
            button1_1.IsVisible = false;
            button1_2.IsVisible = false;
            button1_3.IsVisible = false;
            button1_4.IsVisible = false;
            button1_5.IsVisible = false;
            button2_1.IsVisible = false;
            button2_2.IsVisible = false;
            button2_3.IsVisible = false;
            button3_1.IsVisible = false;
            button3_2.IsVisible = false;
            button3_3.IsVisible = false;
            button4_1.IsVisible = false;
            MenuToggleButton.IsVisible = true;
        }
        private void OnMenuCloseClicked(object sender, EventArgs e)
        {
            close();
        }

        // ===== 第 1 層：自我照護與日常管理 =====
        private void button1Clicked(object sender, EventArgs e)
        {
            button1.IsVisible = !button1.IsVisible;
        }
        //=========================第 1 層=========================//
        private void button1_1Clicked(object sender, EventArgs e)
        {
            button1_1.IsVisible = !button1_1.IsVisible;
        }
        //=========================第 2 層=========================//
        private void button1_1_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("正確洗手:​每次更換透析液前，應使用肥皂和清水徹底洗手，至少20秒，並使用乾淨的毛巾或紙巾擦乾。\n​\n使用酒精消毒:​在無法洗手的情況下，可使用含酒精的手部消毒液進行手部清潔。\n");

            close();
        }

        private void button1_1_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("保持清潔：​\n選擇一個乾淨、無塵、無風的房間進行透析操作，避免在有寵物的環境中進行。\n​\n避免干擾：​\n操作時，應關閉電風扇、空調等可能產生氣流的設備，以減少空氣中懸浮微粒的干擾。\n");

            close();
        }
        private void button1_1_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("佩戴口罩：​\n在更換透析液時，應佩戴口罩，以防止呼吸道病原體的傳播。\n​\n穿著清潔衣物:​操作前應更換乾淨的衣物，避免衣物上的灰塵或細菌污染操作區域。\n");

            close();
        }

        private void button1_1_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("器材消毒：​\n使用前，應確保所有透析器材已經過適當的消毒處理。\n​\n避免重複使用:​一次性使用的器材不得重複使用，以防止交叉感染。\n​");

            close();
        }
        private void button1_1_5Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("保持導管清潔:​每日檢查導管出口處，確保無紅腫、滲液等感染徵象。\n​\n固定導管:​使用透氣膠帶將導管固定，避免導管移動造成皮膚損傷或感染。\n");

            close();
        }

        private void button1_1_6Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("觀察症狀:​如出現腹痛、發燒、透析液混濁等症狀，應立即聯繫醫療人員。\n定期檢查：​\n定期回診，進行血液檢查和透析液培養，以早期發現潛在感染。\n");

            close();
        }

        private void button1_1_7Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("接受專業訓練：​\n在開始居家透析前，應接受醫療人員的專業訓練，學習正確的操作流程和感染預防措施。\n持續學習：​\n定期參加醫院舉辦的衛教課程，更新相關知識和技能。\n");

            close();
        }

        //=========================第 2 層=========================//
        private void button1_2Clicked(object sender, EventArgs e)
        {
            button1_2.IsVisible = !button1_2.IsVisible;
        }
        private void button1_2_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("1. 環境清潔:選擇一個乾淨、無塵、無風的房間進行透析操作，避免在有寵物的環境中進行。操作時，應關閉電風扇、空調等可能產生氣流的設備，以減少空氣中懸浮微粒的干擾。\n2. 手部衛生:每次更換透析液前，應使用肥皂和清水徹底洗手，至少20秒，並使用乾淨的毛巾或紙巾擦乾。在無法洗手的情況下，可使用含酒精的手部消毒液進行手部清潔。\n3. 個人防護:在更換透析液時，應佩戴口罩，以防止呼吸道病原體的傳播。操作前應更換乾淨的衣物，避免衣物上的灰塵或細菌污染操作區域。\n");

            close();
        }
        private void button1_2_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("1. 檢查器材:確認透析液的種類、濃度、有效期限，並檢查包裝是否完整無損。\n2. 連接導管:使用無菌技術連接透析液袋與腹膜透析導管，避免觸碰接頭部分，以防止污染。\n3. 注入透析液:將透析液緩慢注入腹腔，注入時間約為10至15分鐘。\n4. 停留時間:讓透析液在腹腔內停留約4至6小時，期間可進行日常活動。\n5. 引流透析液:將使用過的透析液從腹腔引流出來，引流時間約為10至20分鐘。\n6. 更換透析液:重複上述步驟，每日更換透析液的次數依醫師指示進行。\n");

            close();
        }
        private void button1_2_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("操作後注意事項:1. 檢查透析液:觀察引流出的透析液是否清澈透明，若出現混濁、血色或異味，應立即聯繫醫療人員。\n2. 導管護理:每日檢查導管出口處，確保無紅腫、滲液等感染徵象。\n使用透氣膠帶將導管固定，避免導管移動造成皮膚損傷或感染。\n3. 記錄透析情況:詳細記錄每次透析的時間、透析液種類、注入量、引流量及透析液的外觀，供醫療人員參考。\n");

            close();
        }
        private void button1_2_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("1. 注入困難:若發現注入透析液時有困難，可能是導管阻塞或位置不當，應更換姿勢或輕輕按摩腹部，若仍無改善，應聯繫醫療人員。\n2. 引流不順:若引流透析液時流速變慢或停止，可能是導管被腸道壓迫或位置移動，應更換姿勢或輕輕按摩腹部，若仍無改善，應聯繫醫療人員。\n3. 透析液混濁:若引流出的透析液呈現混濁，可能是腹膜炎的徵兆，應立即聯繫醫療人員進行評估與治療。\n");

            close();
        }
        //=========================第 2 層=========================//
        private void button1_3Clicked(object sender, EventArgs e)
        {
            button1_3.IsVisible = !button1_3.IsVisible;
        }
        private void button1_3_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("避免高鈉食物：如罐頭食品、醃製食品、蜜餞、香腸、臘肉、醬油、番茄醬、沙茶醬、味精等，因為鈉攝取過多會造成水分堆積，增加體重和血壓負擔。\n烹調時減少鹽分：建議使用天然香料（如檸檬、香草）替代鹽，減少口渴感。\n");

            close();
        }

        private void button1_3_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("攝取優質蛋白質：建議每天攝取1.2克/公斤體重的蛋白質，選擇優質蛋白質來源，如雞蛋、魚、瘦肉、黃豆製品等。\n避免過量攝取：過多蛋白質可能增加代謝廢物，增加腎臟負擔。\n");

            close();
        }
        private void button1_3_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("鉀的攝取：一般情況下不需嚴格限制鉀的攝取，但若出現高血鉀症狀，應減少高鉀食物的攝取，如香蕉、橘子、馬鈴薯等。\n磷的攝取：限制高磷食物，如內臟、堅果、乳製品等，必要時可在餐時服用磷結合劑，以維持正常血磷值。\n");

            close();
        }

        private void button1_3_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("控制糖類攝取：部分腹膜透析液中含有葡萄糖，應注意總糖分攝取，避免血糖過高。\n選擇健康脂肪：避免飽和脂肪和反式脂肪，選擇橄欖油、魚油等健康脂肪來源。\n");

            close();
        }
        private void button1_3_5Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("有尿患者：每日飲水量 = 前一日尿量 + 500～700cc。\n無尿患者：每日飲水量約為500cc。\n");

            close();
        }

        private void button1_3_6Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("兩次透析間體重增加：不應超過乾體重的5%，以避免透析時出現低血壓、抽筋等併發症。\n");

            close();
        }

        private void button1_3_7Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("保持口腔濕潤：可用開水漱口，或含檸檬片、無糖口香糖、薄荷片等，減少口渴感。\n避免高鈉食物：減少鹽分攝取，降低口渴的可能性。\n避免長時間待在冷氣房：空氣乾燥可能增加口渴感。\n");

            close();
        }
        private void button1_3_8Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("限制高水分食物：如西瓜、湯品、果凍等，避免攝取過多水分。\n選擇乾飯或乾麵：主食建議選擇含水量較低的乾飯或乾麵，避免稀飯等高水分食物。\n");

            close();
        }
        //=========================第 2 層=========================//
        private void button1_4Clicked(object sender, EventArgs e)
        {
            button1_4.IsVisible = !button1_4.IsVisible;
        }
        private void button1_4_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("1.1 日常自我監測:．體重：每日同一時間、同一服裝下測量（建議早上起床空腹、排空導管後），記錄「實測體重」與「乾體重」差值，若超過 2%–3%（兩次透析間），即需調整飲水量或透析處方。\n．血壓與脈搏：每次透析前、透析後各測一次，記錄坐姿與立姿血壓，觀察有無姿勢性低血壓。\n注意頭暈、心悸等低血壓或高血壓症狀。\n．腹圍與水腫檢查：定期測量腹部圍度，若突然增加可能有腹水或積液。\n觀察下肢、足部是否水腫，是否有壓痕性水腫（按壓凹陷即水腫）。\n．透析液外觀與引流量：每次引流後檢視液體是否清澈無懸浮物，記錄注入量與引流量差值，若引流量持續減少，注意導管阻塞或黏連。\n．尿量與餘留尿：若有自主排尿，記錄每日尿量。\n觀察有無血尿、蛋白尿或混濁現象。\n1.2 定期檢查項目:．血液檢查（每 1–3 月）：血清白蛋白、BUN/Cr、電解質（Na⁺、K⁺、Cl⁻、Ca²⁺、PO₄³⁻）、糖化血色素（HbA1c，若合併糖尿病）、全血球計數（WBC、Hb、Hct）。\n．腹腔透析液培養（有混濁或感染徵象時）：送培養鑑定病原菌，並配合抗生素治療。\n．心電圖與心臟超音波（有心血管病史者）：監測心功能與容積負荷狀況。\n");

            close();
        }

        private void button1_4_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("2.1 自我覺察與記錄:．情緒日誌：每日簡單記錄心情狀態（平靜、焦慮、憂鬱、易怒等），註記產生壓力的事件或思緒。\n．睡眠品質：記錄入睡時間、夜間醒來次數與清晨起床時間，評估日間疲倦、注意力不集中情形。\n．疲勞與體力：使用簡易疲勞量表（例如 0–10 分級）每日評估，留意活動後是否需長時間休息或心跳、呼吸急促。\n2.2 親友與團隊支持:．家庭互動：定期與家人分享治療進度與感受，請家人協助觀察情緒變化。\n安排陪伴與共同活動，減少孤立感。\n．社會支持：參加病友團體或網路論壇，互相鼓勵與經驗交流。\n如有宗教或志工支持，可參加靜心、冥想、閱讀等活動。\n2.3 專業評估與介入:．心理篩檢（每 3–6 月）：使用簡易量表（PHQ-9 抑鬱量表、GAD-7 焦慮量表）評估情緒狀態。\n．諮商與治療：若量表顯示中重度焦慮或憂鬱，建議轉介心理師或精神科醫師。\n可考慮團體支持、正念減壓課程（MBSR）或心智圖像療法。\n．藥物輔助：必要時在醫師評估下，使用抗焦慮或抗憂鬱藥物。\n");

            close();
        }
        //=========================第 2 層=========================//
        private void button1_5Clicked(object sender, EventArgs e)
        {
            button1_5.IsVisible = !button1_5.IsVisible;
        }
        private void button1_5_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("遵從醫囑：所有藥物應依醫師處方使用，切勿自行調整劑量或停藥。\n定時服用：建立固定的服藥時間表，使用藥盒或提醒工具，確保不漏服。\n與食物的關係：注意藥物與食物的相互作用，如某些藥物需空腹服用，某些則需與食物同服。\n");

            close();
        }

        private void button1_5_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("降壓藥：腹膜透析患者常伴隨高血壓，應定期監測血壓，並依醫師指示調整降壓藥物。\n磷結合劑：腎功能下降會導致血磷升高，需使用磷結合劑控制磷水平，並注意與其他藥物的相互作用。\n活性維他命D：用於治療腎性骨病，需監測血鈣、血磷及副甲狀腺激素（PTH）水平，避免過量。\n紅血球生成素刺激劑（ESA）：用於治療貧血，需監測血紅素水平，避免過快上升。\n抗生素：如有感染徵兆，需依醫師指示使用抗生素，並完成療程。\n");

            close();
        }
        private void button1_5_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("藥物紀錄：建立藥物使用紀錄，包括藥物名稱、劑量、服用時間及可能的副作用。\n定期回診：定期回診，與醫師討論藥物使用情況，必要時調整治療方案。\n藥物儲存：藥物應存放於乾燥陰涼處，避免陽光直射，並遠離兒童可及範圍。\n藥物過期處理：過期或不再使用的藥物應依醫院或藥局指示妥善處理，避免自行丟棄。\n");

            close();
        }

        private void button1_5_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("透析對藥物的影響：腹膜透析可能影響某些藥物的吸收或清除，需與醫師討論藥物調整。\n透析前後的服藥時間：某些藥物應在透析前或透析後服用，避免與透析液相互作用。\n");

            close();
        }
        // ===== 第 1 層：健康監測與追蹤 =====
        private void button2Clicked(object sender, EventArgs e)
        {
            button2.IsVisible = !button2.IsVisible;
        }
        //=========================第 2 層=========================//
        private void button2_1Clicked(object sender, EventArgs e)
        {
            button2_1.IsVisible = !button2_1.IsVisible;
        }
        private void button2_1_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("1.1 日常自我監測:體重:每日同一時間、同一服裝下測量（建議早上起床空腹、排空導管後）。\n記錄「實測體重」與「乾體重」差值，若超過 2–3%（兩次透析間），即需調整飲水量或透析處方。\n血壓與脈搏:每次透析前、透析後各測一次。\n記錄坐姿與立姿血壓，觀察有無姿勢性低血壓。\n注意頭暈、心悸等低血壓或高血壓症狀。\n腹圍與水腫檢查:定期測量腹部圍度，若突然增加可能有腹水或積液。\n觀察下肢、足部是否水腫、壓痕性水腫（按壓凹陷即水腫）。\n透析液外觀與引流量:每次引流後檢視液體是否清澈無懸浮物。\n記錄注入量與引流量差值，若引流量持續減少，注意導管阻塞或黏連。\n尿量與餘留尿:若有自主排尿，記錄每日尿量。\n觀察有無血尿、蛋白尿或混濁現象。\n\",\r\n\r\n            \"1.2 定期檢查項目:血液檢查（每 1–3 月）:血清白蛋白、BUN/Cr、電解質（Na⁺、K⁺、Cl⁻、Ca²⁺、PO₄³⁻）。\n糖化血色素（HbA1c，若合併糖尿病）。\n全血球計數（感染指標：WBC；貧血指標：Hb、Hct）。\n腹腔透析液培養（有混濁或感染徵象時）:送培養鑑定病原菌，並配合抗生素治療。\n心電圖與心臟超音波（有心血管病史者）:監測心功能與容積負荷狀況。\n");

            close();
        }

        private void button2_1_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("2.1 自我覺察與記錄\n情緒日誌:每日簡單記錄心情狀態（平靜、焦慮、憂鬱、易怒等）。\n註記產生壓力的事件或思緒。\n睡眠品質:記錄入睡時間、夜間醒來次數與清晨起床時間。\n評估日間疲倦、注意力不集中情形。\n疲勞與體力:使用簡易疲勞量表（例如 0–10 分級）每日評估。\n留意活動後是否需長時間休息或心跳、呼吸急促。\n\",\r\n\r\n            \"2.2 親友與團隊支持:家庭互動:定期與家人分享治療進度與感受，請家人協助觀察情緒變化。\n安排行動陪伴與共同活動，減少孤立感。\n社會支持:參加病友團體或網路論壇，互相鼓勵與經驗交流。\n如有宗教或志工支持，可適度參加靜心、冥想、閱讀等活動。\n\",\r\n\r\n            \"2.3 專業評估與介入:心理篩檢（每 3–6 月）:使用簡易量表（PHQ-9 抑鬱量表、GAD-7 焦慮量表）評估情緒狀態。\n諮商與治療:若量表顯示中重度焦慮或憂鬱，建議轉介心理師或精神科醫師。\n可考慮團體支持、正念減壓課程（MBSR）或心智圖像療法。\n藥物輔助:必要時在醫師評估下，使用抗焦慮或抗憂鬱藥物。\n");

            close();
        }

        //=========================第 2 層=========================//
        private void button2_2Clicked(object sender, EventArgs e)
        {
            button2_2.IsVisible = !button2_2.IsVisible;
        }
        private void button2_2_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("定期回診:患者應依醫師指示，定期回診進行健康評估與治療調整。\n定期檢查:包括血液檢查（如血清白蛋白、BUN、Cr、電解質等）、腹腔透析液培養、心電圖等，評估透析效果與併發症風險。\n");

            close();
        }

        private void button2_2_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("主治醫師:負責整體治療計劃的制定與調整，監測透析效果與併發症。\n護理師:提供透析操作指導、衛教與情緒支持，協助患者建立自我照護能力。\n營養師:根據患者的營養狀況，提供個別化的飲食建議，維持營養平衡。\n社工師:協助患者了解社會資源，提供心理支持與生活輔導。\n");

            close();
        }
        private void button2_2_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("健康數據紀錄:患者應每日記錄體重、血壓、透析液進出量、尿量等數據，並定期與醫療團隊分享。\n數據管理:利用電子健康紀錄系統或手機應用程式，協助患者與醫療團隊追蹤健康狀況。\n");

            close();
        }

        private void button2_2_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("教育訓練:醫療團隊應定期提供透析操作、飲食管理、心理調適等方面的教育訓練，提升患者的自我照護能力。\n自我照護:患者應積極參與自我照護，遵守醫囑，定期回診，維持良好的生活習慣。\n");

            close();
        }
        //=========================第 2 層=========================//
        private void button2_3Clicked(object sender, EventArgs e)
        {
            button2_3.IsVisible = !button2_3.IsVisible;
        }
        private void button2_3_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("影音教學資源:書中提供 QR Code，連結至影音教學平台，患者可觀看由護理師親自示範的換液操作影片，並提供國語、臺語、英文及印尼語版本，方便不同語言背景的患者學習。\n圖文解說:書中以全彩圖解方式，詳細說明腹膜透析的操作流程、注意事項及常見問題，讓患者能夠輕鬆理解並應用於日常生活中。\n");

            close();
        }

        private void button2_3_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("定期回診與衛教:患者應定期回診，與醫療團隊討論治療進展，並參與衛教課程，了解最新的治療方法與健康管理知識。\n參與病友支持團體:透過與其他患者交流，分享經驗與學習，增進自我照護能力，並獲得情感支持。\n");

            close();
        }
        private void button2_3_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("紀錄健康數據:患者應每日記錄體重、血壓、透析液進出量、尿量等數據，並定期與醫療團隊分享，協助調整治療計劃。\n使用健康管理工具:利用手機應用程式或電子健康紀錄系統，追蹤健康狀況，並提醒服藥與回診時間。\n");

            close();
        }

        private void button2_3_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("建立正向思維:透過閱讀、冥想、音樂等方式，培養正向思維，提升心理韌性，面對治療過程中的挑戰。\n維持規律生活:保持規律作息、均衡飲食、適度運動，有助於身心健康，提升生活品質。\n");

            close();
        }
        // ===== 第 1 層：心理與社會支持 =====
        private void button3Clicked(object sender, EventArgs e)
        {
            button3.IsVisible = !button3.IsVisible;
        }
        //=========================第 2 層=========================//
        private void button3_1Clicked(object sender, EventArgs e)
        {
            button3_1.IsVisible = !button3_1.IsVisible;
        }
        private void button3_1_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("定期自我檢視情緒狀態:透析患者可能面臨焦慮、憂鬱等情緒，建議每日記錄心情變化，辨識情緒波動的原因。\n建立正向思維:透過閱讀、冥想、音樂等方式，培養正向思維，提升心理韌性。\n設定可達成的目標:制定短期與長期目標，逐步實現，增強自我效能感。\n");

            close();
        }

        private void button3_1_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("與家人保持良好溝通:分享治療進展與情緒狀態，增進理解與支持。\n參與病友支持團體:透過與其他患者交流，獲得情感支持與實務建議。\n利用社會資源:申請政府提供的喘息服務，減輕照護壓力，維持家庭功能。\n");

            close();
        }
        private void button3_1_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("定期心理評估:使用簡易量表（如PHQ-9、GAD-7）評估情緒狀態，及早發現問題。\n尋求專業諮詢:如有需要，諮詢心理師或精神科醫師，獲得專業建議與治療。\n參加心理健康課程:參與正念減壓（MBSR）等課程，提升情緒調適能力。\n");

            close();
        }

        private void button3_1_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("學習透析相關知識:透過閱讀、影音教學等方式，增強自我照護能力，提升自信心。\n建立日常健康習慣:維持規律作息、適度運動、均衡飲食，有助於身心健康。\n");

            close();
        }
        //=========================第 2 層=========================//
        private void button3_2Clicked(object sender, EventArgs e)
        {
            button3_2.IsVisible = !button3_2.IsVisible;
        }
        private void button3_2_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("健保給付:腹膜透析治療在健保給付範圍內，患者可申請相關項目，減輕醫療費用負擔。\n低收入戶補助:符合資格的患者可申請低收入戶補助，獲得生活與醫療費用的支持。\n身心障礙手冊:持有身心障礙手冊者，可享有相關補助與福利，減輕生活壓力。\n");

            close();
        }

        private void button3_2_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("社會局服務:各地社會局提供多項服務，包括居家照護、喘息服務、生活輔具租借等，協助患者與家庭減輕照護壓力。\n慈善機構援助:如慈濟醫院等慈善機構，提供醫療、生活等方面的援助，患者可主動聯繫，獲得協助。\n社區資源:社區內的志工團體、教會等，亦提供陪伴、關懷等服務，增進患者的社會支持。\n");

            close();
        }
        private void button3_2_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("就業服務:部分機構提供就業輔導與職業訓練，協助患者重返工作崗位，維持生活品質。\n生活輔具:患者可申請生活輔具，如輪椅、助行器等，改善日常生活功能。\n心理諮詢:提供心理諮詢服務，協助患者調適心情，面對疾病挑戰。\n");

            close();
        }

        private void button3_2_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("申請流程:患者可向醫療機構、社會局等單位詢問資源申請流程，獲得必要的協助。\n文件準備:申請資源時，需準備相關文件，如病歷證明、收入證明等，確保申請順利。\n專業協助:醫療機構的社工師、護理師等，可提供資源申請的指導與協助，減少患者的困難。\n");

            close();
        }
        //=========================第 2 層=========================//
        private void button3_3Clicked(object sender, EventArgs e)
        {
            button3_3.IsVisible = !button3_3.IsVisible;
        }
        private void button3_3_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("主要照護者:通常為患者的配偶、子女或直系親屬，負責協助患者進行腹膜透析操作、監測健康狀況、管理藥物及飲食等。\n支持角色:其他家庭成員可協助照護者分擔部分責任，如陪伴患者就醫、協助家務等，共同維護患者的生活品質。\n");

            close();
        }

        private void button3_3_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("學習操作技巧:照護者應與患者共同學習腹膜透析的操作流程，包括洗手、準備透析液、消毒導管出口、注入透析液、等待滲透時間、排出透析液等步驟。\n監督操作過程:在患者進行透析操作時，照護者應在旁協助，確保操作正確，並及時處理可能出現的問題。\n");

            close();
        }
        private void button3_3_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("監測生命徵象:定期測量患者的體重、血壓、血糖等指標，並記錄變化情況。\n紀錄透析數據:每日記錄透析液的進出量、尿量、透析時間等，並與醫療團隊分享，協助調整治療計劃。\n");

            close();
        }

        private void button3_3_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("協助飲食規劃:根據營養師的建議，協助患者制定適合的飲食計劃，控制鉀、磷、鈉等電解質的攝取。\n管理藥物使用:協助患者按時服用藥物，並監測可能的副作用，必要時與醫療團隊溝通。\n");

            close();
        }
        private void button3_3_5Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("提供情緒支持:傾聽患者的心聲，理解其情緒變化，提供安慰與鼓勵。\n協助心理調適:鼓勵患者參與支持團體，與其他病友交流經驗，減輕心理壓力。\n");

            close();
        }

        private void button3_3_6Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("制定應急計劃:與醫療團隊共同制定緊急應變計劃，包括透析液儲備、導管護理、突發情況處理等。\n災難備案準備:在自然災害或突發事件發生前，準備足夠的透析物資，確保患者在家中也能進行透析。\n");

            close();
        }
        private void button3_3_7Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("持續學習:參加醫療機構或社區舉辦的衛教課程，提升自我照護知識與技能。\n尋求支持:加入照護者支持團體，與其他照護者交流經驗，獲得情感支持與實務建議。\n");

            close();
        }

        // ===== 第 1 層：緊急應變與互動教學 =====
        private void button4Clicked(object sender, EventArgs e)
        {
            button4.IsVisible = !button4.IsVisible;
        }
        //=========================第 2 層=========================//
        private void button4_1Clicked(object sender, EventArgs e)
        {
            button4_1.IsVisible = !button4_1.IsVisible;
        }
        private void button4_1_1Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("制定應變計畫:與醫療團隊共同制定個人化的災難應變計畫，包含緊急聯絡人、最近的醫療機構、避難路線等資訊。\n定期演練:定期與家人進行災難應變演練，確保在災難發生時能迅速反應。\n");

            close();
        }

        private void button4_1_2Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("透析相關物資:準備至少3天的透析液、導管、消毒用品等，並確保這些物資在有效期限內。\n生活必需品:備妥飲用水、非易腐食物、手電筒、電池、收音機、現金、個人藥物等。\n");

            close();
        }
        private void button4_1_3Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("保持冷靜:在災難發生時，保持冷靜，按照預先制定的應變計畫行動。\n確保安全:確保自身安全後，評估是否可以繼續進行透析操作，若無法，立即聯繫醫療機構。\n");

            close();
        }

        private void button4_1_4Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("更新聯絡資訊:定期更新醫療機構的聯絡資訊，確保在緊急情況下能迅速聯繫。\n了解支援資源:了解當地政府或非營利組織在災難時提供的支援資源，如避難所、醫療支援等。\n");

            close();
        }
        private void button4_1_5Clicked(object sender, EventArgs e)
        {
            ShowMenuMessage("參加培訓課程:參加由醫療機構舉辦的災難應變培訓課程，提升應對災難的能力。\n家庭成員教育:教育家庭成員了解透析操作流程及災難應變計畫，確保在患者無法自行操作時，家人能提供協助。\n");

            close();
        }


    }
}
