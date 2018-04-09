namespace HVCC.Shell.Views
{
    using HVCC.Shell.Common.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for FacilityUsageGraph.xaml
    /// </summary>
    public partial class FacilityUsageGraphView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public FacilityUsageGraphView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;

            //this.ViewModel.Table = this.tableViewMembers;
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

        private void Administration_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }

}
