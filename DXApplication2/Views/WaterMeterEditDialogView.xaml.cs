namespace HVCC.Shell.Views
{
    using DevExpress.Xpf.Editors.Validation;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.ViewModels;
    using Models;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using Validation;

    /// <summary>
    /// Interaction logic for WaterSystemEditDialogView.xaml
    /// </summary>
    public partial class WaterSystemEditDialogView : UserControl
    {
        public WaterSystemEditDialogView()
        {
            InitializeComponent();
            //this.Loaded += OnLoaded;
        }

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        //private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        //{
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtMeterNumber_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            WaterMeterViewModel vm = this.DataContext as WaterMeterViewModel;
            // It is plusuable to assume that the water meter number may get entered incorrectly, or
            // need to be replaced at some point.  Therefore, I've put in a hot-key <LeftCtrl> that
            // will enable editing of this control. Otherwise the control remains read-only.
            if (e.Key == System.Windows.Input.Key.LeftCtrl && vm.ApplPermissions.CanEditWater)
            {
                object resource = TryFindResource("TextEditEditStyle");
                this.txtMeterNumber.Style = (Style)resource;
                this.txtMeterNumber.IsReadOnly = false;
            }
        }

        /// <summary>
        /// Validates a grid row 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_ValidateRow(object sender, DevExpress.Xpf.Grid.GridRowValidationEventArgs e)
        {
            WaterMeterViewModel vm = this.DataContext as WaterMeterViewModel;
            WaterMeterReading row = e.Row as WaterMeterReading;

            try
            {
                if (null != row)
                {
                    // Rule-1:  All required fields must contain data
                    string results = ValidationRules.IsWaterMeterReadingRowValid((WaterMeterReading)e.Row);
                    if (!String.IsNullOrEmpty(results))
                    {
                        e.IsValid = false;
                        e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                        e.ErrorContent = "The highlighted field cannot be blank";
                        e.SetError(e.ErrorContent);
                        return;
                    }
                    // Rule-2: Meter Reading must be greater than previous reading.
                    if (row.ReadingDate <= vm.SelectedProperty.LastMeterEntry.ReadingDate ||
                        row.MeterReading <= vm.SelectedProperty.LastMeterEntry.MeterReading)
                    {
                        e.IsValid = false;
                        e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                        e.ErrorContent = "The highlighted field cannot be less then the previous reading";
                        e.SetError(e.ErrorContent);
                        return;
                    }
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
                    row.Consumption = vm.CalculateWaterConsumption();
                    row.PropertyID = vm.SelectedProperty.PropertyID; // assign the PropertID since it's a FK
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_RowUpdated(object sender, RowEventArgs e)
        {
            e.Handled = true;
        }
    }
}
