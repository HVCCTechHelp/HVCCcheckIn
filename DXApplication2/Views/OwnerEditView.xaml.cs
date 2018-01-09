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
    /// Interaction logic for PropertyEditDialogView.xaml
    /// </summary>
    public partial class OwnerEditView : UserControl, IView
    {
        //ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public OwnerEditView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            //this.Loaded += OnLoaded;
            //appPermissions = Host.Instance.AppPermissions as ApplicationPermission;
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
            this.tableViewDetail.PostEditor();
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckIn_Clicked(object sender, RoutedEventArgs e)
        {
            // TO-DO : convert to a setable property in the ViewModel....
            this.lgCheckIn.IsCollapsed = true;
            this.lgCheckIn.Visibility = Visibility.Hidden;
        }

        private void checkin_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (this.sePoolMembers.Value > 0 ||
                this.sePoolGuests.Value > 0 ||
                this.seGolfMembers.Value > 0 ||
                this.seGolfGuests.Value > 0)
            {
                this.btnCheckIn.IsEnabled = true;
            }
            else
            {
                this.btnCheckIn.IsEnabled = false;
            }
        }

            /// <summary>
            /// Executed when the View is loaded
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void EditOwner_Loaded(object sender, RoutedEventArgs e)
        {
            // Force an update on the controls that require validation.  This will force
            // them to be invalid if data is missing.
            //teOwnerMailTo.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            ////teOwnerLName.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            //teOwnerAddress.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            //teOwnerCity.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            //teOwnerState.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();
            //teOwnerZip.GetBindingExpression(DevExpress.Xpf.Editors.TextEdit.EditValueProperty).UpdateSource();

            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void imgEdit_Drop(object sender, DragEventArgs e)
        {

        }
    }
}

