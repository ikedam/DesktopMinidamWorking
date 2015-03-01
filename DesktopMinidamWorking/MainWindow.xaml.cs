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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private enum MinidamState
        {
            Idle,       // ひま
            Working,    // 働いている
        }

        private enum MinidamSubState
        {
            IdleDoNothing,  // ひま / ぼーっとしている
            IdleBallooning, // ひま / シャボン玉吹いている
            WorkingEasy,    // 働いている / ゆっくり働いている
            WorkingNormal,  // 働いている / 普通に働いている
            WorkingHard,    // 働いている / 激しく働いている
            WorkingResting, // 働いている / 休憩中
        }

        private struct KeyFrameTransition
        {
            public int possibility; // 遷移確率 (100分率)
            public int frame;  // 遷移先のフレーム

            public KeyFrameTransition(int possibility, int frame)
            {
                this.possibility = possibility;
                this.frame = frame;
            }

            public KeyFrameTransition(int frame)
            {
                this.possibility = 100;
                this.frame = frame;
            }
        }

        private struct KeyFrameInformation
        {
            public int frame;
            public string image;
            public KeyFrameTransition[] transitions;

            public KeyFrameInformation(int frame, string image)
            {
                this.frame = frame;
                this.image = image;
                this.transitions = new KeyFrameTransition[]{};
            }

            public KeyFrameInformation(int frame, KeyFrameTransition[] transitions)
            {
                this.frame = frame;
                this.image = null;
                this.transitions = transitions;
            }

            public KeyFrameInformation(int frame, int nextFrame)
            {
                this.frame = frame;
                this.image = null;
                this.transitions = new KeyFrameTransition[] { new KeyFrameTransition(nextFrame) };
            }
        }

        private struct AnimationTransition
        {
            public int possibility; // 遷移確率
            public MinidamSubState subState;  // 遷移先の状態
            public AnimationTransition(int possibility, MinidamSubState subState)
            {
                this.possibility = possibility;
                this.subState = subState;
            }
        }

        private struct AnimationInformation
        {
            public MinidamSubState subState;
            public int durationFramesMin;
            public int durationFramesMax;
            public string talkingImage;
            public AnimationTransition[] transitions;
            public KeyFrameInformation[] keyFrames;

            public AnimationInformation(MinidamSubState subState, int durationFramesMin, int durationFramesMax, string talkingImage, AnimationTransition[] transitions, KeyFrameInformation[] keyFrames)
            {
                this.subState = subState;
                this.durationFramesMin = durationFramesMin;
                this.durationFramesMax = durationFramesMax;
                this.talkingImage = talkingImage;
                this.transitions = transitions;
                this.keyFrames = keyFrames;
            }
        }

        private static AnimationInformation[] animations = {
            // 暇なとき
            new AnimationInformation(
                MinidamSubState.IdleDoNothing,
                300, 600, "/images/minidam_talking03.png",
                new AnimationTransition[]{},
                new KeyFrameInformation[] {
                    new KeyFrameInformation(1, "/images/minidam_idle01.png"),
                    new KeyFrameInformation(31, "/images/minidam_idle02.png"),
                    new KeyFrameInformation(61, 1),
                }
            ),
            // 働いているとき
            new AnimationInformation(
                MinidamSubState.WorkingNormal,
                300, 600, "/images/minidam_talking01.png",
                new AnimationTransition[]{},
                new KeyFrameInformation[] {
                    new KeyFrameInformation(1, "/images/minidam_work01.png"),
                    new KeyFrameInformation(16, "/images/minidam_work02.png"),
                    new KeyFrameInformation(31, 1),
                }
            ),
        };

        private MinidamState state;
        private MinidamSubState subState;
        private int frame;
        private int duration;
        private BalloonWindow balloonWindow;
        private System.Windows.Threading.DispatcherTimer timer;
        private ModifierKeys lastModifiers;
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

        public MainWindow()
        {
            InitializeComponent();
            // デザイナのプロパティをこのオブジェクトにバインド
            this.DataContext = this;
            this.menuTopmostChecked = true;
            // 画面右下に表示
            Top = System.Windows.SystemParameters.WorkArea.Top
                + System.Windows.SystemParameters.WorkArea.Height
                - Height;
            Left = System.Windows.SystemParameters.WorkArea.Left
                + System.Windows.SystemParameters.WorkArea.Width
                - Width;

            balloonWindow = new BalloonWindow();
            balloonWindow.Visibility = Visibility.Hidden;

            InitNotifyIcon();

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(33.3);
            timer.Tick += timer_Tick;
            timer.Start();

            StopWork();
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
            this.ContextMenu.IsOpen = true;
        }

        private void MenuItemQuit_Click(object sender, RoutedEventArgs e)
        {
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
            if(notifyIcon != null)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
            if (balloonWindow != null)
            {
                balloonWindow.Close();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            CheckModifierKeys();

            foreach (AnimationInformation info in animations)
            {
                if(info.subState != subState)
                {
                    continue;
                }
                if(balloonWindow.Visibility != Visibility.Hidden)
                {
                    imageFile = info.talkingImage;
                    break;
                }

                // 通常のフレーム再生
                if(frame == 0)
                {
                    // フレーム　0 は特別な扱い。
                    // 継続時間を決定する。
                    duration = random.Next(info.durationFramesMin, info.durationFramesMax);
                    frame = 1;
                }
                if(--duration <= 0)
                {
                    // TODO
                }

                while (!ProcessFrame(info)) ;
                ++frame;
            }
        }

        private bool ProcessFrame(AnimationInformation info)
        {
            foreach(KeyFrameInformation keyInfo in info.keyFrames)
            {
                if(keyInfo.frame < frame)
                {
                    continue;
                }
                else if(keyInfo.frame > frame)
                {
                    break;
                }
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
                    imageFile = keyInfo.image;
                }
                break;
            }
            return true;
        }

        private void CheckModifierKeys()
        {
            ModifierKeys curModifier = Keyboard.Modifiers & (
                ModifierKeys.Control
                | ModifierKeys.Shift
                | ModifierKeys.Alt
            );
            if (curModifier == lastModifiers)
            {
                return;
            }
            lastModifiers = curModifier;
            List<string> messages = new List<string>();
            if((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                messages.Add("Shift");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                messages.Add("Control");
            }
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            {
                messages.Add("Alt");
            }
            if(messages.Count <= 0)
            {
                balloonWindow.Visibility = Visibility.Hidden;
                balloonWindow.message = "";
                return;
            }
            balloonWindow.message = string.Join("\n", messages);
            balloonWindow.Show();
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
            state = MinidamState.Working;
            subState = MinidamSubState.WorkingNormal;
            frame = 0;
        }

        private void StopWork()
        {
            state = MinidamState.Idle;
            subState = MinidamSubState.IdleDoNothing;
            frame = 0;
        }
    }
}
