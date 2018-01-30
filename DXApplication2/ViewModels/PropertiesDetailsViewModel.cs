namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Models;
    using Resources;
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

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

            PropertiesList = GetPropertiesList();
        }

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
        /// Collection of properties
        /// </summary>
        private ObservableCollection<v_PropertyDetail> _propertiesList = null;
        public ObservableCollection<v_PropertyDetail> PropertiesList
        {
            get
            {
                return _propertiesList;
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
        /// Currently selected property from a property grid view
        /// </summary>
        private v_PropertyDetail _selectedProperty = null;
        public v_PropertyDetail SelectedProperty
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
                        this._selectedProperty = value;
                    }

                    // Once a property has been selected, we enable the ChangeOwner ribbon button if appropriate
                    if (this.ApplPermissions.CanEditOwner)
                    {
                        this.IsEnabledChangeOwner = true;
                    }
                    else
                    {
                        this.IsEnabledChangeOwner = false;
                    }
                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

        //public int NoteCount { get; set; }
        #endregion

        /* ----------------------------------- Boolean Properties ----------------------------------------- */
        #region Boolean Properties

        /// <summary>
        /// Controls enable/disbale state of the Refresh ribbion action button
        /// </summary>
        private bool _isRefreshEnabled = true; // Default: false
        public bool IsRefreshEnabled
        {
            get { return _isRefreshEnabled; }
            set
            {
                if (value != _isRefreshEnabled)
                {
                    _isRefreshEnabled = value;
                    RaisePropertyChanged("IsRefreshEnabled");
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
                if (this.ApplPermissions.CanEditProperty)
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
                if (this.ApplPermissions.CanEditProperty)
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
                if (this.ApplPermissions.CanEditProperty)
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
        private ObservableCollection<v_PropertyDetail> GetPropertiesList()
        {
            try
            {
                var list = (from a in this.dc.v_PropertyDetails
                            select a);
                return new ObservableCollection<v_PropertyDetail>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving Property data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    IsRefreshEnabled = true;
                    dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Properties);
                    //dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Owners);
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
    public partial class PropertiesDetailsViewModel /*: CommonViewModel, ICommandSink*/
    {

        /// <summary>
        /// Refresh Command
        /// </summary>
        private ICommand _refreshCommand;
        public ICommand RefreshCommand
        {
            get
            {
                return _refreshCommand ?? (_refreshCommand = new CommandHandlerWparm((object parameter) => RefreshAction(parameter), IsRefreshEnabled));
            }
        }

        /// <summary>
        /// Refresh data sources
        /// </summary>
        /// <param name="type"></param>
        public void RefreshAction(object parameter) 
        {
            RaisePropertyChanged("IsBusy");
            PropertiesList = GetPropertiesList();
            RaisePropertyChanged("IsNotBusy");
        }

        public virtual ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
        public bool CanExport = true;
        /// <summary>
        /// Add Cart Command
        /// </summary>
        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get
            {
                CommandAction action = new CommandAction();
                return _exportCommand ?? (_exportCommand = new CommandHandlerWparm((object parameter) => action.ExportAction(parameter, Table, SaveFileDialogService, ExportService), CanExport));
            }
        }

        public bool CanPrint = true;
        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _printCommand;
        public ICommand PrintCommand
        {
            get
            {
                CommandAction action = new CommandAction();
                return _printCommand ?? (_printCommand = new CommandHandlerWparm((object parameter) => action.PrintAction(parameter, Table, ExportService), CanPrint));
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
            v_PropertyDetail p = parameter as v_PropertyDetail;
            IsBusy = true;
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
                return _changeOwnerCommand ?? (_changeOwnerCommand = new CommandHandlerWparm((object parameter) => ChangeOwnerAction(parameter), ApplPermissions.CanEditOwner));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void ChangeOwnerAction(object parameter)
        {
            v_PropertyDetail p = parameter as v_PropertyDetail;
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
                return _propertyNotesReportCommand ?? (_propertyNotesReportCommand = new CommandHandlerWparm((object parameter) => PropertyNotesReportAction(parameter), ApplPermissions.CanViewOwnerNotes));
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
                return _duesBalanceReportCommand ?? (_duesBalanceReportCommand = new CommandHandlerWparm((object parameter) => DuesBalanceReportAction(parameter), ApplPermissions.CanViewOwnerNotes));
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
