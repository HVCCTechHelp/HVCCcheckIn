namespace HVCC.Shell.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Models;
    using HVCC.Shell.ViewModels;
    using DevExpress.Xpf.Grid;
    using System;
    using Validation;
    using DevExpress.Xpf.Bars;
    using HVCC.Shell.Common.Interfaces;

    /// <summary>
    /// Interaction logic for WaterSystemView.xaml
    /// </summary>
    public partial class WaterMeterView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public WaterMeterView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;

            this.ViewModel.Table = this.propertiesTableView;
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
        //protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        case "Property":
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
            PropertiesViewModel vm = this.DataContext as PropertiesViewModel;

            Property row = e.Row as Property;
            //if (null != row)
            if (!e.IsValid)
            {
                e.IsValid = false;
                e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                e.ErrorContent = "The highlighted field cannot be blank";
                e.SetError(e.ErrorContent);
            }
        }

    }
}
