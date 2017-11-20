namespace HVCC.Shell.ViewModels
{
    using System;
    using HVCC.Shell.Common.Commands;

    internal partial class MainViewModel
    {
        private void RegisterAboutHandler()
        {
            this.RegisterCommand(
                CustomCommands.About,
                param => this.CanAboutExecute,
                param => this.AboutExecute());
        }

        private bool CanAboutExecute
        {
            get
            {
                return true;
            }
        }

        private void AboutExecute()
        {
            this.RaiseCommandExecuting(new CommandExecutingEventArgs(CustomCommands.About));

            System.Windows.MessageBox.Show(string.Format("CleanAir.exe\nversion {0}", CurrentVersion), "About", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            this.RaiseCommandExecuted(new CommandExecutedEventArgs(CustomCommands.About, MessageType.None, string.Empty));
        }

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
    }
}
