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
    /// Interaction logic for PostPaymentViw.xaml
    /// </summary>
    public partial class FinancialTransactionView : UserControl, IView
    {
        ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public FinancialTransactionView(IViewModel vm)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editform_Validate(object sender, ValidationEventArgs e)
        {
            //// The ImageEdit control doesn't validate the image by default, therefore
            //// we manually validate the control here so validation is passed.
            if (sender is ImageEdit)
            {
                if (((ImageEdit)sender).HasImage)
                {
                    e.IsValid = true;
                }
                else
                {
                    e.IsValid = false;
                }
            }
            // Validate the RelationToOwner ComboBox
            if (sender is ComboBoxEdit)
            {
                if (string.IsNullOrEmpty((string)e.Value))
                {
                    e.IsValid = false;
                }
                else
                {
                    e.IsValid = true;
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Executed when the View is loaded. Force an update on the controls that require validation.  
        /// This will force them to fail validation if data is missing or invalid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FinancialTransaction_Loaded(object sender, RoutedEventArgs e)
        {
            // Force an update on the controls that require validation.  This will force
            // them to be invalid if data is missing.
            teCredit.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            teDebit.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            ceFiscalYear.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            deTransactionDate.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            ceCreditMethod.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            ceDebitMethod.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            teCheck.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            teReceipt.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            teTotal.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            teComment.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();

            deTransactionDate.Focus();
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void teCredit_GotFocus(object sender, RoutedEventArgs e)
        {
            teCredit.NullText = string.Empty;
            teDebit.Visibility = Visibility.Hidden;
            lbDebit.Visibility = Visibility.Hidden;
            lbDebitMethod.Visibility = Visibility.Hidden;
            ceDebitMethod.Visibility = Visibility.Hidden;
            lbOR.Visibility = Visibility.Hidden;
        }

        private void teDebit_GotFocus(object sender, RoutedEventArgs e)
        {
            teDebit.NullText = string.Empty;
            teCredit.Visibility = Visibility.Hidden;
            lbCredit.Visibility = Visibility.Hidden;
            lbCreditMethod.Visibility = Visibility.Hidden;
            ceCreditMethod.Visibility = Visibility.Hidden;
            lbCheck.Visibility = Visibility.Hidden;
            lbReceipt.Visibility = Visibility.Hidden;
            teCheck.Visibility = Visibility.Hidden;
            teReceipt.Visibility = Visibility.Hidden;
            lbOR.Visibility = Visibility.Hidden;
            //ceGolfCart.Visibility = Visibility.Hidden;
            //ceWaterReconnect.Visibility = Visibility.Hidden;
            //cePoolAssessment.Visibility = Visibility.Hidden;
            //ceWaterReconnect.Visibility = Visibility.Hidden;
        }

        private void ceFiscalYear_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
            UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;

            if (keyboardFocus != null)
            {
                keyboardFocus.MoveFocus(tRequest);
            }
        }
    }
}
