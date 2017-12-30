namespace HVCC.Shell.Views
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.ViewModels;
    using DevExpress.Xpf.Printing;
    using HVCC.Shell.Models;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common.Interfaces;
    using System.ComponentModel;


    /// <summary>
    /// Interaction logic for WellMeterReadingsView.xaml
    /// </summary>
    public partial class WellMeterReadingsView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public WellMeterReadingsView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;

            this.ViewModel.Table = this.tableViewWellHistory;
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
           this.gridTableView.Focus();
        }

        private void wellMeterReadingGrid_Initialized(object sender, System.EventArgs e)
        {
            wellMeterReadingGrid.CurrentColumn = wellMeterReadingGrid.Columns.GetColumnByFieldName("MeterReading");
            gridTableView.FocusedRowHandle = 0;
        }

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


        // DO-DO: Converto to ViewModel Commands......
        #region Report Button Events
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
    }
}
