namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Models;
    using HVCC.Shell.Resources;
    using System;
    using System.Collections.ObjectModel;
    using System.Data.Linq;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    public partial class OwnersViewModel : CommonViewModel, ICommandSink
    {
        public OwnersViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            OwnersList = FetchOwners();

            Host.PropertyChanged +=
                new System.ComponentModel.PropertyChangedEventHandler(this.HostNotification_PropertyChanged);
        }

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

        private ObservableCollection<v_OwnerDetail> _ownersList = null;
        public ObservableCollection<v_OwnerDetail> OwnersList
        {
            get
            {
                return _ownersList;
            }
            set
            {
                if (this._ownersList != value)
                {
                    this._ownersList = value;
                    RaisePropertyChanged("OwnersList");
                }
            }
        }

        private v_OwnerDetail _selectedOwner = null;
        public v_OwnerDetail SelectedOwner
        {
            get
            {
                return _selectedOwner;
            }
            set
            {
                // wrap the setting in a null check.  When the master row is expanded, and a detail row
                // is selected, it causes this propertychanged event and sets the value to null, which we want to ignore.
                if (null != value &&_selectedOwner != value)
                {
                    _selectedOwner = value;

                    // To ensure there is a focused row selected in the detail (Properties) grid,
                    // set the SelectedProperty to the first item.
                    //SelectedProperty = _selectedOwner.Properties[0];
                    RaisePropertyChanged("SelectedOwner");
                }
            }
        }

        public ObservableCollection<v_ActiveRelationship> Relationships
        {
            get
            {
                var list = (from x in dc.v_ActiveRelationships
                            select x);
                return new ObservableCollection<v_ActiveRelationship>(list);
            }
        }

        private v_ActiveRelationship _selectedRelationship = null;
        public v_ActiveRelationship SelectedRelationship
        {
            get
            {
                return _selectedRelationship;
            }
            set
            {
                if (_selectedRelationship != value)
                {
                    _selectedRelationship = value;
                    SelectedOwner = (from x in dc.v_OwnerDetails
                                     where x.OwnerID == _selectedRelationship.OwnerID
                                     select x).FirstOrDefault();
                    IsBusy = true;
                    Host.Execute(HostVerb.Open, "EditOwner", SelectedOwner);
                }
            }
        }

        /* ------------------------------------ Public Methods -------------------------------------------- */
        #region Public Methods

        /// <summary>
        /// Property Changed event handler for the Host instance
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void HostNotification_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Refresh":
                    dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Owners);
                    //OwnersList = FetchOwners();
                    break;
                default:
                    break;
            }
        }

        #endregion

        /* ------------------------------------ Private Methods -------------------------------------------- */
        #region Private Methods

        /// <summary>
        /// Queries the database to get the current list of property records
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<v_OwnerDetail> FetchOwners()
        {
            try
            {
                //// Force a refresh of the datacontext, then get the list of "Properties" from the database
                //this.dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Owners);
                var list = (from a in this.dc.v_OwnerDetails
                            select a);

                return new ObservableCollection<v_OwnerDetail>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving Property data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        #endregion
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class OwnersViewModel : CommonViewModel, ICommandSink
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
            get;
            set;
        }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            ChangeSet cs = dc.GetChangeSet();
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
    public partial class OwnersViewModel
    {
        /// <summary>
        /// RowDoubleClick Event to Command
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
            v_OwnerDetail p = parameter as v_OwnerDetail;
            IsBusy = true;
            Host.Execute(HostVerb.Open, "EditOwner", p);
        }

        ///// <summary>
        ///// RowDoubleClick Command
        ///// </summary>
        //private ICommand _focusedRowChangedCommand;
        //public ICommand FocusedRowChangedCommand
        //{
        //    get
        //    {
        //        return _focusedRowChangedCommand ?? (_focusedRowChangedCommand = new CommandHandlerWparm((object parameter) => FocusedRowChangedAction(parameter), true));
        //    }
        //}

        ///// <summary>
        ///// Grid row double click event to command action
        ///// </summary>
        ///// <param name="type"></param>
        //public void FocusedRowChangedAction(object parameter)
        //{
        //    DevExpress.Xpf.Grid.FocusedRowChangedEventArgs e = parameter as DevExpress.Xpf.Grid.FocusedRowChangedEventArgs;
        //    SelectedProperty = e.NewRow as Property;
        //}

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
            Property p = parameter as Property;
            Host.Execute(HostVerb.Open, "ChangeOwner", p);
        }

        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _ownershipHistoryCommand;
        public ICommand OwnershipHistoryCommand
        {
            get
            {
                return _ownershipHistoryCommand ?? (_ownershipHistoryCommand = new CommandHandler(() => OwnershipHistoryAction(), ApplPermissions.CanViewOwnerNotes));
            }
        }

        /// <summary>
        /// Ownership History command action
        /// </summary>
        /// <param name="type"></param>
        public void OwnershipHistoryAction()
        {
            Host.Execute(HostVerb.Open, "OwnershipHistory");

            //foreach (Owner o in dc.Owners)
            //{
            //    decimal balance = 0;
            //    foreach (Property p in o.Properties)
            //    {
            //        balance += (decimal)p.Balance;
            //    }
            //    FinancialTransaction transaction = new FinancialTransaction();
            //    transaction.OwnerID = o.OwnerID;
            //    transaction.Balance = balance;
            //    transaction.CreditAmount = 0;
            //    transaction.DebitAmount = 0;
            //    transaction.TransactionMethod = "Machine Generated";
            //    transaction.TransactionAppliesTo = "Opening Balance";
            //    transaction.TransactionDate = DateTime.Now;
            //    transaction.Comment = "Sum balance of all properties owned, based on current property balances in the database";

            //    dc.FinancialTransactions.InsertOnSubmit(transaction);
            //    dc.SubmitChanges();
            //}
        }

        ///// <summary>
        ///// Import Command
        ///// </summary>
        //private ICommand _importCommand;
        //public ICommand ImportCommand
        //{
        //    get
        //    {
        //        return _importCommand ?? (_importCommand = new CommandHandlerWparm((object parameter) => ImportAction(parameter), ApplPermissions.CanImport));
        //    }
        //}

        ///// <summary>
        ///// Facility Usage by date range report
        ///// </summary>
        ///// <param name="type"></param>
        //public void ImportAction(object parameter)
        //{
        //    ObservableCollection<Note> notes = new ObservableCollection<Note>();

        //    var list = (from x in dc.Notes
        //                 select x);

        //    foreach (Note n in list)
        //    {
        //        Property property = (from p in dc.Properties
        //                   where p.PropertyID == n.PropertyID
        //                   select p).FirstOrDefault();

        //        n.OwnerID = property.OwnerID;
        //    }
        //    CanSaveExecute = IsDirty;
        //}
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
            OwnersList = FetchOwners();
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
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class OwnersViewModel : IDisposable
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
        ~OwnersViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}