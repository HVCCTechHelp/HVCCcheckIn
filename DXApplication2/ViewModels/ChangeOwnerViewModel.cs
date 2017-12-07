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

        private bool _isDirty = false;
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

        #region Properties
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
                case "Add":
                    var newItems = e.NewItems;
                    foreach (Relationship r in newItems)
                    {
                        bool result = DeactivateRelationship(r);
                    }
                    break;
                case "Remove":
                    var oldItems = e.OldItems;
                    foreach (Relationship r in oldItems)
                    {
                        bool result = ActivateRelationship(r);
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
            return p;
        }

        /// <summary>
        /// Deactivate a relationship from the selected property either by making it inactive or deleting it 
        /// from the Relationships table.
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        private bool DeactivateRelationship(Relationship relationship) // TO-DO: <?> Convert to a private method
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
        private bool ActivateRelationship(Relationship relationship) //  TO-DO: <?> Convert to a private method
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
                else
                {
                    var x = (from r in SelectedProperty.Relationships
                             where r.RelationshipID == relationship.RelationshipID
                             select r).FirstOrDefault();

                    x.Active = true;

                    ChangeSet cs = dc.GetChangeSet();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("Error activating new Relationship/n" + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
                return false;
            }
        }

        private bool CheckForOwner()
        {
            try
            {
                ChangeSet cs = dc.GetChangeSet();

                // The first thing we need to do is make sure we have at lease one active Relationship
                // that is also an Owner. New Relationship records will be added to the Selected.Relationship
                // collection, but their Active status will be null. 
                IEnumerable<object> addList = (from r in cs.Inserts
                                               where (r as Relationship).RelationToOwner == "Owner"
                                               select r);
                foreach (Relationship r in addList)
                {
                    Relationship l = (from y in SelectedProperty.Relationships
                                      where y.FName == r.FName
                                      && y.LName == r.LName
                                      select y).FirstOrDefault();
                    if (null != l) { l.Active = true; }
                }

                IEnumerable<object> updateList = (from r in cs.Updates
                                                  where (r as Relationship).Active == true
                                                  && (r as Relationship).RelationToOwner == "Owner"
                                                  select r);


                int ownerCount = addList.Count() + updateList.Count();
                if (0 == ownerCount)
                { return false; }
                else { return true; }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("Relationship error: " + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
                return false;
            }
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
            if (CheckForOwner())
            {
                this.IsBusy = true;
                RaisePropertiesChanged("IsBusy");
                ChangeSet cs = dc.GetChangeSet();
                this.dc.SubmitChanges();
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

