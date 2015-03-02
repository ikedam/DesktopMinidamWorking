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
            Idle,       // ひま
            Working,    // 働いている
        }

        private MinidamState state;
        private AnimationState animationState;
        private string animationImage;
        private string talkingImage;
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

            if (balloonWindow.Visibility != Visibility.Hidden)
            {
                // しゃべっている間の制御は行わない
                return;
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
            }
            if(--duration <= 0)
            {
                // TODO
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
                    imageFile = animationImage = keyInfo.image;
                }
            }
            return true;
        }

        private void CheckModifierKeys()
        {
            ModifierKeys curModifier = (talkingImage != null)?(Keyboard.Modifiers & (
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
                imageFile = animationImage;
                return;
            }
            imageFile = talkingImage;
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
            animationState = AnimationState.WorkingNormal;
            frame = 0;
        }

        private void StopWork()
        {
            state = MinidamState.Idle;
            animationState = AnimationState.IdleDoNothing;
            frame = 0;
        }
    }
}
