namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using DevExpress.Xpf.Docking;
    using System.Data.Linq;
    using HVCC.Shell.Common;
    using DevExpress.Mvvm;
    using HVCC.Shell.Models;
    using DevExpress.Spreadsheet;
    using System.Windows.Input;
    using HVCC.Shell.Resources;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Validation;
    using System.Collections.Generic;
    using System.Text;
    using DevExpress.Xpf.Grid;
    using System.Windows;

    public partial class ChangeOwnerViewModel : CommonViewModel, ICommandSink
    {
        public ChangeOwnerViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;

            // parameter is set by the invocation: PropertyEditViewModel or ChangeOwnerViewModel
            v_PropertyDetail p = parameter as v_PropertyDetail;
            if (null != p)
            {
                // Set the SelectedProperty, and make a clone copy of it for later reference.
                SelectedProperty = GetProperty(p.PropertyID);
                PreviousOwner.OwnerID = SelectedProperty.OwnerID;
            }
            else
            {
            }
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            CanSaveExecute = false;
            this.RegisterCommands();

            HeaderText = string.Format("Owner Information For Lot#: {0}", SelectedProperty.Customer);

            NewOwner.PropertyChanged +=
                 new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);
        }

        /* -------------------------------- Interfaces ------------------------------------------------ */
        #region Interfaces
        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }
        public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
        public virtual ISaveFileDialogService SaveFileDialogService { get { return GetService<ISaveFileDialogService>(); } }
        #endregion

        public enum ExportType { PDF, XLSX }
        public enum PrintType { PREVIEW, PRINT }

        /* ------------------------------------- Properties and Commands --------------------------- */

        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

        /// <summary>
        /// Runs validation on the view model
        /// </summary>
        /// <returns></returns>
        public override bool IsValid { get { return CkIsValid(); } }

        public override bool IsDirty
        {
            get
            {
                string[] caption = Caption.ToString().Split('*');
                ChangeSet cs = dc.GetChangeSet();
                if (1 == cs.Updates.Count && // there will always be (1) update, because we null out the BillTo field of SelectedProperty
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

        private string _headerText = string.Empty;
        public string HeaderText
        {
            get { return _headerText; }
            set
            {
                if (_headerText != value)
                {
                    _headerText = value;
                    RaisePropertyChanged("HeaderText");
                }
            }
        }

        public Season Season
        {
            get
            {
                Season s = (from x in dc.Seasons
                            where x.IsCurrent == true
                            select x).FirstOrDefault();
                return s;
            }
        }

        #region Properties

        public ObservableCollection<Owner> OwnerList
        {
            get
            {
                var list = (from x in dc.Owners
                            select x);
                return new ObservableCollection<Owner>(list);
            }
        }

        /// <summary>
        /// The Owner entity being acted upon
        /// </summary>
        public Owner PreviousOwner
        {
            get
            {
                Owner _owner = (from x in dc.Owners
                                where x.OwnerID == SelectedProperty.Owner.OwnerID
                                select x).FirstOrDefault();
                return _owner;
            }
        }

        /// <summary>
        /// The Owner entity being acted upon
        /// </summary>
        private Owner _newOwner = new Owner();
        public Owner NewOwner
        {
            get
            {
                return _newOwner;
            }
            set
            {
                this._newOwner = value;
                RaisePropertyChanged("NewOwner");
            }
        }

        /// <summary>
        /// The Owner entity being acted upon
        /// </summary>
        private Owner _selectedOwner = null;
        public Owner SelecctedOwner
        {
            get
            {
                return _selectedOwner;
            }
            set
            {
                this._selectedOwner = value;
                RaisePropertyChanged("SelecctedOwner");
            }
        }

        private string _newNote = string.Empty;
        public string NewNote
        {
            get { return _newNote; }
            set
            {
                if (_newNote != value)
                {
                    _newNote = value;
                    RaisePropertyChanged("NewNote");
                }
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
                return _selectedProperty;
            }
            set
            {
                if (value != this._selectedProperty)
                {
                    this._selectedProperty = value;
                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

        /// <summary>
        /// A collection of properties owned by the current owner
        /// </summary>
        private ObservableCollection<Property> _properties = null;
        public ObservableCollection<Property> Properties
        {
            get
            {
                return _properties;
            }
            set
            {
                if (_properties != value)
                {
                    _properties = value;
                    RaisePropertyChanged("Properties");
                }
            }
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<Relationship> _relationshipsToProcess = null;
        public ObservableCollection<Relationship> RelationshipsToProcess
        {
            get
            {
                return this._relationshipsToProcess;
            }
            set
            {
                if (value != _relationshipsToProcess)
                {
                    // When the collection changes; an item is added/removed, we unregister the previous CollectionChanged
                    // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                    if (_relationshipsToProcess != null)
                    {
                        //_relationshipsToProcess.CollectionChanged -= _relationshipsToProcess_CollectionChanged;
                    }
                    _relationshipsToProcess = value;
                    // Once the new value is assigned, we register a new PropertyChanged event listner.
                    //this._relationshipsToProcess.CollectionChanged += _relationshipsToProcess_CollectionChanged;
                    RaisePropertyChanged("RelationshipsToProcess");
                }
            }
        }

        /// <summary>
        /// Summary
        ///     Raises a property changed event when the NewOwner data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Property_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MailTo")
            {
                RelationshipsToProcess = Helper.GetOwnersFromMailTo(NewOwner.MailTo);
            }

            if (CkIsValid())
            {
                CanSaveExecute = IsDirty;
                RaisePropertyChanged("DataChanged");
            }
        }

        /// <summary>
        /// Executes when the RelationsToProcess collection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _relationshipsToProcess_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            string action = e.Action.ToString();

            switch (action)
            {
                case "Reset":
                    break;

                // When items are "selected" (for removal) they will result in an "Add" action to the Relationships collection.
                // When new names are added to the Relationships collection, they too result in an "Add" action.
                // Therefore, the logic needs to determine which actual action (Add or Remove) needs to happen.
                case "Add":
                    var newItems = e.NewItems;
                    foreach (Relationship r in newItems)
                    {
                        if (0 != r.RelationshipID)
                        {
                            bool result = Helper.RemoveRelationship(this.dc, r, "ChangeOwner");
                        }
                        else
                        {
                            bool result = Helper.AddRelationship(this.dc, SelectedProperty.Owner, r);
                        }
                    }
                    break;
                case "Remove":
                    var oldItems = e.OldItems;
                    foreach (Relationship r in oldItems)
                    {
                        bool result = Helper.AddRelationship(this.dc, SelectedProperty.Owner, r);
                    }
                    break;
            }
            CanSaveExecute = CkIsValid();
            RaisePropertyChanged("DataChanged");
        }

        /* ====== Keep for reference ========= */
        //
        // Acts on a Collection:  When any property of a collection is changed, this will be invoked.
        //
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //protected void RegisterForChangedNotification<T>(ObservableCollection<T> list) where T : INotifyPropertyChanged
        //{
        //    list.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(this.List_CollectionChanged<T>);
        //    foreach (T row in list)
        //    {
        //        row.PropertyChanged += new PropertyChangedEventHandler(this.ListItem_PropertyChanged);
        //    }
        //}

        //private void List_CollectionChanged<T>(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) where T : INotifyPropertyChanged
        //{
        //    if (e != null && e.OldItems != null)
        //    {
        //        foreach (T row in e.OldItems)
        //        {
        //            if (row != null)
        //            {
        //                // If one is deleted you can DeleteOnSubmit it here or something, also unregister for its property changed
        //                row.PropertyChanged -= this.ListItem_PropertyChanged;
        //            }
        //        }
        //    }

        //    if (e != null && e.NewItems != null)
        //    {
        //        foreach (T row in e.NewItems)
        //        {
        //            if (row != null)
        //            {
        //                // If a new one is entered you can InsertOnSubmit it here or something, also register for its property changed
        //                row.PropertyChanged += this.ListItem_PropertyChanged;
        //            }
        //        }
        //    }
        //}
        //
        ///// <summary>
        ///// Listen for changes to a collection item property change
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void ListItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    //CanSaveExecute = IsDirty;
        //    //RaisePropertyChanged("DataChanged");
        //}

        #endregion

        /* ---------------------------------- Public/Private Methods ------------------------------------------ */
        #region Methods

        private Property GetProperty(int pID)
        {
            Property p = (from x in dc.Properties
                          where x.PropertyID == pID
                          select x).FirstOrDefault();

            return p as Property;
        }

        #endregion
    }

    /*==================================================== Command Sink Bindings ================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class ChangeOwnerViewModel : CommonViewModel, ICommandSink
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

            if (0 == NewOwner.OwnerID)
            {
                if (null == RelationshipsToProcess || 0 == RelationshipsToProcess.Count)
                {
                    MessageBox.Show("You must have at lease one owner.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Helper.CheckForOwner(RelationshipsToProcess) && IsValid)
                {
                    this.IsBusy = true;
                    RaisePropertyChanged("IsBusy");

                    int? newOwnerID = 0;

                    // Stop notifcations and write NewOwner to the database.
                    NewOwner.PropertyChanged -=
                         new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);

                    // Call the Stored Procedure directly to perform the Insert.  We do this outside of the
                    // managed datacontext so we can get the OwnerID of the new record which is required for
                    // inserting Relationships for the new owner.
                    NewOwner.Customer = SelectedProperty.Customer;
                    dc.usp_InsertOwner(
                            NewOwner.Customer,
                            NewOwner.MailTo,
                            NewOwner.Address,
                            NewOwner.Address2,
                            NewOwner.City,
                            NewOwner.State,
                            NewOwner.Zip,
                            NewOwner.PrimaryPhone,
                            NewOwner.SecondaryPhone,
                            NewOwner.EmailAddress,
                            NewOwner.IsSendByEmail,
                            true,
                            ref newOwnerID);

                    NewOwner = (from x in dc.Owners
                                where x.OwnerID == newOwnerID
                                select x).FirstOrDefault();

                    // We have to also add an initial FinancialTransaction record to set the new owner
                    // account balance to $0.00 since we use the OwnerDetail View for the Grids
                    FinancialTransaction newTrans = new FinancialTransaction();
                    newTrans.OwnerID = NewOwner.OwnerID;
                    newTrans.Balance = 0m;
                    newTrans.FiscalYear = Season.TimePeriod;
                    newTrans.CreditAmount = 0;
                    newTrans.DebitAmount = 0;
                    newTrans.Comment = "New account establlished for owner";
                    newTrans.TransactionDate = DateTime.Now;
                    newTrans.TransactionAppliesTo = "Account";
                    newTrans.TransactionMethod = "MachineGenerated";
                    dc.FinancialTransactions.InsertOnSubmit(newTrans);

                    // Insert the Relationship collection
                    foreach (Relationship r in RelationshipsToProcess)
                    {
                        r.Owner = NewOwner;
                        r.Active = true;
                        r.Photo = ApplDefault.Photo;
                    }
                    dc.Relationships.InsertAllOnSubmit(RelationshipsToProcess);
                }
            }

            string billTo = string.Format("{0} {1} {2} {3} {4} {5}"
                   , NewOwner.MailTo
                   , NewOwner.Address
                   , NewOwner.Address2
                   , NewOwner.City
                   , NewOwner.State
                   , NewOwner.Zip);
            //SelectedProperty.BillTo = billTo;

            // Create the OwnershipChange record
            OwnershipChange oc = new OwnershipChange();
            oc.PropertyID = SelectedProperty.PropertyID;
            oc.PreviousOwnerID = PreviousOwner.OwnerID;
            oc.PreviousOwner = PreviousOwner.MailTo;
            oc.NewOwnerID = NewOwner.OwnerID;
            oc.NewOwner = billTo;
            dc.OwnershipChanges.InsertOnSubmit(oc);

            // Set the previous owner inactive. We also need to de-activate any relationships associated to the 
            // previous owner if they do not own any other properties.
            var plist = (from x in dc.Properties
                         where x.OwnerID == PreviousOwner.OwnerID
                         select x);
            if (0 == plist.Count())
            {
                PreviousOwner.IsCurrentOwner = false;

                var rlist = (from r in dc.Relationships
                            where r.OwnerID == PreviousOwner.OwnerID
                            select r);
                foreach (Relationship x in rlist)
                {
                    x.Active = false;
                }
            }

            // Change the Property Owner to the NewOwner
            NewOwner.IsCurrentOwner = true;
            SelectedProperty.Owner = NewOwner;
            var list = (from r in dc.Relationships
                        where r.OwnerID == NewOwner.OwnerID
                        select r);
            foreach (Relationship x in list)
            {
                x.Active = true;
            }

            ChangeSet cs = dc.GetChangeSet();
            this.dc.SubmitChanges();
            this.IsBusy = false;
            RaisePropertyChanged("IsNotBusy");
            Host.Execute(HostVerb.Close, this.Caption);
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

    /*==================================================== ViewModel Commands ===================================================================*/
    public partial class ChangeOwnerViewModel : CommonViewModel, ICommandSink
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
            RowDoubleClickEventArgs p = parameter as RowDoubleClickEventArgs;
            object x = p.Source;
            NewOwner= p.Source.FocusedRow as Owner;

            ApplPermissions.CanEditOwner = false;
            ApplPermissions.CanAddRelationship = false;
            RaisePropertyChanged("ApplPermissions");

            CanSaveExecute = IsDirty;
            RaisePropertyChanged("DataChanged");

        }

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
            try
            {
                Enum.TryParse(parameter.ToString(), out ExportType type);

                switch (type)
                {
                    case ExportType.PDF:
                        SaveFileDialogService.Filter = "PDF files|*.pdf";
                        if (SaveFileDialogService.ShowDialog())
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
    }

    /*======================================================= Validation ==============================================================================*/
    public partial class ChangeOwnerViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CkIsValid()
        {
            StringBuilder message = new StringBuilder();

            if (
                   !String.IsNullOrEmpty(NewOwner.MailTo)
                && !String.IsNullOrEmpty(NewOwner.Address)
                && !String.IsNullOrEmpty(NewOwner.City)
                && !String.IsNullOrEmpty(NewOwner.State)
                && !String.IsNullOrEmpty(NewOwner.Zip)
                )
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers the required Properties to be validated
        /// </summary>
        string IDataErrorInfo.Error
        {
            get
            {
                //if (!allowValidation) return null;

                IDataErrorInfo iDataErrorInfo = (IDataErrorInfo)this;
                string error = String.Empty;

                //// The following properties must contain data in order to pass basic validation
                error =
                    iDataErrorInfo[BindableBase.GetPropertyName(() => NewOwner.MailTo)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => NewOwner.Address)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => NewOwner.City)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => NewOwner.State)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => NewOwner.Zip)];

                if (!string.IsNullOrEmpty(error))
                {
                    return "Please check input data.";
                    //return error;
                }
                return null;
            }
        }

        /// <summary>
        /// Assign the validation rule on based on the property name
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        string IDataErrorInfo.this[string columnName]
        {
            get
            { // is hit...
                //// If invoked, will throw validation error if property is null/blank

                StringBuilder errorMsg = new StringBuilder();

                if (columnName == BindableBase.GetPropertyName(() => NewOwner.MailTo))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "MailTo", NewOwner.MailTo));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => NewOwner.Address))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "Address", NewOwner.Address));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => NewOwner.City))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "City", NewOwner.City));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => NewOwner.State))
                {
                    errorMsg.Append(RequiredValidationRule.CkStateAbbreviation(() => "State", NewOwner.State));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => NewOwner.Zip))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "Zip", NewOwner.Zip));
                    return errorMsg.ToString();
                }
                //// No errors found......
                return null;
            }
        }
        #region IsValid
        /// <summary>
        /// Runs validation on the view model
        /// </summary>
        /// <returns></returns>
        //public bool IsValid()
        //{
        //    StringBuilder message = new StringBuilder();

        //    message.Append(this.EnableValidationAndGetError());


        //    if (!String.IsNullOrEmpty(SelectedProperty.OwnerFName))
        //    {
        //        message.Append(RequiredValidationRule.CheckNullInput(SelectedProperty.OwnerFName, SelectedProperty.OwnerFName));
        //    }

        //    if (!string.IsNullOrEmpty(message.ToString()))
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        #endregion


    }

    /*======================================================== Disposition ============================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    public partial class ChangeOwnerViewModel : IDisposable
    {
        // Resources that must be disposed:
        public HVCCDataContext dc = null;

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
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TableForm"/> class.  (a.k.a. destructor)
        /// </summary>
        ~ChangeOwnerViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
}

