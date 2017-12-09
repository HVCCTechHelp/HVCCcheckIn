namespace HVCC.Shell.ViewModels
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
    using System.Collections.Generic;
    using DevExpress.Xpf.Spreadsheet;
    using System.IO;
    using System.Windows;
    using HVCC.Shell.Helpers;

    public partial class PropertiesUpdatedViewModel : CommonViewModel, ICommandSink
    {

        public PropertiesUpdatedViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            CanSaveExecute = false;
            //ImportPropertyAction(parameter);
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
        public virtual bool DialogResult { get; protected set; }

        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

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
                Caption = caption[0].TrimEnd(' ') + "*";
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
        }

        /// <summary>
        /// Collection of properties through an ownership change
        /// </summary>
        private ObservableCollection<Property> _propertiesUpdated = new ObservableCollection<Property>();
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
                if (value != _selectedProperty)
                {
                    _selectedProperty = value;
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
        private bool CanSaveExecute { get; set; }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            RaisePropertiesChanged("IsBusy");
            ChangeSet cs = dc.GetChangeSet();
            //this.dc.SubmitChanges();                       (DEBUG)
            this.IsBusy = false;
            RaisePropertiesChanged("IsNotBusy");
            Host.Execute(HostVerb.Close, this.Caption);
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
    /*================================================================================================================================================*/

    public partial class PropertiesUpdatedViewModel : CommonViewModel
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
        private ICommand _importCommand;
        public ICommand ImportCommand
        {
            get
            {
                return _importCommand ?? (_importCommand = new CommandHandler(() => ImportPropertyAction(), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void ImportPropertyAction()
        {
            string importFileName = string.Empty;

            // Open a file chooser dialog window. Capture the user's file selection.
            OpenFileDialogService.Filter = "XLSX files|*.xlsx";
            OpenFileDialogService.FilterIndex = 1;
            DialogResult = OpenFileDialogService.ShowDialog();
            if (DialogResult)
            {
                IFileInfo file = OpenFileDialogService.Files.First();
                importFileName = file.GetFullName();
            }

            // We are only interested in (3) columns in the import file.
            int[] colArray = new int[3];

            // Set the busy flag so the cursor in the UI will spin to indicate something is happening.
            RaisePropertyChanged("IsBusy");

            //List<Relationship> OwnerList = new List<Relationship>();

            // Process the excel sprea-sheet to import and update the property records
            try
            {
                SpreadsheetControl spreadsheetControl1 = new SpreadsheetControl();
                IWorkbook workbook = spreadsheetControl1.Document;
                // Load a workbook from a stream. 
                using (FileStream stream = new FileStream(importFileName, FileMode.Open))
                {
                    workbook.LoadDocument(stream, DocumentFormat.OpenXml);
                    Worksheet sheet = workbook.Worksheets[1];
                    RowCollection rowCollection = sheet.Rows;
                    int rowCount = rowCollection.LastUsedIndex;

                    for (int row = 0; row <= rowCount; row++)
                    {
                        RowNum = row + 1; // need to account for the zero offset in the spread-sheet
                        Row currentRow = rowCollection[row];
                        Range cellRange = currentRow.GetRangeWithAbsoluteReference();

                        // Row[0] is the header row. We read the header to determin what the offsets
                        // are for the Customer, Bill To and Balance columns. This way, if there are
                        // more columns included in the import file it can handle the file format.
                        string cellData = String.Empty;
                        if (0 == row)
                        {
                            for (int cell = 0; cell <= 25; cell++)
                            {
                                cellData = cellRange[cell].Value.ToString();
                                switch (cellData)
                                {
                                    case "Customer":
                                        Visibility v = Visibility.Hidden;
                                        colArray[(int)Column.Customer] = cell;
                                        break;
                                    case "Bill to":
                                        colArray[(int)Column.BillTo] = cell;
                                        break;
                                    case "Balance Total":
                                        colArray[(int)Column.Balance] = cell;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            if (0 == colArray[(int)Column.Customer] ||
                                 0 == colArray[(int)Column.BillTo] ||
                                 0 == colArray[(int)Column.Balance])
                            {
                                throw new System.FormatException("Import file is invalid or missing required columns");
                            }
                        }
                        else
                        {
                            // Get the 'Customer' data from the cell.  Using 'ConvertCustomerToProperty()', we can
                            // divide up the string into section-block-lot-sublot so it populates the 'importProperty'
                            // 
                            string customer = cellRange[colArray[(int)Column.Customer]].Value.ToString();
                            Property importProperty = Helper.ConvertCustomerToProperty(customer);
                            importProperty.Customer = customer;
                            importProperty.BillTo = cellRange[colArray[(int)Column.BillTo]].Value.ToString();
                            importProperty.Balance = Decimal.Parse(cellRange[colArray[(int)Column.Balance]].Value.ToString());

                            // Look up PropertyID. If it is not-null then update the property record with the new value(s).
                            // Otherwise Insert it as a new property record.
                            Property foundProperty = dc.Properties.Where(x => x.Section == importProperty.Section &&
                                                                                x.Block == importProperty.Block &&
                                                                                x.Lot == importProperty.Lot &&
                                                                                x.SubLot == importProperty.SubLot).SingleOrDefault();

                            // In theroy, we should never find a property that isn't already in the database.
                            if (null == foundProperty)
                            {
                                MessageBoxService.ShowMessage("Warnning: A new property is about to be added " + importProperty.Customer);
                                // TO-DO:  Add handeling of user input (messagebox result)

                                //this.PropertiesUpdated.Add(importProperty);
                                //this.dc.Properties.InsertOnSubmit(importProperty);
                            }
                            else // update existing record with new/changed value(s)
                            {
                                // Check to see if the Balance amount needs to be updated
                                if (foundProperty.Balance != importProperty.Balance)
                                {
                                    // Assign the new (updated) value to the foundProperty with the values from the import
                                    // spread-sheet. The previous balance is kept for comparison in the datagrid.
                                    // Balance values are inverse. Therefore, a positive balance value indicates
                                    // the member owes money.
                                    foundProperty.PreviousBalance = (decimal)foundProperty.Balance;
                                    foundProperty.Balance = importProperty.Balance;
                                    if (foundProperty.Balance > 0)
                                    {
                                        foundProperty.IsInGoodStanding = false;
                                        foundProperty.Status = "Past Due";
                                    }
                                    else
                                    {
                                        foundProperty.IsInGoodStanding = true;
                                        foundProperty.Status = String.Empty;
                                    }
                                    this.PropertiesUpdated.Add(foundProperty);
                                }
                            }
                        }
                    }

                    // Get the change set for the inport.
                    ChangeSet cs = this.dc.GetChangeSet();

                    this.IsBusy = false;
                    workbook.Dispose();
                    RaisePropertyChanged("IsNotBusy");
                    CanSaveExecute = true;
                    RaisePropertyChanged("DataChanged");
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error importing data at row " + RowNum + " Message: " + ex.Message);
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