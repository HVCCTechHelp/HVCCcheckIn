namespace HVCC.Shell.ViewModels
{
    using System;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Common.ViewModels;
    using System.Windows.Input;
    using HVCC.Shell.Resources;
    using DevExpress.Mvvm;

    public partial class MainViewModel : ViewModelBase
    {

        internal static string CurrentVersion
        {
            get
            {
                try
                {
                    return System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed
                           ? System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                           : "<Unpublished>";
                }
                catch (Exception)
                {
                    return "<error>";
                }
            }
        }

        /// <summary>
        /// About Commaind
        /// </summary>
        private ICommand _aboutCommand;
        public ICommand AboutCommand
        {
            get
            {
                return _aboutCommand ?? (_aboutCommand = new CommandHandler(() => AboutAction(), true));
            }
        }

        public void AboutAction()  // AboutCommand
        {
            //System.Windows.MessageBox.Show(string.Format("HVCC.exe\nversion {0}\n\nWritten By: Andy Tudhope\nHVCCTechHelp@gmail.com\n(c)2017\n\nUser Role: {1}", CurrentVersion, this.DBRole.ToString()), "About",
            System.Windows.MessageBox.Show(string.Format("HVCC.exe\nversion {0}\n\nWritten By: Andy Tudhope\nHVCCTechHelp@gmail.com\n(c)2017\n\nUser Role: {1}", CurrentVersion, "DBROle"), "About",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

    }
}
