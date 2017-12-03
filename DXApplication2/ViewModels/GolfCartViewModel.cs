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

    public partial class GolfCartViewModel : CommonViewModel, ICommandSink
    {
        public GolfCartViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
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

        /* ------------------------------------- Golf Cart Properties and Commands --------------------------- */

        public override bool IsValid { get { return true; } }

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
                Caption = caption[0].TrimEnd(' ') + "* ";
                return true;
            }
            set
            {

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

        /// <summary>
        /// 
        /// </summary>
        private string _searchName = String.Empty;
        public string SearchName
        {
            get
            {
                return _searchName;
            }
            set
            {
                if (value != this._searchName)
                {
                    _searchName = value;
                    RaisePropertyChanged("SearchName");
                }
            }
        }

        /// <summary>
        /// The current season for reporting
        /// </summary>
        private string _timePeriod = String.Empty;
        public string TimePeriod
        {
            get
            {
                if (String.Empty == this._timePeriod)
                {
                    var season = (from s in this.dc.Seasons
                                  where s.IsCurrent == true
                                  select s).FirstOrDefault();
                    if (null != season)
                    {
                        _timePeriod = season.TimePeriod;
                    }
                }
                return _timePeriod;
            }
            set
            {
                if (value != this._timePeriod)
                {
                    _timePeriod = value;
                    RaisePropertyChanged("TimePeriod");
                }
            }
        }

        #region Property entities

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
                //// wrap the setter with a check for a null value.  This condition happens when
                //// a Relationship is selected from the Relationship grid. Therefore, when
                //// a Relationship is selected we won't null out the SelectedProperty.
                if (value != _selectedProperty)
                {
                    _selectedProperty = value;
                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

        /// <summary>
        /// A collection of relationships to display in the Relationships grid of the view
        /// </summary>
        private ObservableCollection<Relationship> _foundRelationships = null;
        public ObservableCollection<Relationship> FoundRelationships
        {
            get
            {
                return _foundRelationships;
            }
            set
            {
                if (_foundRelationships != value)
                {
                    _foundRelationships = value;
                    this.SelectedFoundRelation = _foundRelationships[0];
                    RaisePropertyChanged("FoundRelationships");
                }
            }
        }

        /// <summary>
        /// Currently selected relationship
        /// </summary>
        private Relationship _selectedFoundRelation = new Relationship();
        public Relationship SelectedFoundRelation
        {
            get
            {
                return _selectedFoundRelation;
            }
            set
            {
                if (value != _selectedFoundRelation)
                {
                    _selectedFoundRelation = value;
                    RaisePropertyChanged("SelectedFoundRelation");
                }
            }
        }

        /// <summary>
        /// A collection of registered golf carts to display in the grid of the view
        /// </summary>
        private ObservableCollection<GolfCart> _registeredCarts = null;
        public ObservableCollection<GolfCart> RegisteredCarts
        {
            get
            {
                if (_registeredCarts == null)
                {
                    var currentSeason = (from s in this.dc.Seasons
                                         where s.IsCurrent == true
                                         select s).FirstOrDefault();
                    _registeredCarts = GetRegisteredCarts(currentSeason.TimePeriod);
                }
                return _registeredCarts;
            }
            set
            {
                if (_registeredCarts != value)
                {
                    _registeredCarts = value;
                }
                RaisePropertyChanged("RegisteredCarts");
            }
        }

        /// <summary>
        /// Currently selected golf cart record
        /// </summary>
        private GolfCart _selectedCart = new GolfCart();
        public GolfCart SelectedCart
        {
            get
            {
                if (null != _selectedCart)
                {
                    var p = (from x in this.dc.Properties
                             where x.PropertyID == _selectedCart.PropertyID
                             select x).FirstOrDefault();
                    this.SelectedProperty = (Property)p;
                    _selectedCart.PropertyChanged += _selectedCart_PropertyChanged;
                }
                return _selectedCart;
            }
            set
            {
                if (value != _selectedCart)
                {
                    if (null != _selectedCart)
                    {
                        _selectedCart.PropertyChanged -= _selectedCart_PropertyChanged;
                    }
                    _selectedCart = value;
                    _selectedCart.PropertyChanged += _selectedCart_PropertyChanged;
                    RaisePropertyChanged("SelectedCart");
                }
            }
        }

        /// <summary>
        /// Summary
        ///     Raises a property changed event when the SelectedCart data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _selectedCart_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (this.IsDirty)
            //{
            RaisePropertyChanged("DataChanged");
            //}
        }

        #endregion

        /* ---------------------------------- GolfCart:Public/Private Methods ------------------------------------------ */
        #region GolfCartMethods

        /// <summary>
        /// A collection of Carts that have been registered
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<GolfCart> GetRegisteredCarts(string timePeriod)
        {
            try
            {
                //// Get the list of "Properties" from the database
                var list = (from a in this.dc.GolfCarts
                            where a.Year == timePeriod
                            select a);

                return new ObservableCollection<GolfCart>(list);
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public void RevertGolfCartEdits()
        {
            ChangeSet changeSet = this.dc.GetChangeSet();

            //// First, check the change set to see if there are pending Updates. If so,
            //// iterate over the Updates collection to see if they are for the currently
            //// selected property.  If found, remove them from the change set.
            if (0 != changeSet.Updates.Count)
            {
                foreach (var v in changeSet.Updates)
                {
                    if (typeof(GolfCart) == v.GetType())
                    {
                        this.dc.Refresh(RefreshMode.OverwriteCurrentValues, v);
                    }
                }
            }
            // The, check for Inserts and remove them....
            if (0 != changeSet.Inserts.Count)
            {
                foreach (var v in changeSet.Inserts)
                {
                    if (typeof(GolfCart) == v.GetType())
                    {
                        this.dc.GetTable(v.GetType()).DeleteOnSubmit(v);
                        this.RegisteredCarts.Remove((GolfCart)v);
                    }
                }
            }
            changeSet = this.dc.GetChangeSet();
        }

        #endregion
    }
    /*================================================================================================================================================*/

    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class GolfCartViewModel : CommonViewModel, ICommandSink
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
            RaisePropertiesChanged("DataChanged");
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
    public partial class GolfCartViewModel : CommonViewModel, ICommandSink
    {
        bool CanNameSearch = true;
        /// <summary>
        /// Check In Command
        /// </summary>
        private ICommand _nameSearchCommand;
        public ICommand NameSearchCommand
        {
            get
            {
                return _nameSearchCommand ?? (_nameSearchCommand = new CommandHandler(() => NameSearchAction(), CanNameSearch));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void NameSearchAction()
        {
            try
            {
                // Query the Relationships table to get a list of Relationships matching the last name, filtered by Owner.  
                var list = (from a in this.dc.Relationships
                            where a.LName == this.SearchName
                            && a.RelationToOwner == "Owner"
                            select a);

                ObservableCollection<Relationship> relationships = new ObservableCollection<Relationship>();
                GolfCart cart = new GolfCart();
                foreach (Relationship r in list)
                {
                    try
                    {
                        // Now check to make sure there isn't already a record in the GolfCart registration Table.
                        cart = (from g in this.RegisteredCarts
                                where g.PropertyID == r.PropertyID
                                && g.LName == r.LName
                                select g).FirstOrDefault();

                        // If no registered cart was found, then we add the name being searched for to the list of names to choose from to add.
                        if (null == cart)
                        {
                            Property property = (from c in this.dc.Properties
                                                 where c.PropertyID == r.PropertyID
                                                 select c).FirstOrDefault();
                            r.Customer = property.Customer;
                            relationships.Add(r);
                            this.FoundRelationships = relationships;
                        }
                        // otherwise, there is already a registered cart for this name & propertyID combination. In this case we make the searched
                        // for name the selected cart record in the registered cart grid.
                        else
                        {
                            this.SelectedCart = cart;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBoxService.ShowMessage("Error: " + ex.Message);
                    }
                    finally
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Search Error: " + ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Add Cart Command
        /// </summary>
        private ICommand _addCartCommand;
        public ICommand AddCartCommand
        {
            get
            {
                return _addCartCommand ?? (_addCartCommand = new CommandHandler(() => AddCartAction(), true));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void AddCartAction()
        {
            if (null != this.SelectedFoundRelation.Property)
            {
                GolfCart addItem = new GolfCart();
                addItem.FName = this.SelectedFoundRelation.FName;
                addItem.LName = this.SelectedFoundRelation.LName;
                addItem.PropertyID = this.SelectedFoundRelation.PropertyID;
                addItem.Year = this.TimePeriod;
                addItem.Quanity = 1;
                addItem.IsPaid = true;
                addItem.PaymentDate = DateTime.Now;
                try
                {
                    Property p = (from x in this.dc.Properties
                                  where x.PropertyID == this.SelectedFoundRelation.PropertyID
                                  select x).FirstOrDefault();
                    addItem.Customer = p.Customer;
                }
                catch (Exception ex)
                {
                    MessageBoxService.ShowMessage("Error: " + ex.Message);
                }

                // add the selected relationship to the registered carts collection. Then remove it from the found relationships collection. Effectively, we move it
                // from one to the other collection. Lastly, add it to the datacontext queue to be inserted on save.
                // Additionally, when the RegisteredCarts collection is modified, it will trigger a PropertyChanged event to notify
                // Main that data has been updated.
                this.RegisteredCarts.Add(addItem);
                this.dc.GolfCarts.InsertOnSubmit(addItem);

                this.SelectedCart = addItem;

                // Clear the search text box.
                this.FoundRelationships.Remove(this.SelectedFoundRelation);
                this.SearchName = String.Empty;

            }
            else
            {
                MessageBoxService.ShowMessage("Please select a name", "Warning", MessageButton.OK, MessageIcon.Warning);
            }
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
        /// Add Cart Command
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

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class GolfCartViewModel : IDisposable
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
            if (null != _selectedCart)
            {
                _selectedCart.PropertyChanged -= _selectedCart_PropertyChanged;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TableForm"/> class.  (a.k.a. destructor)
        /// </summary>
        ~GolfCartViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}
