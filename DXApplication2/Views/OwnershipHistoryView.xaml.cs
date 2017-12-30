namespace HVCC.Shell.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Models;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common.Interfaces;

    /// <summary>
    /// Interaction logic for OwnershipChangesView.xaml
    /// </summary>
    public partial class OwnershipHistoryView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public OwnershipHistoryView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;
            //INotifyPropertyChanged pc = vm as INotifyPropertyChanged;
            //if (null != pc)
            //{
            //    pc.PropertyChanged += Pc_PropertyChanged;
            //}

            this.ViewModel.Table = this.tableViewOwnershipHistory;
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
