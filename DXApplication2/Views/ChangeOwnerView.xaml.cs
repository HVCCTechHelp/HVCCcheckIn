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
    using HVCC.Shell.Common.Interfaces;
    using System.Data.Linq;

    /// <summary>
    /// Interaction logic for ChangeOwnerView.xaml
    /// </summary>
    public partial class ChangeOwnerView : UserControl, IView
    {
        ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public ChangeOwnerView(IViewModel vm)
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
            //ChangeOwnerViewModel vm = this.DataContext as ChangeOwnerViewModel; 

            //if (null != vm)
            //{
            //    vm.PropertyChanged +=
            //        new System.ComponentModel.PropertyChangedEventHandler(this.PropertiesViewModel_PropertyChanged);
            //}
        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //switch (e.PropertyName)
            //{
            //    case "<property>":
            //        break;
            //    default:
            //        break;
            //}
        }

        /// <summary>
        /// Executes when the TableView is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void relationshipsTableView_Loaded(object sender, RoutedEventArgs e)
        {
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
                    // TO-DO: this work needs to go into the view model
                    //row.Photo = vm.ApplDefault.Photo; //PropertiesViewModel.DefaultBitmapImage;
                    //row.Image = Helper.ArrayToBitmapImage(row.Photo.ToArray());
                    //row.PropertyID = vm.SelectedProperty.PropertyID;
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
            e.Handled = true;
        }
    }
}
