namespace HVCC.Shell.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Models;
    using HVCC.Shell.Validation;
    using HVCC.Shell.ViewModels;
    using DevExpress.Xpf.Grid;

    /// <summary>
    /// Interaction logic for ChangeOwnerView.xaml
    /// </summary>
    public partial class ChangeOwnerView : UserControl
    {
        PropertiesViewModel vm = null; // new PropertiesViewModel();

        public ChangeOwnerView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }


        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            vm = this.DataContext as PropertiesViewModel; 

            //// Bind the properties of the view  to the properties of the FileInformation view model.  
            //// The properties are INotify, so when one changes it registers a PropertyChange
            //// event on the other.  Also note, this code must reside outside of the
            //// constructor or a XAML error will be thrown.
            if (null != vm)
            {
                vm.PropertyChanged +=
                    new System.ComponentModel.PropertyChangedEventHandler(this.PropertiesViewModel_PropertyChanged);
            }
        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "OwnerUpdated":
                    this.btnCommit.IsEnabled = false;
                    Helper.UpdateCaption(vm.ActiveDocPanel, true);
                    break;
                default:
                    break;
            }
            Helper.UpdateCaption(vm.ActiveDocPanel, vm.IsDirty);
        }

        /// <summary>
        /// Executes when the TableView is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void relationshipsTableView_Loaded(object sender, RoutedEventArgs e)
        {
            this.vm.PopulateRelationshipsToProcess();
            Helper.UpdateCaption(vm.ActiveDocPanel, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TableView_ValidateRow(object sender, GridRowValidationEventArgs e)
        {
            Relationship row = e.Row as Relationship;
            if (null != row)
            {
                string results = ValidationRules.IsRelationshipRowValid(row);
                if (!String.IsNullOrEmpty(results))
                {
                    e.IsValid = false;
                    e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                    e.ErrorContent = "The highlighted field cannot be blank";
                    e.SetError(e.ErrorContent);
                }
                else
                {
                    row.Photo = vm.ApplDefault.Photo; //PropertiesViewModel.DefaultBitmapImage;
                    row.Image = Helper.ArrayToBitmapImage(row.Photo.ToArray());
                    row.PropertyID = vm.SelectedProperty.PropertyID;
                    e.IsValid = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TableView_InvalidRowException(object sender, InvalidRowExceptionEventArgs e)
        {
            e.ExceptionMode = ExceptionMode.NoAction;
            e.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TableView_RowUpdated(object sender, RowEventArgs e)
        {
            Helper.UpdateCaption(vm.ActiveDocPanel, true);
            e.Handled = true;
        }
    }
}
