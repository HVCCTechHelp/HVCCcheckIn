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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXRibbonWindow
    {
        PropertiesViewModel vm = null;

        public MainWindow()
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
            // vm = ((PropertiesViewModel)this.DataContext);
            vm = ((PropertiesViewModel)this.DataContext);
            vm.ViewModels.Add(this.DataContext);

            if (null != vm)
            {
                if (!vm.IsConnected)
                {
                    MessageBox.Show("The server is not responding\nPlease check that the server is turned on and try again.\nThe program will now terminate", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Windows.Application.Current.Shutdown();
                }
                vm.PropertyChanged +=
                    new System.ComponentModel.PropertyChangedEventHandler(this.PropertiesViewModel_PropertyChanged);
                vm.DocGroup = this.primaryDocumentGroup;
            }
        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IEnumerable<DocumentPanel> dpList;

            try
            {
                switch (e.PropertyName)
                {
                    case "IsBusy":
                        if (vm.IsBusy)
                        {
                            Mouse.OverrideCursor = Cursors.Wait;
                        }
                        else
                        {
                            Mouse.OverrideCursor = Cursors.Arrow;
                        }
                        break;

                    // When data changes, we update the panel's caption with an astrict to indicate a dirty state.  Since
                    // property, relationship, and water share a symbiotic data relationship, if anyone of the property related
                    // documents panel data change, we update them all.  This leaves the Golf Cart panel as a standalone. Therefore,
                    // the logic has to account for the combinations of data changes.
                    case "IsEnabledSave":
                    case "DataUpdated":
                        dpList = GetAllDocumentPanels(this.layoutGroupMain);
                        foreach (DocumentPanel dp in dpList)
                        {
                            // If the user landed here from a Change Ownership, then we close the ChangeOwnership document panel and 
                            // make the Manage Properties panel the active panel.
                            if (dp.Caption.ToString() == "Change Ownership" && vm.IsBusy)
                            {
                                dp.CloseCommand = DevExpress.Xpf.Docking.CloseCommand.Close;
                                this.primaryDocumentGroup.Remove(dp);
                                DocumentPanel makeActive = (from x in dpList
                                                            where x.Caption.ToString() == "Manage Properties"
                                                            select x).FirstOrDefault();
                                if (null == makeActive)
                                {
                                    makeActive = (from x in dpList
                                                  select x).LastOrDefault();
                                }
                                this.dockLayoutManager.Activate(makeActive);
                                vm.ActiveDocPanel = makeActive;
                                break;
                            }

                            if (vm.IsDirty)
                            {
                                dp.TabBackgroundColor = System.Windows.Media.Colors.Magenta;
                            }
                            else
                            {
                                dp.TabBackgroundColor = System.Windows.Media.Colors.Black;
                            }
                            Helper.UpdateCaption(dp, vm.IsDirty);
                        }
                        break;

                    // When the active document panel changes, the background colors are adjusted to reflect which
                    // panel is the active on.
                    case "ActiveDocPanel":
                        dpList = GetAllDocumentPanels(this.layoutGroupMain);
                        foreach (DocumentPanel dp in dpList)
                        {
                            if (dp == vm.ActiveDocPanel)
                            {
                                dp.TabBackgroundColor = System.Windows.Media.Colors.Black;
                            }
                            else
                            {
                                dp.TabBackgroundColor = System.Windows.Media.Colors.White;
                            }

                            if (vm.IsDirty)
                            {
                                dp.TabBackgroundColor = System.Windows.Media.Colors.Magenta;
                            }
                            Helper.UpdateCaption(dp, vm.IsDirty);
                        }
                        break;

                    // If there are water meter reading exceptions we create a new document panel to display them in a grid.
                    case "MeterExceptions":
                        CreateMeterExceptionsDocPanel();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                // On a change ownership, when the CO docpanel is closed, it modifies the document panel collection. Therefore, and exception is
                // thrown.  Simply ignore the collection modified expection and continue on.
                if (!ex.Message.Contains("Collection was modified"))
                {
                    MessageBox.Show("Error in Main: " + ex.Message);
                }
            }
        }

        ////
        //// All things related to DocumentPanels
        ////
        #region DocumentPanel Region

        /// <remarks>
        /// This code removes a dockpanel from the primary document group. 
        /// </remarks>
        private void DockLayoutManager_DockItemClosing(object sender, DevExpress.Xpf.Docking.Base.ItemCancelEventArgs e)
        {
            if (true == vm.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show("You have unsaved edits, Save edits?", "Unsaved Edits", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (MessageBoxResult.Yes == result)
                {
                    vm.Save();
                }
                else if (MessageBoxResult.Cancel == result)
                {
                    e.Cancel = true; ;
                }
                else
                {
                    result = MessageBox.Show("Confirm you would like to abandon all pending changes", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (MessageBoxResult.Yes == result)
                    {
                        //// If the user clicked the window's "X" to close, and they have elected NOT to save
                        //// the changes, then we revert them back.
                        if (e.Item.Caption.ToString().Contains("Properties"))
                        {
                            vm.CancelPropertyAction();
                        }
                        if (e.Item.Caption.ToString().Contains("Golf Cart"))
                        {
                            // TO-DO: Golf....move to the GOlf V/VM
                            //vm.RevertGolfCartEdits();
                        }
                        this.primaryDocumentGroup.Remove(e.Item);
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            }
            else
            {
                this.primaryDocumentGroup.Remove(e.Item);
                int count = this.primaryDocumentGroup.Items.Count();
                // If there are no (more) documents in the documentGroup, then turn off HitTestVisible so
                // Main doesn't throw w/ a null reference 
                this.layoutGroupMain.IsHitTestVisible = true;
                e.Handled = true;
            }
            Helper.UpdateCaption(vm.ActiveDocPanel, vm.IsDirty);
        }

        /// <remarks>
        /// When a document item is closed, clean up our list of <code>OpenMvvmBinder</code>s.
        /// </remarks>
        private void DockLayoutManager_DockItemClosed(object sender, DevExpress.Xpf.Docking.Base.DockItemClosedEventArgs e)
        {
            try
            {
                UserControl uc = ((ContentItem)e.Item).Content as UserControl;

                //// TO-DO:  Dispose of the ViewModel and remove this DocPanel from the DockManager list
                if (null != uc)
                {
                    IDisposable disp = uc.DataContext as IDisposable;
                    if (null != disp)
                    {
                        disp.Dispose();
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("DocItemClosed Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Event handler for when a document panel become the active (focused) panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DockLayoutManager_DockItemActivated(object sender, DevExpress.Xpf.Docking.Base.DockItemActivatedEventArgs e)
        {
            //// Check to make sure we actually have a dockpanel to activate
            if (null != e.Item)
            {
                //PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);

                //// When any panel other than the Dashboard becomes active, we register the panel as active in the ViewModel
                LayoutPanel panel = e.Item as LayoutPanel;
                if ("Dashboard" != panel.Caption.ToString())
                {
                    vm.ActiveDocPanel = panel;
                }
            }
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

        //private static int GetIDocumentPanelCount(LayoutGroup layoutGroup)
        //{
        //    using (IEnumerable<BaseLayoutItem> list = new List<BaseLayoutItem>())
        //    {
        //        list = GetAllDocumentPanels();
        //    }
        //    return list.Count();
        //}

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

        /// <summary>
        /// Creates a new document panel, or brings to focus a panel that exists
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="content"></param>
        private void CreateDockPanel(string caption, object content)
        {
            //PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);
            bool exists = false;

            //// Get the collection of document panels, then check to see if this doc panel already exists.
            //// An existing panel will become the active panel, and the dashboard flyout will be collapsed.
            var docGroupMembers = this.primaryDocumentGroup.GetItems();
            foreach (BaseLayoutItem i in docGroupMembers)
            {
                if (0 == String.Compare(i.Caption.ToString().Trim(), caption.Trim(), true))
                {
                    exists = true;
                    primaryDocumentGroup.SelectedTabIndex = i.TabIndex;
                    this.dockLayoutManager.Activate(i);
                    break;
                }
            }

            if (!exists)
            {
                DocumentPanel docPanel = new DocumentPanel(); // { DataContext = this.DataContext };

                docPanel.Caption = caption;
                docPanel.Content = content;

                this.primaryDocumentGroup.Add(docPanel);
                primaryDocumentGroup.SelectedTabIndex = primaryDocumentGroup.Items.Count - 1;
                this.dockLayoutManager.Activate(docPanel);
                docPanel.TabBackgroundColor = System.Windows.Media.Colors.Aqua;
            }

            // The LayoutGroup for the DocumentManager is initially not HitTestVisible to
            // avoid Main throwing w/ a null reference if the user clicks when there aren't
            // Documents in the DocumentGroup.  Once the first DocumentPanel is created
            // HitTest is enabled.
            this.layoutGroupMain.IsHitTestVisible = true;
        }
        #endregion

        ////
        //// Add On-Click Event Handlers for each tile or ribbon button here.....
        ////
        #region On-Click Events
        /// <summary>
        /// Creates or Focuses the Properties DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_Property(object sender, MouseButtonEventArgs e)
        {
            Object content = new HVCC.Shell.Views.PropertyDetailsView() { DataContext = this.DataContext };
            CreateDockPanel("Manage Properties ", content);
        }

        /// <summary>
        /// Creates of Focuses the Administration DocumentPanel (used for testing purposes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_GolfCart(object sender, MouseButtonEventArgs e)
        {
            //this.viewModelController.CreateGolfCartViewModel();

            IViewModel vm = new GolfCartViewModel() { Caption = "Golf Carts" };
            Object content = new HVCC.Shell.Views.GolfCartView(vm);
            CreateDockPanel(vm.Caption, content);
        }

        /* -------------  Deprecated: The Relationships DocumentPanel was combined into the Properties Edit Dialog --- */
        /// <summary>
        /// Creates or Focuses the Relationships DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void OnClicked_Relationship(object sender, MouseButtonEventArgs e)
        //{
        //    PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);
        //    //vm.IsEnabledAddRelationship = true;
        //    Object content = new HVCC.Shell.Views.RelationshipView() { DataContext = this.DataContext };
        //    CreateDockPanel("Manage Relationships", content);
        //}

        /// <summary>
        /// Creates or Focuses the WaterSystem DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_WaterSystem(object sender, MouseButtonEventArgs e)
        {
            Object content = new HVCC.Shell.Views.WaterSystemView() { DataContext = this.DataContext };
            CreateDockPanel("Water System ", content);
        }

        /// <summary>
        /// Creates or Focuses the WaterSystem DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_WellMeterReadings(object sender, MouseButtonEventArgs e)
        {
            Object content = new HVCC.Shell.Views.WellMeterReadingsView() { DataContext = this.DataContext };
            CreateDockPanel("Well Meter Readings ", content);
        }

        /// <summary>
        /// Creates of Focuses the Administration DocumentPanel (used for testing purposes)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_Administration(object sender, MouseButtonEventArgs e)
        {
            Object content = new HVCC.Shell.Views.Administration() { DataContext = this.DataContext };
            CreateDockPanel("Administration ", content);
        }

        /// <summary>
        /// Creates or Focuses the Change Owner DocumentPanel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_ChangeOwner(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //this.TopRibbon.IsMinimized = true;
            //PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);
            if (vm.IsDirty)
            {
                MessageBox.Show("Current changes must be saved or undone before you can modify property owenership.", "Unsaved Edits", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            }
            else
            {
                Object content = new HVCC.Shell.Views.ChangeOwnerView() { DataContext = this.DataContext };
                CreateDockPanel("Change Ownership", content);
            }
        }

        /// <summary>
        /// Creates or Focuses the property Import DocumentPanel.  The input file is an Excel spread-sheet provided by Koeta from QuickBooks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClicked_Import(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            //this.TopRibbon.IsMinimized = true;
            if (vm.IsDirty)
            {
                MessageBox.Show("Current changes must be saved or undone before you can import property data.", "Unsaved Edits", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            }
            else
            {
                Object content = new HVCC.Shell.Views.PropertiesUpdatedView() { DataContext = this.DataContext };
                CreateDockPanel("Import Results", content);
                vm.Import();
            }
        }

        #region DEPRECATED 8/9/2017
        /// <summary>
        /// Opens the Relationship Dialog box for adding Relationships
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void bbAddRelationship_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        //{
        //    //this.TopRibbon.IsMinimized = true;

        //    // Since the user clicked on the "Add Relationship" ribbon buttion, we have to
        //    // control some user interaction here.  The DialogService creates a model dialog window.
        //    // Therefore, when the user clicks on one of the dialog's command buttons, the dialog
        //    // send back a 'result' on closing.  For the "Add" function to work, we have to wrap
        //    // the creation and termination user interactions here.  So, if the "Add" button is
        //    // clicked, the action is to recreate another dialog window.  The dialog button
        //    // actions are mapped as follows:
        //    //      Add == MessageBoxResult.None
        //    //      Update = MessageBoxResult.OK
        //    //      Cancel = MessageBoxResult.Cancel
        //    MessageBoxResult result = new MessageBoxResult();
        //    result = MessageBoxResult.None;
        //    while (MessageBoxResult.None == result)
        //    {
        //        result = vm.ShowRelationshipDialog(null, PropertiesViewModel.DialogType.ADD);
        //    }
        //}
        #endregion
        #endregion
        private void CreateMeterExceptionsDocPanel()
        {
            Object content = new HVCC.Shell.Views.WaterMeterExceptionsView() { DataContext = this.DataContext };
            CreateDockPanel("Water Meter Import Exceptions", content);
        }

        /// <summary>
        /// Event handler invoked when a document panel is closed by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DXRibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);

            if (vm.IsDirty)
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

        /// <summary>
        /// Expands the Ribbon on MouseEnter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopRibbon_MouseEnter(object sender, MouseEventArgs e)
        {
            TopRibbon.ExpandMinimizedRibbon();
            e.Handled = true;
        }

        /// <summary>
        /// Minimizes the Ribbon on MouseLeave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopRibbon_MouseLeave(object sender, MouseEventArgs e)
        {
            TopRibbon.IsMinimized = true;
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
        #endregion

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
