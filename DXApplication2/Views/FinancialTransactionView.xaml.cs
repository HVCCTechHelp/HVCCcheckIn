namespace HVCC.Shell.Views
{
    using DevExpress.Xpf.Editors;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common.Interfaces;
    using Models;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for FinancialTransactionView.xaml
    /// </summary>
    public partial class FinancialTransactionView : UserControl, IView
    {
        //ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public FinancialTransactionView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;
            //appPermissions = Host.Instance.AppPermissions as ApplicationPermission;
            EventManager.RegisterClassHandler(typeof(TextEdit), FrameworkElement.GotFocusEvent, 
                new RoutedEventHandler((s, e) =>
                {
                    TextEdit editor = (TextEdit)s;
                    editor.Dispatcher.BeginInvoke(new SimpleDelegate(editor.SelectAll));
                }));
        }

        delegate void SimpleDelegate();

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
            this.tePayment.Focus();
        }

        private void FinancialTransactionView_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        //protected void PropertyViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        case "<property>":
        //            break;
        //        default:
        //            break;
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_CellValueChanging(object sender, CellValueChangedEventArgs e)
        {
            this.tableViewTransactions.PostEditor();
        }

        private void TextEdit_LostFocus(object sender, RoutedEventArgs e)
        {
            /// Force the grid to update to reflect the amount of the payment applied to an invoice.
            this.paymentGrid.RefreshData();
        }

        private void lgPayment_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                this.tePayment.Focus();
                Keyboard.Focus(tePayment);
            }
        }
    }
}
