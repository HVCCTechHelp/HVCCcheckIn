namespace HVCC.Shell.Views
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.ViewModels;
    using DevExpress.Xpf.Printing;


    /// <summary>
    /// Interaction logic for WellMeterReadingsView.xaml
    /// </summary>
    public partial class WellMeterReadingsView : UserControl
    {
        WaterWellViewModel vm = null;
        PropertiesViewModel pvm = null;

        public WellMeterReadingsView()
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
            // When this view is loaded we associate the ViewModel. On the first load, the data context
            // is set to the parent VM. We capture that VM, and reset the view's VM to the associated VM.
            // This is just done on the first load. Otherwise, we associated the VM to the existing VM object.
            if (this.DataContext is WaterWellViewModel)
            {
                // ViewModel (vm) and ParentViewModel (pvm) is already assigned
                // When this view becomes active, we assign the primary gridTable view for exporing.
                pvm.GridTableView = this.gridTableView2;
            }
            else
            {
                pvm = this.DataContext as PropertiesViewModel;
                this.DataContext = new WaterWellViewModel();

                pvm.ViewModels.Add(this.DataContext);
                vm = this.DataContext as WaterWellViewModel;
                vm.ParentViewModel = pvm;
                pvm.GridTableView = this.gridTableView2;
            }
            this.gridTableView.Focus();
        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        //protected void WaterWellViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        default:
        //            break;
        //    }
        //    Helper.UpdateCaption(vm.ActiveDocPanel, vm.IsDirty);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GridColumn_Validate(object sender, DevExpress.Xpf.Grid.GridCellValidationEventArgs e)
        {
            object o = e;
            int i = 0;
            bool isValidValue = System.Int32.TryParse(e.Value.ToString(), out i);
            if (isValidValue)
            {
                vm.MeterReadingToGallons(sender, e);

                e.IsValid = true;
                e.Handled = true;
            }
            else
            {
                e.IsValid = false;
                e.Handled = true;
            }

        }

        private void gridTableView_CellValueChanged(object sender, DevExpress.Xpf.Grid.CellValueChangedEventArgs e)
        {
            if (wellMeterReadingGrid.IsValidRowHandle(e.RowHandle + 1))
            {
                wellMeterReadingGrid.CurrentColumn = wellMeterReadingGrid.Columns.GetColumnByFieldName("MeterReading");
                gridTableView.FocusedRowHandle = e.RowHandle + 1;
            }
            else
            {
                btnSave.Focus();
            }
            e.Handled = true;
        }

        #region Report Button Events
        Dictionary<int, int> daysInMonth;
        private Dictionary<int, int> PopulateDates()
        {
            Dictionary<int, int> daysInMonth = new Dictionary<int, int>();
            daysInMonth.Add(1, 31);
            daysInMonth.Add(2, 28);
            daysInMonth.Add(3, 31);
            daysInMonth.Add(4, 30);
            daysInMonth.Add(5, 31);
            daysInMonth.Add(6, 30);
            daysInMonth.Add(7, 31);
            daysInMonth.Add(8, 31);
            daysInMonth.Add(9, 30);
            daysInMonth.Add(10, 31);
            daysInMonth.Add(11, 30);
            daysInMonth.Add(12, 31);
            return daysInMonth;
        }

        private void bb_DetailReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //Dictionary<int, int> daysInMonth = PopulateDates();
            //int year = DateTime.Now.Year;
            //int mon = DateTime.Now.Month;
            //DateTime start = new DateTime(year, mon, 1, 12, 0, 0);
            //DateTime end = new DateTime(year, mon, daysInMonth[mon], 12, 0, 0);
            //report.Parameters["StartDate"].Value = start;
            //report.Parameters["EndDate"].Value = end;

            Reports.WellMeterDetailReport report = new Reports.WellMeterDetailReport();
            PrintHelper.ShowPrintPreview(this, report);
        }

        private void bb_SummaryReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Reports.WellMeterSummaryReport report = new Reports.WellMeterSummaryReport();
            PrintHelper.ShowPrintPreview(this, report);
        }
        #endregion // Bar Button Events

        private void wellMeterReadingGrid_Initialized(object sender, System.EventArgs e)
        {
            wellMeterReadingGrid.CurrentColumn = wellMeterReadingGrid.Columns.GetColumnByFieldName("MeterReading");
            gridTableView.FocusedRowHandle = 0;

        }
    }
}
