namespace HVCC.Shell.ViewModels
{
    using System;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Common.ViewModels;
    using System.Windows.Input;
    using HVCC.Shell.Resources;

    public partial class MainViewModel : CommonViewModel
    {
        //private void RegisterAboutHandler()
        //{
        //    this.RegisterCommand(
        //        CustomCommands.About,
        //        param => this.CanAboutExecute,
        //        param => this.AboutExecute());
        //}

        //private bool CanAboutExecute
        //{
        //    get
        //    {
        //        return true;
        //    }
        //}

        //private void AboutExecute()
        //{
        //    this.RaiseCommandExecuting(new CommandExecutingEventArgs(CustomCommands.About));

        //    System.Windows.MessageBox.Show(string.Format("CleanAir.exe\nversion {0}", CurrentVersion), "About", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

        //    this.RaiseCommandExecuted(new CommandExecutedEventArgs(CustomCommands.About, MessageType.None, string.Empty));
        //}

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
