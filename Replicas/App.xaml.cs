using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Replicas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool okToLaunch = true;
            string title = Jbh.AppManager.AppName;
            if (string.IsNullOrWhiteSpace(Jbh.AppManager.DataPath))
            {
                MessageBox.Show("The folder for the " + title + " application data within the Jbh.Info\\AppData directory has not been found.\n\nThe application will now be shut down.", title, MessageBoxButton.OK, MessageBoxImage.Error);
                okToLaunch = false;
            }
            else
            {
                DateTime? launched = Jbh.AppManager.RunStart;
                if (launched.HasValue)
                {

                    MessageBoxResult answer = MessageBox.Show(title + " appears to be already running.\n\nLaunched at " + launched.Value.ToShortTimeString() + " on " + launched.Value.ToShortDateString() + "\n\nOnly continue if you are sure that " + title + " is not currently running.\n\nContinue?", title, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (answer != MessageBoxResult.OK)
                    {
                        okToLaunch = false;
                    }
                }
            }
            if (okToLaunch)
            {
                Jbh.AppManager.SetRunStart();
                MainWindow w = new MainWindow();
                w.Show();
            }
            else
            {
                Current.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Jbh.AppManager.DeleteRuntimeFile();
        }
    }
}