﻿namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
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

    public partial class PropertiesUpdatedViewModel : CommonViewModel, ICommandSink
    {

        public PropertiesUpdatedViewModel(IDataContext dc, object arg)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            // PropertiesDetailsView: ImportCommand should pass in an ObservableCollection<Property>.
            // The items in the collection are properties which have updates/changes from an import.
            ObservableCollection<Property> p = arg as ObservableCollection<Property>;
            ImportPropertyAction(Host.Parameter);
        }

        /* -------------------------------- Interfaces ------------------------------------------------ */
        #region Interfaces
        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }
        public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
        public virtual ISaveFileDialogService SaveFileDialogService { get { return GetService<ISaveFileDialogService>(); } }
        protected virtual IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        #endregion

        public enum ExportType { PDF, XLSX }
        public enum PrintType { PREVIEW, PRINT }

        public enum Column : int
        {
            Customer = 0,
            BillTo = 1,
            Balance = 2
        }

        int RowNum;
        /* ------------------------------------- Properties --------------------------- */
        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

        public override bool IsValid {  get { return true; } }

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
            set { }
        }

        private bool _isBusy = false;
        public override bool IsBusy
        {
            get
            {
                return _isBusy;
            }
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

        //public virtual bool DialogResult { get; protected set; }
        //public virtual string ResultFileName { get; protected set; }

        #region Properties
        /// <summary>
        /// Collection of properties
        /// </summary>
        private ObservableCollection<Property> _propertiesList = null;
        public ObservableCollection<Property> PropertiesList
        {
            get
            {
                if (this._propertiesList == null)
                {
                    //// Get the list of "Properties" from the database
                    var list = (from a in this.dc.Properties
                                select a);

                    this._propertiesList = new ObservableCollection<Property>(list);
                }
                return this._propertiesList;
            }
            //set
            //{
            //    if (this._propertiesList != value)
            //    {
            //        this._propertiesList = value;
            //        RaisePropertyChanged("PropertiesList");
            //    }
            //}
        }

        /// <summary>
        /// Collection of properties through an ownership change
        /// </summary>
        private ObservableCollection<Property> _propertiesUpdated = null;
        public ObservableCollection<Property> PropertiesUpdated
        {
            get
            {
                return this._propertiesUpdated;
            }
            set
            {
                if (this._propertiesUpdated != value)
                {
                    this._propertiesUpdated = value;
                    RaisePropertyChanged("PropertiesUpdated");
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

        #endregion

        /* ---------------------------------- Public/Private Methods ------------------------------------------ */
        #region Methods

        #endregion
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class PropertiesUpdatedViewModel : CommonViewModel, ICommandSink
    {
        #region ICommandSink Implementation
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

    public partial class PropertiesUpdatedViewModel: CommonViewModel
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
        /// Import Balance data Command
        /// </summary>
        private ICommand _importPropertyCommand;
        public ICommand ImportPropertyCommand
        {
            get
            {
                return _importPropertyCommand ?? (_importPropertyCommand = new CommandHandlerWparm((object parameter) => ImportPropertyAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void ImportPropertyAction(object parameter)
        {
            PropertiesUpdated = parameter as ObservableCollection<Property>;

            //ObservableCollection<Property> list = parameter as ObservableCollection<Property>;
            //foreach (Property p in list)
            //{
            //    Property foundProperty = (from x in dc.Properties
            //                              where x.PropertyID == p.PropertyID
            //                              select x).FirstOrDefault();

            //    foundProperty.PreviousBalance = p.PreviousBalance;
            //    foundProperty.Balance = p.Balance;
            //    foundProperty.Status = p.Status;
            //    foundProperty.IsInGoodStanding = p.IsInGoodStanding;

            //    this.PropertiesUpdated.Add(foundProperty);
            //}
            //ChangeSet cs = dc.GetChangeSet();
            //RaisePropertyChanged("DataChanged");
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
            Host.Parameter = this.SelectedProperty.PropertyID;
            Host.Execute(HostVerb.Open, "PropertyEdit");
        }

        #endregion

    }
    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    public partial class PropertiesUpdatedViewModel : IDisposable
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
        ~PropertiesUpdatedViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }

}

