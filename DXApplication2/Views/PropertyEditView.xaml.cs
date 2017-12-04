namespace HVCC.Shell.Views
{
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Editors;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.ViewModels;
    using Models;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Linq;
    using System.IO;
    using HVCC.Shell.Common.Interfaces;
    using System.Windows.Media.Imaging;


    /// <summary>
    /// Interaction logic for PropertyEditDialogView.xaml
    /// </summary>
    public partial class PropertyEditView : UserControl, IView
    {
        ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public PropertyEditView(IViewModel vm)
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
        private void Te_Validate(object sender, ValidationEventArgs e)
        {
            string input = e.Value as string;
            if (string.IsNullOrEmpty(input))
            {
                e.IsValid = false;
            }
            else
            {
                e.IsValid = true;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Validates a grid row 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_ValidateRow(object sender, DevExpress.Xpf.Grid.GridRowValidationEventArgs e)
        {
            Relationship row = e.Row as Relationship;

            try
            {
                if (null != row)
                {
                    // TO-DO:  <?> Add validation rules for Relationships
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Row Error: " + ex.Message);
            }
            finally
            {
                if (e.IsValid)
                {
                    // TO-DO:  ?
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Cancels addition of new row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_RowCanceled(object sender, RowEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Grid row updated event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_RowUpdated(object sender, RowEventArgs e)
        {
            Relationship row = e.Row as Relationship;
            if (null == row.Active)
            {
                // TO-DO: make assignments here.....
                //vm.AssignDefaultValues(row);
            }
            e.Handled = true;
        }

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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void teParcel_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //// It is plusuable to assume that the parcel number may get entered incorrectly, or
            //// need to be replaced at some point.  Therefore, I've put in a hot-key <LeftCtrl> that
            //// will enable editing of this control. Otherwise the control remains read-only.
            //if (e.Key == System.Windows.Input.Key.LeftCtrl && Host.Instance.AppPermissions.CanEditPropertyInfo)
            //{
            //    object resource = TryFindResource("TextEditEditStyle");
            //    this.teParcel.Style = (Style)resource;
            //    this.teParcel.IsReadOnly = false;
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PropertyDialog_Unloaded(object sender, RoutedEventArgs e)
        {
            // TO-DO : This may no longer been necessary since the DockPanel close also disposes of the VM and other view states...

            this.btnCheckIn.IsEnabled = true; // set true before the window closes, otherwise the 'false' state is retained

            // TO-DO: disposition will now be handled through the MvvmBinder interface
            //try
            //{
            //    IDisposable disp = this.DataContext as IDisposable;
            //    if (null != disp)
            //    {
            //        disp.Dispose();
            //    };
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Dispose Error: " + ex.Message);
            //}

        }
    }
}
