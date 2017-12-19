namespace HVCC.Shell
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Docking;
    using DevExpress.Xpf.Ribbon;
    using HVCC.Shell.ViewModels;
    using Helpers;
    using Models;
    using HVCC.Shell.Common.Interfaces;
    using DevExpress.XtraReports;
    using DevExpress.Xpf.Printing;
    using System.ComponentModel;
    using HVCC.Shell.Common;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXRibbonWindow, IView
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
            Host.Instance.OpenMvvmBinders.CollectionChanged += OpenMvvmBinders_CollectionChanged;
        }

        /// <summary>
        /// Summary:
        ///     Event handler for MvvmBinder collection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenMvvmBinders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var newBinder in e.NewItems)
                {
                    if (newBinder is IMvvmBinder)
                    {
                        var v = (newBinder as IMvvmBinder).View;
                        INotifyPropertyChanged propChange = (newBinder as IMvvmBinder).ViewModel as INotifyPropertyChanged;
                        if (null != propChange)
                        {
                            propChange.PropertyChanged += ViewModel_PropertyChanged;
                        }

                        // The MainWindow has an MvvmBinder, but will not have a data context set.  Therefore,
                        // only Views tied to a Viewmodel/DataContext will be created as a DockPanel.
                        if (!v.ToString().Contains("MainWindow"))
                        {
                            this.CreateDockPanel(v);
                        }
                    }
                }
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var oldBinder in e.OldItems)
                {
                    if (oldBinder is IMvvmBinder)
                    {
                        var v = (oldBinder as IMvvmBinder).View;
                        this.CloseDockPanel(v);
                    }
                }
            }
        }

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // Check to make sure there is a connection to the database
            if (!Host.Instance.IsConnected)
            {
                MessageBox.Show("Cannot establish connection to the database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Summary:
        ///     ViewModel Property Changed event handler for view model property changes
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            try
            {
                switch (e.PropertyName)
                {
                    case "IsBusy":
                        Mouse.OverrideCursor = Cursors.Wait;
                        break;
                    case "IsNotBusy":
                        Mouse.OverrideCursor = Cursors.Arrow;
                        break;

                    // When data changes, we update the panel's caption with an astrict to indicate a dirty state.  Since
                    // property, relationship, and water share a symbiotic data relationship, if anyone of the property related
                    // documents panel data change, we update them all.  This leaves the Golf Cart panel as a standalone. Therefore,
                    // the logic has to account for the combinations of data changes.
                    case "DataChanged":
                        foreach (IMvvmBinder binder in Host.Instance.OpenMvvmBinders)
                        {
                            // Get the document panel that coorsponds to the view.  Null will
                            // be returned for the MainWindow binder, so that is ignored.
                            // Depending on the dirty state of the view model, the tab background and caption
                            // are set to indiate dirty/not-dirty.
                            DocumentPanel dp = GetDocumentPanelByView(binder.View);
                            if (null != dp)
                            {
                                if (binder.ViewModel.IsDirty)
                                {
                                    dp.TabBackgroundColor = System.Windows.Media.Colors.Magenta;
                                }
                                else
                                {
                                    dp.TabBackgroundColor = System.Windows.Media.Colors.Black;
                                }
                                dp.Caption = binder.ViewModel.Caption;
                            }
                        }
                        break;

                    // If there are water meter reading exceptions we create a new document panel to display them in a grid.
                    //case "MeterExceptions":
                    //    CreateMeterExceptionsDocPanel();
                    //    break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                //// On a change ownership, when the CO docpanel is closed, it modifies the document panel collection. Therefore, and exception is
                //// thrown.  Simply ignore the collection modified expection and continue on.
                //if (!ex.Message.Contains("Collection was modified"))
                //{
                    MessageBox.Show("Error in Main: " + ex.Message);
                //}
            }
        }

        /*===========================================================================================*/
        ////
        //// Add On-Click Event Handlers for each tile or ribbon button here.....
        ////
        #region On-Click Events
        /// <summary>
        /// Creates or Focuses the Owners DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_Owners(object sender, MouseButtonEventArgs e)
        {
            // Using Dependancy Inversion, bind the viewModel to the view through the Host.Instance interface
            Host.Instance.Execute(HostVerb.Open, "Owners");
        }

        /// <summary>
        /// Creates or Focuses the Properties DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_Property(object sender, MouseButtonEventArgs e)
        {
            // Using Dependancy Inversion, bind the viewModel to the view through the Host.Instance interface
            Host.Instance.Execute(HostVerb.Open, "Properties");
        }

        /// <summary>
        /// Creates or Focuses the Owners DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void OnClicked_OwnerXRelationships(object sender, MouseButtonEventArgs e)
        //{
        //    // Using Dependancy Inversion, bind the viewModel to the view through the Host.Instance interface
        //    Host.Instance.Execute(HostVerb.Open, "OwnerXRelationships");
        //}

        /// <summary>
        /// Creates of Focuses the Administration DocumentPanel (used for testing purposes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void OnClicked_GolfCartXOwner(object sender, MouseButtonEventArgs e)
        //{
        //    // Using Dependancy Inversion, bind the viewModel to the view through the Host.Instance interface
        //    Host.Instance.Execute(HostVerb.Open, "GolfCartXOwner");
        //}

        /// <summary>
        /// Creates of Focuses the Administration DocumentPanel (used for testing purposes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_GolfCart(object sender, MouseButtonEventArgs e)
        {
            // Using Dependancy Inversion, bind the viewModel to the view through the Host.Instance interface
            Host.Instance.Execute(HostVerb.Open, "GolfCart");
        }

        /// <summary>
        /// Creates or Focuses the WaterSystem DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_WaterSystem(object sender, MouseButtonEventArgs e)
        {
            Host.Instance.Execute(HostVerb.Open, "WaterMeter");
        }

        /// <summary>
        /// Creates or Focuses the WaterSystem DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_WellMeterReadings(object sender, MouseButtonEventArgs e)
        {
            Host.Instance.Execute(HostVerb.Open, "WellMeter");
        }

        /// <summary>
        /// Creates of Focuses the Administration DocumentPanel (used for testing purposes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_Administration(object sender, MouseButtonEventArgs e)
        {
            // Using Dependancy Inversion, bind the viewModel to the view through the Host.Instance interface
            Host.Instance.Execute(HostVerb.Open, "Administration");
        }

        #endregion

        ////
        //// All things related to DocumentPanels
        ////
        #region DocumentPanel Region

        /// <remarks>
        /// Summary:
        ///     Remove a dockpanel from the primary document group. 
        /// </remarks>
        private void DockLayoutManager_DockItemClosing(object sender, DevExpress.Xpf.Docking.Base.ItemCancelEventArgs e)
        {
            DockLayoutManager dm = sender as DockLayoutManager;
            string caption = dm.ActiveDockItem.Caption.ToString();

            // TO-DO : can't use "AnyDirty"... Need to figure out which VM we are dealing with.....
            if (Host.Instance.AnyDirty())
            {
                MessageBoxResult result = MessageBox.Show("You have unsaved edits. Save changes before closing?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        e.Cancel = true;
                        return;
                    case MessageBoxResult.No:
                        break;
                    default:
                        e.Cancel = true;
                        return;
                }
            }

            // Remove the MvvmBinder.
            Host.Instance.Execute(HostVerb.Close, caption); // TO-DO: This may need to move to Closed()

            // Set the previous DockPanel as the active panel
            int count = this.primaryDocumentGroup.Items.Count();
            primaryDocumentGroup.SelectedTabIndex = primaryDocumentGroup.Items.Count - 1;
            if (primaryDocumentGroup.SelectedTabIndex > 0)
            {
                this.dockLayoutManager.Activate(primaryDocumentGroup.Items[primaryDocumentGroup.SelectedTabIndex]);
            }


            // If there are no (more) documents in the documentGroup, then turn off HitTestVisible so
            // Main doesn't throw w/ a null reference if the user clicks on the empty document group panel.
            if (0 == count)
            {
                this.layoutGroupMain.IsHitTestVisible = false;
            }
            e.Handled = true;
        }

        /// <remarks>
        /// Summary:
        ///     When a document panel is closed, clean up our list of <code>OpenMvvmBinders</code>.
        ///     
        /// Parameters:
        ///     Sender
        ///         Invoking method
        ///         
        ///     DockItemClosingEventArgs
        ///         Event args
        /// </remarks>
        private void DockLayoutManager_DockItemClosed(object sender, DevExpress.Xpf.Docking.Base.DockItemClosedEventArgs e)
        {
            try
            {
                UserControl uc = ((ContentItem)e.Item).Content as UserControl;

                if (null != uc)
                {
                    // Invoke the Host Instance to execute the close() method on the user control
                    Host.Instance.Execute(HostVerb.Close, uc);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DockItem Closed Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler for when a document panel becomes the active (focused) panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DockLayoutManager_DockItemActivated(object sender, DevExpress.Xpf.Docking.Base.DockItemActivatedEventArgs e)
        {
            //// Check to make sure we actually have a dockpanel to activate
            //if (null != e.Item)
            //{
            //    //// When any panel other than the Dashboard becomes active, we register the panel as active in the ViewModel
            //    LayoutPanel panel = e.Item as LayoutPanel;
            //    if (null != panel || "Dashboard" != panel.Caption.ToString())
            //    {
            //        panel.TabBackgroundColor = System.Windows.Media.Colors.Aqua; //System.Windows.Media.Colors.Aqua;
            //    }
            //}
        }

        /// <summary>
        /// Returns a list of documents panels
        /// </summary>
        /// <param name="layoutGroup"></param>
        /// <returns></returns>
        private static IEnumerable<DocumentPanel> GetAllDocumentPanels(DevExpress.Xpf.Docking.LayoutGroup layoutGroup)
        {
            IEnumerable<BaseLayoutItem> list = new List<BaseLayoutItem>();
            foreach (DocumentGroup docGroup in GetAllDocumentGroups(layoutGroup))
            {
                if (null != docGroup)
                {
                    list = list.Concat(docGroup.Items.Where(z => z is DocumentPanel));
                }
            }

            var docPanels = from p in list
                            where p is DocumentPanel
                            select p as DocumentPanel;

            return docPanels;
        }

        /// <summary>
        /// Returns a list of document groups
        /// </summary>
        /// <param name="layoutGroup"></param>
        /// <returns></returns>
        private static IEnumerable<DocumentGroup> GetAllDocumentGroups(DevExpress.Xpf.Docking.LayoutGroup layoutGroup)
        {
            IEnumerable<BaseLayoutItem> list = layoutGroup.Items.Where(i => i is DocumentGroup);

            foreach (var item in layoutGroup.Items.Where(i => !(i is DocumentGroup)))
            {
                DevExpress.Xpf.Docking.LayoutGroup lo = item as DevExpress.Xpf.Docking.LayoutGroup;
                if (null != lo)
                {
                    list = list.Concat(GetAllDocumentGroups(lo));
                }
            }

            var docGroups = from g in list
                            where g is DocumentGroup
                            select g as DocumentGroup;

            return docGroups;
        }

        private DocumentPanel GetDocumentPanelByView(IView v)
        {
            IEnumerable<DocumentPanel> dpList;
            dpList = GetAllDocumentPanels(this.layoutGroupMain);
            foreach (DocumentPanel dp in dpList)
            {
                if (v == dp.Content)
                {
                    return dp;
                }
            }
            return null;
        }

        /// <summary>
        /// (MVVM) Creates a new document panel, or brings to focus a panel that exists
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="content"></param>
        private void CreateDockPanel(IView view)
        {
            string viewCaption = view.ViewModel.Caption;

            bool exists = false;

            //// Get the collection of document panels, then check to see if this doc panel already exists.
            //// An existing panel will become the active panel, and the dashboard flyout will be collapsed.
            var docGroupMembers = this.primaryDocumentGroup.GetItems();
            foreach (BaseLayoutItem i in docGroupMembers)
            {
                if (0 == String.Compare(i.Caption.ToString().Trim(), viewCaption.Trim(), true))
                {
                    exists = true;
                    primaryDocumentGroup.SelectedTabIndex = i.TabIndex;
                    this.dockLayoutManager.Activate(i);
                    break;
                }
            }

            if (!exists)
            {
                DocumentPanel docPanel = new DocumentPanel();

                docPanel.Caption = viewCaption;
                docPanel.Content = view;

                this.primaryDocumentGroup.Add(docPanel);
                primaryDocumentGroup.SelectedTabIndex = primaryDocumentGroup.Items.Count - 1;
                this.dockLayoutManager.Activate(docPanel);
                docPanel.TabBackgroundColor = System.Windows.Media.Colors.Black; //System.Windows.Media.Colors.Aqua;
            }

            // The LayoutGroup for the DocumentManager is initially not HitTestVisible to
            // avoid Main throwing w/ a null reference if the user clicks when there aren't
            // Documents in the DocumentGroup.  Once the first DocumentPanel is created
            // HitTest is enabled.
            this.layoutGroupMain.IsHitTestVisible = true;
        }

        /// <summary>
        /// Summary:
        ///     Close a document panel
        ///     
        /// Parameters:
        ///     IView v - The view being closed
        /// </summary>
        /// <param name="v"></param>
        private void CloseDockPanel(IView v)
        {
            try
            {
                var docGroupMembers = this.primaryDocumentGroup.GetItems();
                DocumentPanel dp = GetDocumentPanelByView(v);
                this.primaryDocumentGroup.Remove(dp);
            }
            catch (Exception ex)
            {
                MessageBox.Show("DocItem Close Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Summary:
        ///     Event handler invoked when the main window is closed by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DXRibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Host.Instance.AnyDirty())
            {
                MessageBoxResult result = MessageBox.Show("You have unsaved edits, close without saving changes?", "Unsaved Edits", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

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

        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public object SaveState()
        {
            throw new NotImplementedException();
        }

        public void RestoreState(object state)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}
