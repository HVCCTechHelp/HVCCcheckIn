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
    using System.Collections.Generic;

    public partial class ChangeOwnerViewModel : CommonViewModel, ICommandSink
    {

        public ChangeOwnerViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            Property p = parameter as Property;
            if (null != p)
            {
                SelectedProperty = GetProperty(p.PropertyID);
                OriginalProperty = SelectedProperty.Clone() as Property;
                SelectedProperty.BillTo = string.Empty;
                // Relationships = new ObservableCollection<Relationship>(p.Relationships) ;
                ObservableCollection<Relationship> tmpRC = new ObservableCollection<Relationship>();
                foreach (Relationship r in p.Relationships)
                {
                    Relationship x = r.Clone() as Relationship;
                    tmpRC.Add(x);
                }
                Relationships = tmpRC;
            }
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            CanSaveExecute = false;
            this.RegisterCommands();
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
        public override bool IsValid { get; }

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

        #region Properties
        /// <summary>
        /// Original Property record reference passed in (selected property) from a property grid view
        /// </summary>
        private Property _originalProperty = new Property();
        public Property OriginalProperty
        {
            get
            {
                return _originalProperty;
            }
            set
            {
                if (value != this._originalProperty)
                {
                    this._originalProperty = value;
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
                }
            }
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<Relationship> _relationships = null;
        public ObservableCollection<Relationship> Relationships
        {
            get
            {
                this._relationships.CollectionChanged += _relationshipsToProcess_CollectionChanged;
                return this._relationships;
            }
            set
            {
                if (_relationships != value)
                {
                    _relationships = value;
                }
            }
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<Relationship> _relationshipsToProcess = new ObservableCollection<Relationship>();
        public ObservableCollection<Relationship> RelationshipsToProcess
        {
            get
            {
                this._relationshipsToProcess.CollectionChanged += _relationshipsToProcess_CollectionChanged;
                return this._relationshipsToProcess;
            }
            set
            { }
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
                // Therefore, the logic needs to determine which asction (Add or Remove) needs to happen.
                case "Add":
                    var newItems = e.NewItems;
                    foreach (Relationship r in newItems)
                    {
                        if (0 != r.RelationshipID)
                        {
                            bool result = Helper.RemoveRelationship(this.dc, this.SelectedProperty, r);
                        }
                        else
                        {
                            bool result = Helper.AddRelationship(this.dc, this.SelectedProperty, r);
                        }
                    }
                    break;
                case "Remove":
                    var oldItems = e.OldItems;
                    foreach (Relationship r in oldItems)
                    {
                        bool result = Helper.AddRelationship(this.dc, this.SelectedProperty, r);
                    }
                    break;
            }
            if (0 < _relationshipsToProcess.Count()) { CanSaveExecute = true; } else { CanSaveExecute = false; }
            RaisePropertyChanged("DataChanged");
        }

        /* ====== Keep for reference ========= */
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

        ///// <summary>
        ///// Listen for changes to a collection item property change
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void ListItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    CanSaveExecute = IsDirty;
        //    RaisePropertyChanged("DataChanged");
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

    /*================================================================================================================================================*/
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
            if (Helper.CheckForOwner(this.dc, this.SelectedProperty))
            {
                this.IsBusy = true;
                RaisePropertiesChanged("IsBusy");

                OwnershipChange oc = new OwnershipChange();
                oc.NewOwner = SelectedProperty.BillTo;
                oc.PreviousOwner = OriginalProperty.BillTo;
                dc.OwnershipChanges.InsertOnSubmit(oc);

                ChangeSet cs = dc.GetChangeSet();
                //this.dc.SubmitChanges();
                this.IsBusy = false;
                RaisePropertiesChanged("IsNotBusy");
                Host.Execute(HostVerb.Close, this.Caption);
            }
            else
            {
                MessageBoxService.ShowMessage("You must have at lease one owner.", "Warning", MessageButton.OK, MessageIcon.Warning);
            }
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
    public partial class ChangeOwnerViewModel : CommonViewModel, ICommandSink
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

        /// <summary>
        /// RowDoubleClick Command
        /// </summary>
        //private ICommand _rowDoubleClickCommand;
        //public ICommand RowDoubleClickCommand
        //{
        //    get
        //    {
        //        return _rowDoubleClickCommand ?? (_rowDoubleClickCommand = new CommandHandlerWparm((object parameter) => RowDoubleClickAction(parameter), true));
        //    }
        //}

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        //public void RowDoubleClickAction(object parameter)
        //{
        //    object o = parameter;
        //    Host.Parameter = this.SelectedProperty;
        //    Host.Execute(HostVerb.Open, "Edit");

        //}
    }

    /*================================================================================================================================================*/
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

