namespace HVCC.Shell
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Windows;
    using HVCC.Shell;
    using HVCC.Shell.ViewModels;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnAppStartup(object sender, StartupEventArgs e)
        {
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();
            Window w = new MainWindow() { DataContext = new MainViewModel() };
            this.MainWindow = w;
            this.MainWindow.Show();
            this.MainWindow.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            this.Shutdown();
        }

        private static List<string> openDocuments = new List<string>();
        public static List<string> OpenDocuments
        {
            get
            {
                return openDocuments;
            }

            set
            {
                openDocuments = value;
            }
        }

    }
}
