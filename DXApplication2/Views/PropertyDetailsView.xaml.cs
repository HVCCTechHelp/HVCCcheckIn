namespace HVCC.Shell.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Models;
    using HVCC.Shell.ViewModels;
    using DevExpress.Mvvm;
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Docking;
    using HVCC.Shell.Common.Interfaces;
    using System.Collections.Generic;
    using System;
    using DevExpress.Xpf.Printing;

    /// <summary>
    /// Interaction logic for PropertyDetailView.xaml
    /// </summary>
    public partial class PropertyDetailsView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public PropertyDetailsView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;

            this.ViewModel.Table = this.propertiesView;
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
        private void view_ValidateRow(object sender, GridRowValidationEventArgs e)
        {
            if (!e.IsValid)
            {
                e.IsValid = false;
                e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                e.ErrorContent = "The highlighted field cannot be blank";
                e.SetError(e.ErrorContent);
            }
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

        private void bb_PeriodReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Dictionary<int, int> daysInMonth = PopulateDates();
            int year = DateTime.Now.Year;
            int mon = DateTime.Now.Month;

            DateTime start = new DateTime(year, mon, 1, 12, 0, 0);
            DateTime end = new DateTime(year, mon, daysInMonth[mon], 12, 0, 0);

            Reports.FacilitiesUsageReport report = new Reports.FacilitiesUsageReport();
            report.Parameters["StartDate"].Value = start;
            report.Parameters["EndDate"].Value = end;
            PrintHelper.ShowPrintPreview(this, report);
        }

        private void bb_DailyReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            DateTime forDate = DateTime.Now;

            Reports.DailyDetailReport report = new Reports.DailyDetailReport();
            report.Parameters["ForDate"].Value = forDate;
            PrintHelper.ShowPrintPreview(this, report);
        }

        private void bb_NotesReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Dictionary<int, int> daysInMonth = PopulateDates();
            int year = DateTime.Now.Year;
            int mon = DateTime.Now.Month;

            DateTime start = new DateTime(year, mon, 1, 12, 0, 0);
            DateTime end = new DateTime(year, mon, daysInMonth[mon], 12, 0, 0);

            Reports.PropertyNotesReport report = new Reports.PropertyNotesReport();
            report.Parameters["FromDate"].Value = start;
            report.Parameters["ToDate"].Value = end;
            PrintHelper.ShowPrintPreview(this, report);
        }

        private void bb_BalanceReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Reports.BalancesDueReport report = new Reports.BalancesDueReport();
            PrintHelper.ShowPrintPreview(this, report);
        }

        private void bb_AnnualInvoiceReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            Reports.AnnuaInvoices report = new Reports.AnnuaInvoices();
            report.Parameters["selectedProperty"].Value = (this.propertyGrid.SelectedItem as Property).PropertyID;
            PrintHelper.ShowPrintPreview(this, report);
        }
        #endregion
    }
}
