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
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for ConvertOwners.xaml
    /// </summary>
    public partial class OwnersView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public OwnersView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;

            this.ViewModel.Table = this.tableViewOwners;
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

        //public static int expandedRow = 0;
        ///// <summary>
        ///// Executes when a Master grid expands the detail grid.  When this happens, the previous master row
        ///// is collapsed before expanding the newly selected master row.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void ownerGrid_MasterRowExpanded(object sender, RowEventArgs e)
        //{
        //    try
        //    {
        //        if (0 < expandedRow)
        //        {
        //            ownerGrid.CollapseMasterRow(expandedRow);
        //        }

        //        // Set focus on the first row (after the 'click to add new row') of the detail grid
        //        expandedRow = e.RowHandle;
        //        tableViewOwners.FocusedRowHandle = expandedRow;
        //        var detailGridControl = ownerGrid.GetDetail(e.RowHandle) as GridControl;
        //        var rowHandle = detailGridControl.GetRowHandleByVisibleIndex(0);
        //        detailGridControl.View.FocusedRowHandle = rowHandle;
        //    }
        //    catch
        //    {
        //        // Happens when use expanded, collapses, expands the same row...
        //        expandedRow = 0;
        //    }
        //}

        private void Owners_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void BFilterStatus_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            string tempStr = this.ownerGrid.FilterString;
            string statusFilterString = "[Balance] >= 150.00";
            // Only add it if it's not already there.
            if (!tempStr.Contains(statusFilterString) && !tempStr.Contains("(" + statusFilterString + ")"))
            {
                if (!string.IsNullOrEmpty(this.ownerGrid.FilterString))
                {
                    this.ownerGrid.FilterString = string.Format("{0} And ({1})", this.ownerGrid.FilterString, statusFilterString);
                }
                else
                {
                    this.ownerGrid.FilterString = statusFilterString;
                }
            }
        }
    }
}
