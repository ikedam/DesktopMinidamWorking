using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesktopMinidamWorking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static Random random = new Random();
        private static AnimationScenario scenario = new AnimationScenario();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        /**
         *  ミニダムの大枠の状態
         */
        public enum MinidamState
        {
            Unknown,
            Idle,       // ひま
            Working,    // 働いている
        }

        private AnimationState animationState;
        private string animationImage;
        private string talkingImage;
        private int frame;
        private int duration;
        private int talkingDuration = 0;
        private bool silentMode;
        private bool talkingAlert;
        private BalloonWindow balloonWindow;
        private System.Windows.Threading.DispatcherTimer timer;
        private ModifierKeys lastModifiers;
        private string reportFile;
        private DateTime startTime;
        private DateTime workStartTime;

        private delegate void MessageToShow();
        private MessageToShow messageToShow;

        // タスクトレイのアイコン
        // なぜか WPF ではサポートされないので、
        // ここだけ　WindowsForm。
        private System.Windows.Forms.NotifyIcon notifyIcon;

        private bool _menuTopmostChecked;
        public bool menuTopmostChecked
        {
            get
            {
                return _menuTopmostChecked;
            }
            set
            {
                _menuTopmostChecked = value;
                OnPropertyChanged("menuTopmostChecked");
            }
        }

        private bool _watchModifiers;
        public bool watchModifiers
        {
            get
            {
                return _watchModifiers;
            }
            set
            {
                _watchModifiers = value;
                OnPropertyChanged("watchModifiers");
            }
        }

        private string _imageFile;
        public string imageFile
        {
            get
            {
                return _imageFile;
            }

            protected set
            {
                _imageFile = value;
                OnPropertyChanged("imageFile");
            }
        }

        private MinidamState _state;
        private MinidamState state
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                OnPropertyChanged("IsIdle");
                OnPropertyChanged("IsWorking");
            }
        }


        public bool IsIdle
        {
            get
            {
                return state == MinidamState.Idle;
            }
        }

        public bool IsWorking
        {
            get
            {
                return state == MinidamState.Working;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            // デザイナのプロパティをこのオブジェクトにバインド
            this.DataContext = this;
            // 本来、親の DataContext だけに設定すればいいはずだが、
            // 最初の表示までは DataContext が作られないとかなんとか。
            this.ContextMenu.DataContext = this;

            silentMode = ((Keyboard.Modifiers & ModifierKeys.Shift) != 0);

            if (Properties.Settings.Default.version != System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString())
            {
                Properties.Settings.Default.Upgrade();
            }
            if (Properties.Settings.Default.version != null && Properties.Settings.Default.version != "")
            {
                // 設定から復旧
                reportFile = Properties.Settings.Default.reportFile;
                menuTopmostChecked = Properties.Settings.Default.topmost;
                watchModifiers = Properties.Settings.Default.watchModifiers;
                Left = Properties.Settings.Default.X;
                Top = Properties.Settings.Default.Y;
            }
            else
            {
                reportFile = "勤務記録.txt";
                menuTopmostChecked = true;
                watchModifiers = true;
                GotoHome();
            }

            balloonWindow = new BalloonWindow();
            CloseBalloon();

            InitNotifyIcon();

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33.3);
            timer.Tick += timer_Tick;
            timer.Start();

            state = MinidamState.Unknown;
            StopWork();

            startTime = DateTime.Now;
            WriteReport(startTime, "起動したよ");
            if(silentMode)
            {
                messageToShow = () => OpenBalloon("デバッグモード", alerting: true);
            }
        }

        private void GotoHome()
        {
            // 画面右下に表示
            Left = System.Windows.SystemParameters.WorkArea.Left
                + System.Windows.SystemParameters.WorkArea.Width
                - Width;
            Top = System.Windows.SystemParameters.WorkArea.Top
                + System.Windows.SystemParameters.WorkArea.Height
                - Height;
        }

        private string GetReportFilePath()
        {
            if(System.IO.Path.IsPathRooted(reportFile))
            {
                return reportFile;
            }
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                reportFile
            );
        }

        private string FormatDuration(int duration)
        {
            if(duration < 0)
            {
                return "";
            }
            TimeSpan span = TimeSpan.FromSeconds(duration);
            return string.Format("{0}:{1:00}:{2:00}", span.Days * 24 + span.Hours, span.Minutes, span.Seconds);
        }

        private bool WriteReport(DateTime timeToRecord, string message, int powerOnDuration = -1, int workDuration = -1)
        {
            if(silentMode)
            {
                // デバッグモード
                return true;
            }
            try
            {
                string[] values = {
                    timeToRecord.ToString("yyyy/MM/dd HH:mm:ss"),
                    message,
                    FormatDuration(powerOnDuration),
                    FormatDuration(workDuration),
                };
                System.IO.StreamWriter st = new System.IO.StreamWriter(
                    GetReportFilePath(),
                    true,
                    Encoding.GetEncoding("UTF-8")
                );
                st.WriteLine(string.Join("\t", values));
                st.Close();
            }
            catch(Exception e)
            {
                messageToShow = () => OpenBalloon("ファイルへの書き込みに失敗したよ！\n" + e.ToString(), alerting: true);
                return false;
            }
            return true;
        }

        private Uri getBundledResurceUri(string name)
        {
            // Pack URIs in WPF
            // https://msdn.microsoft.com/en-us/library/aa970069%28v=vs.110%29.aspx 
            return new Uri(string.Format("pack://application:,,,{0}", name));
        }

        private void InitNotifyIcon()
        {
            // タスクトレイに表示
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = Title;

            // Pack URIs in WPF
            // https://msdn.microsoft.com/en-us/library/aa970069%28v=vs.110%29.aspx 
            System.IO.Stream s = Application.GetResourceStream(getBundledResurceUri("/images/icon.ico")).Stream;
            notifyIcon.Icon = new System.Drawing.Icon(s);
            notifyIcon.Click += new EventHandler(NotifyIcon_Click);
            notifyIcon.Visible = true;
        }

        private void NotifyIcon_Click(object sender, System.EventArgs e)
        {
            this.Activate();
            this.ContextMenu.IsOpen = true;
        }

        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
            if (IsWorking)
            {
                messageToShow = () => OpenBalloon("まだ働いてるよ！", 30);
                return;
            }
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MenuItemTopmost_CheckToggled(object sender, RoutedEventArgs e)
        {
            Topmost = menuTopmostChecked;
            balloonWindow.Topmost = menuTopmostChecked;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (state != MinidamState.Working)
            {
                DateTime stopTime = DateTime.Now;
                WriteReport(stopTime, "終了したよ！",
                    powerOnDuration: (int)((stopTime - workStartTime).TotalSeconds)
                );
            }
            else
            {
                DateTime stopTime = DateTime.Now;
                WriteReport(stopTime, "はたらいてるのに終了したよ！",
                    powerOnDuration: (int)((stopTime - startTime).TotalSeconds),
                    workDuration: (int)((stopTime - workStartTime).TotalSeconds)
                );
            }
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            if (balloonWindow != null)
            {
                balloonWindow.Close();
            }
            Properties.Settings.Default.reportFile = reportFile;
            Properties.Settings.Default.topmost = menuTopmostChecked;
            Properties.Settings.Default.watchModifiers = watchModifiers;
            Properties.Settings.Default.X = (int)Left;
            Properties.Settings.Default.Y = (int)Top;
            Properties.Settings.Default.version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Properties.Settings.Default.Save();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (frame != 0)
            {
                CheckModifierKeys();

                if (messageToShow != null)
                {
                    // メッセージの表示が予約されている
                    messageToShow();
                    messageToShow = null;
                    return;
                }

                if (IsTalking())
                {
                    // しゃべっている間の制御は行わない
                    if (talkingDuration > 0)
                    {
                        if (--talkingDuration <= 0)
                        {
                            CloseBalloon();
                        }
                    }
                    return;
                }
            }
            
            AnimationDefinition def = scenario.GetAnimationDefinition(animationState);
            // 通常のフレーム再生
            if(frame == 0)
            {
                // フレーム　0 は特別な扱い。
                // 継続時間を決定する。
                duration = random.Next(def.durationFramesMin, def.durationFramesMax);
                frame = 1;
                talkingImage = def.talkingImage;
                if (IsTalking())
                {
                    imageFile = talkingImage;
                }
            }
            if(--duration <= 0)
            {
                int possibility = random.Next(100);
                foreach(AnimationTransition trans in def.transitions)
                {
                    possibility -= trans.possibility;
                    if(possibility < 0)
                    {
                        animationState = trans.state;
                        break;
                    }
                }
                frame = 0;
                return;
            }

            while (!ProcessFrame(def)) ;
            ++frame;
        }

        private bool ProcessFrame(AnimationDefinition def)
        {
            KeyFrameInformation keyInfo = def.GetKeyFrame(frame);
            if(keyInfo != null)
            {
                if (keyInfo.transitions.Length >= 0)
                {
                    int possibility = random.Next(100);
                    foreach (KeyFrameTransition trans in keyInfo.transitions)
                    {
                        possibility -= trans.possibility;
                        if (possibility < 0)
                        {
                            frame = trans.frame;
                            return false;
                        }
                    }
                }
                if (keyInfo.image != null)
                {
                    animationImage = keyInfo.image;
                    if (!IsTalking())
                    {
                        imageFile = animationImage;
                    }
                }
            }
            return true;
        }

        private void CheckModifierKeys()
        {
            if (messageToShow != null || (IsTalking() && talkingAlert) || talkingImage == null)
            {
                return;
            }
            ModifierKeys curModifier = watchModifiers?(Keyboard.Modifiers & (
                ModifierKeys.Control
                | ModifierKeys.Shift
                | ModifierKeys.Alt
            )):0;
            if (curModifier == lastModifiers)
            {
                return;
            }
            lastModifiers = curModifier;
            List<string> messages = new List<string>();
            if ((curModifier & ModifierKeys.Shift) != 0)
            {
                messages.Add("Shift");
            }
            if ((curModifier & ModifierKeys.Control) != 0)
            {
                messages.Add("Control");
            }
            if ((curModifier & ModifierKeys.Alt) != 0)
            {
                messages.Add("Alt");
            }
            if(messages.Count <= 0)
            {
                CloseBalloon();
                return;
            }
            messageToShow = () => OpenBalloon(string.Join("\n", messages));
        }

        private bool IsTalking()
        {
            return balloonWindow.Visibility != Visibility.Hidden || messageToShow != null;
        }

        private void OpenBalloon(string message, int duration = 0, bool alerting = false)
        {
            imageFile = talkingImage;
            talkingDuration = duration;
            talkingAlert = alerting;
            balloonWindow.message = message;
            balloonWindow.Title = Title;
            balloonWindow.Icon = Icon;
            balloonWindow.Show();
            if (balloonWindow.Owner == null)
            {
                balloonWindow.Owner = this;
            }
        }
        
        private void CloseBalloon()
        {
            balloonWindow.Visibility = Visibility.Hidden;
            balloonWindow.message = "";
            talkingDuration = 0;
            talkingAlert = false;
            imageFile = animationImage;
            messageToShow = null;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            balloonWindow.Left = Left + Width / 2 - balloonWindow.Width / 2 - 20;
            balloonWindow.Top = Top - balloonWindow.Height + 40;
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            switch(state)
            {
                case MinidamState.Idle:
                    StartWork();
                    break;
                case MinidamState.Working:
                    StopWork();
                    break;
            }
        }

        private void StartWork()
        {
            if(state == MinidamState.Working)
            {
                return;
            }
            messageToShow = () => OpenBalloon("はたらくよ！！", 30);
            Title = "働いてるよ！ - 働くミニダム";
            state = MinidamState.Working;
            animationState = AnimationState.WorkingNormal;
            frame = 0;
            workStartTime = DateTime.Now;
            WriteReport(workStartTime, "おしごとはじめ");
        }

        private void StopWork()
        {
            if (state == MinidamState.Idle)
            {
                return;
            }
            if (state == MinidamState.Working)
            {
                messageToShow = () => OpenBalloon("今日のおしごと\nおわり！", 30);
                DateTime workStopTime = DateTime.Now;
                WriteReport(workStopTime, "おしごとおわり", workDuration: (int)((workStopTime - workStartTime).TotalSeconds));
            }
            Title = "働くミニダム";
            state = MinidamState.Idle;
            animationState = AnimationState.IdleDoNothing;
            frame = 0;
        }

        private void MenuItemWorkStart_Click(object sender, RoutedEventArgs e)
        {
            StartWork();
        }

        private void MenuItemWorkFinish_Click(object sender, RoutedEventArgs e)
        {
            StopWork();
        }

        public void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if(IsWorking)
            {
                e.Cancel = true;
                Title = "まだ働いてるよ！ - 働くミニダム";
                messageToShow = () => OpenBalloon("まだ働いてるよ！", alerting: true);
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(IsTalking() && talkingAlert)
            {
                CloseBalloon();
            }
        }

        private void MenuItemOpenReport_Click(object sender, RoutedEventArgs e)
        {
            if(System.IO.File.Exists(GetReportFilePath()))
            {
                System.Diagnostics.Process.Start(GetReportFilePath());
            }
            else
            {
                messageToShow = () => OpenBalloon("勤務ファイルがないよ！", alerting: true);
            }
        }

        private void MenuItemGoHome_Click(object sender, RoutedEventArgs e)
        {
            GotoHome();
        }

        private void MenuItemSelectReportFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.Title = "勤務記録ファイルを指定";
            dialog.Filter = "テキストファイル|*.txt";
            dialog.FileName = GetReportFilePath();
            dialog.CreatePrompt = true;
            dialog.OverwritePrompt = false;
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                reportFile = dialog.FileName;
                if(!System.IO.File.Exists(reportFile))
                {
                    WriteReport(startTime, "起動したよ");
                    if (IsWorking)
                    {
                        WriteReport(workStartTime, "おしごとはじめ");
                    }
                }
            }
        }
    }
}
