namespace HVCC.Shell.ViewModels
{
    using System.Windows.Input;
    using Common;
    using HVCC.Shell.Common.Interfaces;

    internal partial class MainViewModel
    {
        private void RegisterHelpHandler()
        {
            this.RegisterCommand(
                ApplicationCommands.Help,
                param => this.CanHelpExecute,
                param => this.HelpExecute());
        }

        private bool CanHelpExecute
        {
            get
            {
                return true;
            }
        }

        private void HelpExecute()
        {
            // TO-DO: ???
            //this.RaiseCommandExecuting(new CommandExecutingEventArgs(ApplicationCommands.Help));
            //var focusedElement = FocusManager.GetFocusedElement(this.View);
            //var focusedDependencyObject = focusedElement as System.Windows.DependencyObject;
            //if (null == focusedDependencyObject)
            //{
            //    focusedDependencyObject = this.View;
            //}

            //IHelpContext helpContext = FindHelpContext(focusedDependencyObject);
            //int? helpId = null == helpContext ? (int?)null : helpContext.HelpId;

            //////System.Windows.MessageBox.Show(string.Format("Help({0}) w/helpContext = {1}", this.View.GetType().ToString(), helpId));
            //this.RaiseCommandExecuted(new CommandExecutedEventArgs(ApplicationCommands.Help, MessageType.None, string.Empty));

            //IViewModelLaunchingPad launchingPad = PlugInManager.Instance.LaunchingPad;
            //if (null != launchingPad)
            //{
            //    launchingPad.OpenHelpWindow(helpId);
            //}
        }

        public static IHelpContext FindHelpContext(System.Windows.DependencyObject element)
        {
            IHelpContext helpContext = element as IHelpContext;

            while (null == helpContext && null != element)
            {
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);

                if (element is IHelpContext)
                {
                    helpContext = element as IHelpContext;
                    break;
                }
                else if (element is System.Windows.FrameworkElement)
                {
                    System.Windows.FrameworkElement frameworkElement = element as System.Windows.FrameworkElement;
                    helpContext = frameworkElement.DataContext as IHelpContext;
                }
            }

            return helpContext;
        }
    }
}
