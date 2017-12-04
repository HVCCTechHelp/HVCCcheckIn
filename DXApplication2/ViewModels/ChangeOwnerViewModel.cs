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


    public partial class ChangeOwnerViewModel : CommonViewModel, ICommandSink
    {

        public ChangeOwnerViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            if (null != this.Host.Parameter)
            {
                int pID;
                Int32.TryParse(this.Host.Parameter.ToString(), out pID);
                this.SelectedProperty = GetProperty(pID);
            }
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

        public override bool IsValid { get; }

        private bool _isDirty = false;
        public override bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                _isDirty = value;
            }
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
        /// A collection of relationships to act upon, generlly related to ownership additions & deletetions
        /// </summary>
        private ObservableCollection<Relationship> _relationshipsToProcess = null;
        public ObservableCollection<Relationship> RelationshipsToProcess
        {
            get
            {
                if (this._relationshipsToProcess == null)
                {
                    var list = (from r in dc.Relationships
                                where r.PropertyID == SelectedProperty.PropertyID
                                select r);

                    _relationshipsToProcess = new ObservableCollection<Relationship>(list);
                    //this.RegisterForChangedNotification<Relationship>(_relationshipsToProcess);
                }
                return this._relationshipsToProcess;
            }
            set
            {
                if (_relationshipsToProcess != value)
                {
                    _relationshipsToProcess = value;
                    RaisePropertiesChanged("RelationshipsToProcess");
                }
            }
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<Relationship> _relationshipsToDelete = new ObservableCollection<Relationship>();
        public ObservableCollection<Relationship> RelationshipsToDelete
        {
            get
            {
                //this.RegisterForChangedNotification<Relationship>(_relationshipsToDelete);
                _relationshipsToDelete.CollectionChanged += _relationshipsToDelete_CollectionChanged;
                return this._relationshipsToDelete;
            }
            set
            {
                if (value != this._relationshipsToDelete)
                {
                    this._relationshipsToDelete = value;
                    RaisePropertyChanged("RelationshipsToDelete");
                }
            }
        }

        private void _relationshipsToDelete_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Seleccted in the grid
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                IsDirty = true;
                string[] caption = Caption.ToString().Split('*');
                Caption = caption[0].TrimEnd(' ') + "*";
            }
            // Unselected in the grid
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                if (0 == RelationshipsToDelete.Count())
                {
                    IsDirty = false;
                    string[] caption = Caption.ToString().Split('*');
                    Caption = caption[0].TrimEnd(' ');
                }
            }
            //if (null != a)
            //{
            //    dc.Relationships.DeleteOnSubmit(a);
            //}
            //ChangeSet cs = dc.GetChangeSet();

            RaisePropertyChanged("DataChanged");
        }

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        public Property SelectedProperty { get; set; }

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
        //    // TO-DO: add logic to handle property changes..... (IsDirty?)
        //}

        #endregion

        /* ---------------------------------- Public/Private Methods ------------------------------------------ */
        #region Methods

        private Property GetProperty(int pID)
        {
            Property p = (from x in dc.Properties
                          where x.PropertyID == pID
                          select x).FirstOrDefault();
            return p;
        }

        /// <summary>
        /// Change ownership of property, called from the Save command
        /// </summary>
        private void ExecuteOwnershipChanges()
        {
            //// The Ownership changes are bound to two different properties, RelationshipsToProcess and RelationshipsToDelete.
            //// RelationshipToProcess is the full collection of Relationships to be acted on (added or removed), while
            //// RelationshipsToDelete (the selected items) are a sub-set.  This is required by the grid in order to show
            //// the full collection and the items selected. This way, the two collections (deletions/additions) are managed separately. 
            try
            {
                //// First, iterate over the RelationshipToDelete collection and add them to the data context's pending
                //// deletions collection so they will be removed on Save().  Then remove them from the RelationshipToProcess
                //// collection.  This will result in the collection just containing items that must be added to the 
                //// data context's Insert collection on Save().  Lastly, use the common IsDirty flag to indicate the
                //// data context is dirty, which will enable the Save() command on the Main ribbon.
                foreach (Relationship r in this.RelationshipsToDelete)
                {
                    // v1.3.1.x Relationships can no longer be deleted if there are FacilitiesUsage records
                    // associated to the RelationshipID due to the FK restraint. Rather, they 
                    // are now de-activated.
                    DeactivateRelationship(r);
                }

                // If a Relationship does not have an ID, we add it as a new Relationship
                foreach (Relationship r in this.RelationshipsToProcess)
                {
                    if (0 == r.RelationshipID &&
                        !this.RelationshipsToDelete.Contains(r))
                    {
                        this.AddRelationship(r);
                    }
                }

                //Property foundProperty = (from x in PropertiesUpdated
                //                          where (x.Section == SelectedProperty.Section &&
                //                                 x.Block == SelectedProperty.Block &&
                //                                 x.Lot == SelectedProperty.Lot &&
                //                                 x.SubLot == SelectedProperty.SubLot)
                //                          select x).FirstOrDefault();
                ////
                //// If we have come by way of an Import, then the collection 'PropertiesUpdated' needs to be updated.
                //// The Import results grid is bound to 'PropertiesUpdates' collection, so once we remove the relationships for
                //// the curented (selected) property, we remove that reference from the collection; effectively popping
                //// it off the list.
                //if (null != foundProperty)
                //{
                //    PropertiesUpdated.Remove(foundProperty);
                //}

                //// Lastly, null out the two collections so old data doesn't linger.
                //this.RelationshipsToProcess = null;
                //this.RelationshipsToDelete = null;
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error changing owner:" + ex.Message);
            }
        }

        /// <summary>
        /// Deactivate a relationship from the selected property either by making it inactive or deleting it 
        /// from the Relationships table.
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public bool DeactivateRelationship(Relationship relationship) // TO-DO: <?> Convert to a private method
        {
            try
            {
                // Check to see if this Relationship is in the database (has a non-zero ID), 
                // or is pending insertion (has a zero ID).
                if (0 != relationship.RelationshipID)
                {

                    // Test to see if the relationship being removed has any FacilitiesUage records.
                    // If not, we can delete the relationship record. Otherwise, we have to set
                    // the records to inactive.
                    FacilityUsage chkR = (from x in this.dc.FacilityUsages
                                          where x.RelationshipId == relationship.RelationshipID
                                          select x).FirstOrDefault();

                    if (null == chkR)
                    {
                        this.dc.Relationships.DeleteOnSubmit(relationship);
                    }
                    else
                    {
                        relationship.Active = false;
                    }
                }
                else
                {
                    // This is a pending insert, so we can simply remove the record from the in-memory
                    // store.  We raise a PropertiesList property change event to force any/all bound
                    // views to be updated.
                    this.SelectedProperty.Relationships.Remove(relationship);
                }
                //}
                //ChangeSet cs = dc.GetChangeSet();  // <I> This is only for debugging.......
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("Error: " + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Adds a relationship for the selected property
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public bool AddRelationship(Relationship relationship) //  TO-DO: <?> Convert to a private method
        {
            try
            {
                // Check to see if this Relationship is in the database (has a non-zero ID), 
                // or is pending insertion (has a zero ID).  We only want to process new
                // relationships.
                if (0 == relationship.RelationshipID)
                {
                    relationship.Active = true;
                    // Add the default HVCC image to the relationship record.  
                    relationship.Photo = (this.Host.AppDefault as ApplicationDefault).Photo;
                    // Add this relationship to the pending database changes. Actual update isn't
                    // immeidate and is dependent on user clicking the 'save' button.
                    this.dc.Relationships.InsertOnSubmit(relationship);
                    // Because of the way I implemented the Relationship grid in the edit dialog,
                    // the new relationship also needs to be manually added to the VM collection of 
                    // the selected property.  [It is not automaticly added to the collection via the datacontext
                    // even after the new Relationship is added to the database.]
                    this.SelectedProperty.Relationships.Add(relationship);
                }
                else { }
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("Error adding new Relationship/n" + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Reverts changes made to the Relationship collection of the current selected Property
        /// </summary>
        public void RevertRelationshipActions()
        {
            this.RelationshipsToDelete = null;
            //this.RelationshipsToProcess = null;
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

            foreach (Relationship r in RelationshipsToProcess)
            {
                AddRelationship(r);
            }
            foreach (Relationship r in RelationshipsToDelete)
            {
                DeactivateRelationship(r);
            }

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

    public partial class ChangeOwnerViewModel : CommonViewModel, ICommandSink
    {

        /* ---------------------------------- Commands & Actions --------------------------------------- */
        #region Commands
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
        /// Print Command
        /// </summary>
        private ICommand _rowDoubleClickCommand;
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

        #endregion

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

