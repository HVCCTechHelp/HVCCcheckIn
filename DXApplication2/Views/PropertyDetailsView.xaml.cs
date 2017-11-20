﻿namespace HVCC.Shell.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Models;
    using HVCC.Shell.ViewModels;
    using DevExpress.Mvvm;
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Docking;

    /// <summary>
    /// Interaction logic for PropertyDetailView.xaml
    /// </summary>
    public partial class PropertyDetailsView : UserControl
    {
        PropertiesViewModel vm = null; // new PropertiesViewModel();

        public PropertyDetailsView()
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
            //// Bind the properties of the view  to the properties of the view model.  
            //// The properties are INotify, so when one changes it registers a PropertyChange
            //// event on the other.  Also note, this code must reside outside of the
            //// constructor or a XAML error will be thrown.
            vm = this.DataContext as PropertiesViewModel;

            // Assign the grid view for Export and Printing
            vm.GridTableView = this.propertiesView;

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
        //protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        //case "PropertiesList":
        //        //case "SelectedProperty":
        //        case "DataUpdated":
        //            Helper.UpdateCaption(vm.ActiveDocPanel, vm.IsDirty);
        //            break;
        //        default:
        //            break;
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        object expandedRow;
        private void grid_MasterRowExpanded(object sender, RowEventArgs e)
        {
            //// When another row is expanded, the previous row is collapsed.
            if (expandedRow != null)
            {
                var expandedRowHandle = propertyGrid.FindRow(expandedRow);
                propertyGrid.CollapseMasterRow(expandedRowHandle);
            }
            expandedRow = e.Row;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void view_ValidateRow(object sender, GridRowValidationEventArgs e)
        {
            if (!e.IsValid)
            {
                e.IsValid = false;
                e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                e.ErrorContent = "The highlighted field cannot be blank";
                e.SetError(e.ErrorContent);
            }
            Helper.UpdateCaption(vm.ActiveDocPanel, vm.IsDirty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableView_RowDoubleClick(object sender, RowDoubleClickEventArgs e)
        {
            UICommand results = vm.ShowPropertyDialog((Property)propertyGrid.SelectedItem);
        }
    }
}
