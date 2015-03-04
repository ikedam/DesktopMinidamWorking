using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DesktopMinidamWorking
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private System.Threading.Mutex mutex;

        public App()
            : base()
        {
            mutex = new System.Threading.Mutex(false, "MutexForMinidamWorking");
            if(!mutex.WaitOne(0))
            {
                // すでに起動している
                Shutdown();
            }
        }

        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if(MainWindow != null)
            {
                (MainWindow as MainWindow).Application_SessionEnding(sender, e);
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if(MainWindow != null && MainWindow.IsVisible)
            {
                MainWindow.Close();
            }
        }
    }
}
