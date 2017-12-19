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
    using System.Data.Linq.SqlClient;
    using System.Windows;
    using System.Collections.Generic;

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

        ///// <summary>
        ///// A collection of registered golf carts to display in the grid of the view
        ///// </summary>
        //private ObservableCollection<GolfCart> _registeredCarts = null;
        //public ObservableCollection<GolfCart> RegisteredCarts
        //{
        //    get
        //    {
        //        if (_registeredCarts == null)
        //        {
        //            var currentSeason = (from s in this.dc.Seasons
        //                                 where s.IsCurrent == true
        //                                 select s).FirstOrDefault();
        //            _registeredCarts = GetRegisteredCarts(currentSeason.TimePeriod);
        //        }
        //        return _registeredCarts;
        //    }
        //}

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
        /// The Owner record for the selected property
        /// </summary>
        private ObservableCollection<Owner> _owners = null;
        public ObservableCollection<Owner> Owners
        {
            get
            {
                return _owners;
            }
            set
            {
                if (_owners != value)
                {
                    _owners = value;
                    RaisePropertyChanged("Owners");
                }
            }
        }

        /// <summary>
        /// Currently selected golf cart record
        /// </summary>
        private Owner _selectedOwner = new Owner();
        public Owner SelectedOwner
        {
            get
            {
                return _selectedOwner;
            }
            set
            {
                if (value != _selectedOwner)
                {
                    _selectedOwner = value;
                }
            }
        }

        public ObservableCollection<GolfCart> RegisteredGolfCarts
        {
            get
            {
                try
                {
                    var list = (from x in dc.GolfCarts
                                select x);

                    return new ObservableCollection<GolfCart>(list);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
        }

        /// <summary>
        /// Currently selected golf cart record
        /// </summary>
        private GolfCart _selectedCartOwner = null;
        public GolfCart SelectedCartOwner
        {
            get
            {
                return _selectedCartOwner;
            }
            set
            {
                if (value != _selectedCartOwner)
                {

                    _selectedCartOwner = value;
                    RaisePropertyChanged("SelectedCartOwner");
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

        //public IQueryable<v_GolfCartRegistration> SearchForOwnerOfRegisteredCart(string qs)
        //{
        //    var q = RegisteredGolfCarts.AsQueryable();

        //    var likestr = string.Format("%{0}%", qs);
        //    q = q.Where(x => x.MailTo.Contains(qs));

        //    return q as IQueryable<v_GolfCartRegistration>;
        //}

        /// <summary>
        /// 
        /// </summary>
        public void NameSearchAction()
        {
            try
            {
                var owners = (from x in dc.GolfCarts
                              select x);

                // If no owners are found in current collection of known carts, then this name (& cart) needs
                // to be registered.
                if (1 == owners.Count())
                {
                    MessageBox.Show("Owner already has registered cart", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    //v_GolfCartRegistration foundOwner = owners.ElementAt(0);
                    //var element = (from x in RegisteredGolfCarts
                    //               where x.CartID == foundOwner.CartID
                    //               select x).SingleOrDefault();

                    //SelectedCartOwner = element as v_GolfCartRegistration;
                }
                // The search name does not exist in the collection of known/registered cart,
                // so we will search the Owners table for a match.
                else if (1 < owners.Count())
                {
                    var ownerList = (from o in dc.Owners
                                     select o);

                    var q = ownerList.AsQueryable();

                    var likestr = string.Format("%{0}%", SearchName);
                    q = q.Where(x => x.MailTo.Contains(likestr));
                    IQueryable<Owner> matchingOwners = q as IQueryable<Owner>;

                    if (0 < matchingOwners.Count())
                    {
                        Owners = new ObservableCollection<Owner>(matchingOwners);
                    }
                }
                else
                {
                    MessageBox.Show("No names match your search criteria", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Search Error: " + ex.Message);
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
            MessageBox.Show("Not Implemented", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
