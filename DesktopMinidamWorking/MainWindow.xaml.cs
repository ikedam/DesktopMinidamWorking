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
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

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

        public MainWindow()
        {
            InitializeComponent();
            // デザイナのプロパティをこのオブジェクトにバインド
            this.ContextMenu.DataContext = this;
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
        }

        private void InitNotifyIcon()
        {
            // タスクトレイに表示
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Text = Title;

            // Pack URIs in WPF
            // https://msdn.microsoft.com/en-us/library/aa970069%28v=vs.110%29.aspx 
            System.IO.Stream s = Application.GetResourceStream(new Uri("pack://application:,,,/images/icon.ico")).Stream;
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
    }
}
