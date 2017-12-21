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

        public static int expandedRow = 0;
        /// <summary>
        /// Executes when a Master grid expands the detail grid.  When this happens, the previous master row
        /// is collapsed before expanding the newly selected master row.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ownerGrid_MasterRowExpanded(object sender, RowEventArgs e)
        {
            if (0 < expandedRow)
            {
                ownerGrid.CollapseMasterRow(expandedRow);
            }

            // Set focus on the first row (after the 'click to add new row') of the detail grid
            expandedRow = e.RowHandle;
            tableViewOwners.FocusedRowHandle = expandedRow;
            var detailGridControl = ownerGrid.GetDetail(e.RowHandle) as GridControl;
            var rowHandle = detailGridControl.GetRowHandleByVisibleIndex(0);
            detailGridControl.View.FocusedRowHandle = rowHandle;
        }
    }
}
