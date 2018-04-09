namespace HVCC.Shell.Views
{
    using DevExpress.Xpf.Editors;
    using Models;
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Common.Interfaces;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for ReportCarouselView.xaml
    /// </summary>
    public partial class ReportCarouselView : UserControl, IView
    {
        ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public ReportCarouselView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            //this.Loaded += OnLoaded;
            appPermissions = Host.Instance.AppPermissions as ApplicationPermission;
        }
        public object SaveState()
        {
            //throw new NotImplementedException();
            return null;
        }

        public void RestoreState(object state)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
        }

    }
}
