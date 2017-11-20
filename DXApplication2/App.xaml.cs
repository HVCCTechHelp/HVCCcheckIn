namespace HVCC.Shell
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
    using System.Windows;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnAppStartup_UpdateThemeName(object sender, StartupEventArgs e)
        {
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();
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
