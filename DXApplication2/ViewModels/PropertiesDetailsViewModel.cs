namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using DevExpress.Xpf.Docking;
    using System.Data.Linq;
    using HVCC.Shell.Common;
    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;
    using HVCC.Shell.Models;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Helpers;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Spreadsheet;
    using DevExpress.Xpf.Printing;
    using System.Collections.Generic;
    using System.Windows;
    using System.Text;
    using System.Windows.Input;
    using Resources;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Common.Interfaces;

    public partial class PropertiesDetailsViewModel : CommonViewModel, ICommandSink
    {

        public PropertiesDetailsViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            Host.PropertyChanged +=
                new System.ComponentModel.PropertyChangedEventHandler(this.HostNotification_PropertyChanged);
        }
        /* -------------------------------- Interfaces ------------------------------------------------ */
        #region Interfaces
        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }
        public virtual ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        protected virtual IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
        public enum ExportType { PDF, XLSX }
        public enum PrintType { PREVIEW, PRINT }

        #endregion

        /* ------------------------------ Common ViewModel Properties  --------------------------------------- */
        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

        public override bool IsValid => throw new NotImplementedException();

        public override bool IsDirty
        {
            get
            {
                string[] caption = Caption.ToString().Split('*');
                ChangeSet cs = dc.GetChangeSet();
                if (0 == cs.Updates.Count &&
                    0 == cs.Inserts.Count &&
                    0 == cs.Deletes.Count)
                {
                    Caption = caption[0].TrimEnd(' ');
                    return false;
                }
                Caption = caption[0].TrimEnd(' ') + "*";
                return true;
            }
            set { }
        }

        private bool _isBusy = false;
        public override bool IsBusy
        {
            get
            { return _isBusy; }
            set
            {
                if (value != _isBusy)
                {
                    _isBusy = value;
                    if (_isBusy) { RaisePropertyChanged("IsBusy"); }
                    else { RaisePropertyChanged("IsNotBusy"); }
                }
            }
        }

        /* ------------------------------ Public Variables and Types ----------------------------------------- */
        #region Public Variables
        int RowNum;

        public enum Column : int
        {
            Customer = 0,
            BillTo = 1,
            Balance = 2
        }
        #endregion

        /* ----------------------------------View Model Properties ------------------------------------ */
        #region ViewModel Entities


        /// <summary>
        /// Facility usage collection
        /// </summary>
        private ObservableCollection<FacilityUsage> _usageList = null;
        public ObservableCollection<FacilityUsage> UsagesList
        {
            get
            {
                return this._usageList;
            }
            set
            {
                if (this._usageList != value)
                {
                    this._usageList = value;
                }
            }
        }

        #region Property entities
        /// <summary>
        /// Collection of properties
        /// </summary>
        private ObservableCollection<Property> _propertiesList = null;
        public ObservableCollection<Property> PropertiesList
        {
            get
            {
                if (null == _propertiesList)
                {
                    var list = (from a in this.dc.Properties
                                select a);
                    return new ObservableCollection<Property>(list);
                }
                return this._propertiesList;
            }
            set
            {
                if (this._propertiesList != value)
                {
                    _propertiesList = null;
                    this._propertiesList = value;
                    RaisePropertyChanged("PropertiesList");
                }
            }
        }

        /// <summary>
        /// Register for Property Changes to the ViewModel's SelectedProperty entity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedProperty_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // We listen for changes to the Balance value of the SelectedProperty.  The 'IsInGoodStading' flag is bound to
            // the value of Balance.  When a balance is owed, we consider the member to not be in good standing.  Therefore
            // we toggle the IsInGoodStanding flag based on Balance value.   Also keep in mind a positive balance means there
            // is a balance owed.
            if (e.PropertyName == "Balance")
            {
                if (this.SelectedProperty.Balance > 0)
                {
                    this.SelectedProperty.IsInGoodStanding = false;
                }
                else
                {
                    this.SelectedProperty.IsInGoodStanding = true;
                }
            }

            if (e.PropertyName == "IsGolf" || e.PropertyName == "IsPool")
            {
                int foo = 0;
                foo++;
            }
        }

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        private Property _selectedProperty = null;
        public Property SelectedProperty
        {
            get
            {
                return this._selectedProperty;
            }
            set
            {
                //// wrap the setter with a check for a null value.  This condition happens when
                //// a Relationship is selected from the Relationship grid. Therefore, when
                //// a Relationship is selected we won't null out the SelectedProperty.
                if (null != value)
                {
                    if (value != this._selectedProperty)
                    {
                        // When the selected property is change; a new selection is made, we unregister the previous PropertyChanged
                        // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                        if (this._selectedProperty != null)
                        {
                            this._selectedProperty.PropertyChanged -= SelectedProperty_PropertyChanged;
                        }

                        this._selectedProperty = value;
                        // Once the new value is assigned, we register a new PropertyChanged event listner.
                        this._selectedProperty.PropertyChanged += SelectedProperty_PropertyChanged;

                        this._selectedProperty.PropertyComments = GetPropertyNotes();
                    }

                    // Once a property has been selected, we enable the ChangeOwner ribbon button if appropriate
                    if (this.ApplPermissions.CanChangeOwner)
                    {
                        this.IsEnabledChangeOwner = true;
                    }
                    else
                    {
                        this.IsEnabledChangeOwner = false;
                    }

                    // If appropriate, enable the Add Relationship ribbon button
                    if (this.ApplPermissions.CanAddRelationship)
                    {
                        this.IsEnabledAddRelationship = true;
                    }
                    else
                    {
                        this.IsEnabledAddRelationship = false;

                    }

                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

        /// <summary>
        /// Currently selected relationship
        /// </summary>
        private Relationship _selectedRelation = new Relationship();
        public Relationship SelectedRelation
        {
            get
            {
                return this._selectedRelation;
            }
            set
            {
                if (value != this._selectedRelation)
                {
                    this._selectedRelation = value;
                    //// The database stores the raw binary data of the image.  Before it can be
                    //// displayed in the ImageEdit control, it must be encoded into a BitmapImage
                    if (null == this.SelectedRelation.Photo)
                    {
                        this.SelectedRelation.Photo = this.ApplDefault.Photo; //DefaultBitmapImage; 
                    }
                    RaisePropertyChanged("SelectedRelation");
                }
            }
        }

        /// <summary>
        /// An integer representing the count of notes associated to the selected property
        /// </summary>
        private int _noteCount = 0;
        public int NoteCount
        {
            get
            {
                return this._noteCount;
            }
            set
            {
                if (this._noteCount != value)
                {
                    this._noteCount = value;
                }
            }
        }
        #endregion

        #endregion

        /* ----------------------------------- Boolean Properties ----------------------------------------- */
        #region Boolean Properties

        /// <summary>
        /// Controls the enable/disable state of the Save ribbon action button
        /// </summary>
        private bool _isEnabledSave = false;  // Default: false
        public bool IsEnabledSave
        {
            get { return _isEnabledSave; }
            set
            {
                if (value != _isEnabledSave)
                {
                    _isEnabledSave = value;
                    RaisePropertyChanged("IsEnabledSave");
                }
            }
        }

        /// <summary>
        /// Controls the enable/disable state of the Import ribbon action button
        /// </summary>
        private bool _isEnabledImport = false;  // Default: false
        public bool IsEnabledImport
        {
            get { return _isEnabledImport; }
            set
            {
                if (value != _isEnabledImport)
                {
                    _isEnabledImport = value;
                    RaisePropertyChanged("IsEnabledImport");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledChangeOwner = false; // Default: false
        public bool IsEnabledChangeOwner
        {
            get { return _isEnabledChangeOwner; }
            set
            {
                if (value != _isEnabledChangeOwner)
                {
                    _isEnabledChangeOwner = value;
                    RaisePropertyChanged("IsEnabledChangeOwner");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledAddRelationship = false; // Default: false
        public bool IsEnabledAddRelationship
        {
            get { return _isEnabledAddRelationship; }
            set
            {
                if (value != _isEnabledAddRelationship)
                {
                    _isEnabledAddRelationship = value;
                    RaisePropertyChanged("IsEnabledAddRelationship");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledExport = true; // Default: true
        public bool IsEnabledExport
        {
            get { return _isEnabledExport; }
            set
            {
                if (value != _isEnabledExport)
                {
                    _isEnabledExport = value;
                    //RaisePropertyChanged("IsEnabledExport");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledPrint = true; // Default: true
        public bool IsEnabledPrint
        {
            get { return _isEnabledPrint; }
            set
            {
                if (value != _isEnabledPrint)
                {
                    _isEnabledPrint = value;
                    //RaisePropertyChanged("IsEnabledPrint");
                }
            }
        }

        #endregion

        /* ----------------------------------- Style Properteis ------------------------------------------- */
        #region Style Properties
        /// <summary>
        /// Get TextBox control adornments
        /// </summary>
        private System.Windows.Style _tbStyle = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
        public System.Windows.Style TbStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditPropertyInfo)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
                }
            }
        }

        /// <summary>
        /// Get TextEdit control adornments
        /// </summary>
        private System.Windows.Style _teStyle = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
        public System.Windows.Style TeStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditPropertyInfo)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextEditDisplayStyle"];
                }
            }
        }

        /// <summary>
        /// Get ComboBoxEdit control adornments
        /// </summary>
        private System.Windows.Style _cbStyle = (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
        public System.Windows.Style CbStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditPropertyInfo)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
                }
            }
        }
        #endregion

        /* ----------------------------- Private Methods --------------------------------------------------- */
        #region Private Methods

        /// <summary>
        /// Queries the database to get the current list of property records
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<FacilityUsage> GetFacilitiyUsages()
        {
            try
            {
                //// Get the list of "Properties" from the database
                var list = (from a in this.dc.FacilityUsages
                            select a);

                return new ObservableCollection<FacilityUsage>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving Facilitity Usage data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Queries the database to get the current list of property records
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<Property> FetchProperties()
        {
            try
            {
                //// Force a refresh of the datacontext, then get the list of "Properties" from the database
                this.dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Properties);
                var list = (from a in this.dc.Properties
                            select a);
                return new ObservableCollection<Property>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving Property data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Builds a history of property notes
        /// </summary>
        /// <returns></returns>
        private string GetPropertyNotes()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var notes = (from n in this.dc.Notes
                             where n.PropertyID == this.SelectedProperty.PropertyID
                             orderby n.Entered descending
                             select n);

                this._noteCount = notes.Count();

                // Iterate through the notes collection and build a string of the notes in 
                // decending order.  This string will be reflected in the UI as a read-only
                // history of all note entries.
                foreach (Note n in notes)
                {
                    sb.Append(n.Entered.ToShortDateString()).Append(" ");
                    sb.Append(n.EnteredBy).Append(" - ");
                    sb.Append(n.Comment);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving Property Note data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        #endregion

        /* ------------------------------------ Public Methods -------------------------------------------- */
        #region Public Methods

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void HostNotification_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Refresh":
                    PropertiesList = FetchProperties();
                    break;
                default:
                    break;
            }
        }

        #endregion
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class PropertiesDetailsViewModel : CommonViewModel, ICommandSink
    {
        public void RegisterCommands()
        {
            this.RegisterSaveHandler();
        }

        private void RegisterSaveHandler()
        {
            _sink.RegisterCommand(
                ApplicationCommands.Save,
                param => this.CanSaveExecute,
                param => this.SaveExecute());
        }

        /// <summary>
        /// 
        /// </summary>
        private bool CanSaveExecute
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            this.dc.SubmitChanges();
            RaisePropertyChanged("DataChanged");
            this.IsBusy = false;
        }

        #region ICommandSink Implementation
        private CommandSink _sink = new CommandSink();

        // Required by the ICommandSink Interface
        public bool CanExecuteCommand(ICommand command, object parameter, out bool handled)
        {
            return _sink.CanExecuteCommand(command, parameter, out handled);
        }

        // Required by the ICommandSink Interface
        public void ExecuteCommand(ICommand command, object parameter, out bool handled)
        {
            _sink.ExecuteCommand(command, parameter, out handled);
        }
        #endregion

    }

    /*================================================================================================================================================*/
    /// <summary>
    /// ViewModel Commands
    /// </summary>
    public partial class PropertiesDetailsViewModel : CommonViewModel, ICommandSink
    {
        /// <summary>
        /// Add Cart Command
        /// </summary>
        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get
            {
                return _exportCommand ?? (_exportCommand = new CommandHandlerWparm((object parameter) => ExportAction(parameter), true));
            }
        }

        /// <summary>
        /// Exports data grid to Excel
        /// </summary>
        /// <param name="type"></param>
        public void ExportAction(object parameter) //ExportCommand
        {
            string fn;
            try
            {
                Enum.TryParse(parameter.ToString(), out ExportType type);

                switch (type)
                {
                    case ExportType.PDF:
                        SaveFileDialogService.Filter = "PDF files|*.pdf";
                        if (SaveFileDialogService.ShowDialog())

                            fn = SaveFileDialogService.GetFullFileName();

                            ExportService.ExportToPDF(this.Table, SaveFileDialogService.GetFullFileName());
                        break;
                    case ExportType.XLSX:
                        SaveFileDialogService.Filter = "Excel 2007 files|*.xlsx";
                        if (SaveFileDialogService.ShowDialog())
                            ExportService.ExportToXLSX(this.Table, SaveFileDialogService.GetFullFileName());
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error exporting data:" + ex.Message);
            }
            finally
            {
                //this.IsRibbonMinimized = true;
            }
        }

        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _printCommand;
        public ICommand PrintCommand
        {
            get
            {
                return _printCommand ?? (_printCommand = new CommandHandlerWparm((object parameter) => PrintAction(parameter), true));
            }
        }

        /// <summary>
        /// Prints the current document
        /// </summary>
        /// <param name="type"></param>
        public void PrintAction(object parameter) //PrintCommand
        {
            try
            {
                Enum.TryParse(parameter.ToString(), out PrintType type);

                switch (type)
                {
                    case PrintType.PREVIEW:
                        ExportService.ShowPrintPreview(this.Table);
                        break;
                    case PrintType.PRINT:
                        ExportService.Print(this.Table);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error printing data:" + ex.Message);
            }
            finally
            {
                //this.IsRibbonMinimized = true;
            }
        }

        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _rowDoubleClickCommand;
        public ICommand RowDoubleClickCommand
        {
            get
            {
                return _rowDoubleClickCommand ?? (_rowDoubleClickCommand = new CommandHandlerWparm((object parameter) => RowDoubleClickAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void RowDoubleClickAction(object parameter)
        {
            Property p = parameter as Property;
            Host.Execute(HostVerb.Open, "PropertyEdit", p);
        }

        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _changeOwnerCommand;
        public ICommand ChangeOwnerCommand
        {
            get
            {
                return _changeOwnerCommand ?? (_changeOwnerCommand = new CommandHandlerWparm((object parameter) => ChangeOwnerAction(parameter), ApplPermissions.CanChangeOwner));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void ChangeOwnerAction(object parameter)
        {
            Property p = parameter as Property;
            Host.Execute(HostVerb.Open, "ChangeOwner", p);
        }

        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _importCommand;
        public ICommand ImportCommand
        {
            get
            {
                return _importCommand ?? (_importCommand = new CommandHandlerWparm((object parameter) => ImportAction(parameter), ApplPermissions.CanImport));
            }
        }

        /// <summary>
        /// Facility Usage by date range report
        /// </summary>
        /// <param name="type"></param>
        public void ImportAction(object parameter)
        {
            Host.Execute(HostVerb.Open, "ImportBalances");
        }

        /// <summary>
        /// Facility Usage by date range report
        /// </summary>
        private ICommand _periodUsageReportCommand;
        public ICommand PeriodUsageReportCommand
        {
            get
            {
                return _periodUsageReportCommand ?? (_periodUsageReportCommand = new CommandHandlerWparm((object parameter) => PeriodUsageReportAction(parameter), true));
            }
        }

        /// <summary>
        /// Facility Usage by date range report
        /// </summary>
        /// <param name="type"></param>
        public void PeriodUsageReportAction(object parameter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Facility Usage for the current day
        /// </summary>
        private ICommand _dayUsageReportCommand;
        public ICommand DayUsageReportCommand
        {
            get
            {
                return _dayUsageReportCommand ?? (_dayUsageReportCommand = new CommandHandlerWparm((object parameter) => DayUsageReportAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void DayUsageReportAction(object parameter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        private ICommand _propertyNotesReportCommand;
        public ICommand PropertyNotesReportCommand
        {
            get
            {
                return _propertyNotesReportCommand ?? (_propertyNotesReportCommand = new CommandHandlerWparm((object parameter) => PropertyNotesReportAction(parameter), ApplPermissions.CanViewPropertyNotes));
            }
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        /// <param name="type"></param>
        public void PropertyNotesReportAction(object parameter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        private ICommand _duesBalanceReportCommand;
        public ICommand DuesBalanceReportCommand
        {
            get
            {
                return _duesBalanceReportCommand ?? (_duesBalanceReportCommand = new CommandHandlerWparm((object parameter) => DuesBalanceReportAction(parameter), ApplPermissions.CanViewPropertyNotes));
            }
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        /// <param name="type"></param>
        public void DuesBalanceReportAction(object parameter)
        {
            Reports.BalancesDueReport report = new Reports.BalancesDueReport();
            //PrintHelper.ShowPrintPreview(this, report);
        }
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class PropertiesDetailsViewModel : IDisposable
    {
        // Resources that must be disposed:
        private HVCCDataContext dc = null;

        private bool disposed = false;

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(!this.disposed); // if !disposed, there is more cleanup to do.
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.DisposeManagedResources();

                    //// TODO:   Clean up desposables.....
                    if (null != this.dc)
                    {
                        this.dc.Dispose();
                        this.dc = null;
                    }
                }

                /*Clean up unmanaged resources here*/

                this.disposed = true;
            }
        }

        protected virtual void DisposeManagedResources()
        {
            // No op.
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TableForm"/> class.  (a.k.a. destructor)
        /// </summary>
        ~PropertiesDetailsViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
