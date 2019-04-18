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
    using System.Text;
    using HVCC.Shell.Common.Commands;

    public partial class OwnerBalanceUpdatedViewModel : CommonViewModel, ICommandSink
    {

        public OwnerBalanceUpdatedViewModel(IDataContext dc, object parameter)
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

        public enum Filter : int
        {
            XLSX = 1,
            CSV = 2
        }

        public enum CSVcolumn : int
        {
            Blank = 0,
            Active = 1,
            Customer = 2,
            Balance = 3
        }
        public enum XSLcolumn : int
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
        /// Collection of properties through an ownership change
        /// </summary>
        private ObservableCollection<UnmatchedBalance> _ownersUpdated = null;
        public ObservableCollection<UnmatchedBalance> OwnersUpdated
        {
            get
            {
                return this._ownersUpdated;
            }
            set
            {
                if (this._ownersUpdated != value)
                {
                    this._ownersUpdated = value;
                    RaisePropertyChanged("OwnersUpdated");
                }
            }
        }

        private UnmatchedBalance _selectedItem = null;
        public UnmatchedBalance SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (value != _selectedItem)
                {
                    _selectedItem = value;
                    RaisePropertyChanged("SelectedItem");
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
    public partial class OwnerBalanceUpdatedViewModel : CommonViewModel, ICommandSink
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
            RaisePropertyChanged("IsBusy");
            ChangeSet cs = dc.GetChangeSet();
            //this.dc.SubmitChanges();                       (DEBUG)
            this.IsBusy = false;
            RaisePropertyChanged("IsNotBusy");
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

    public partial class OwnerBalanceUpdatedViewModel : CommonViewModel
    {

        /* ---------------------------------- Commands & Actions --------------------------------------- */
        #region Commands

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
        /// Import Balance data Command
        /// </summary>
        private ICommand _importCommand;
        public ICommand ImportCommand
        {
            get
            {
                return _importCommand ?? (_importCommand = new CommandHandler(() => ImportBalanceAction(), true));
            }
        }

        /// <summary>
        /// Import Balances command action
        /// </summary>
        /// <param name="type"></param>
        public void ImportBalanceAction()
        {
            string importFileName = string.Empty;

            // Open a file chooser dialog window. Capture the user's file selection.
            OpenFileDialogService.Filter = "XLSX files|*.xlsx|CSV files|*.csv";
            OpenFileDialogService.FilterIndex = 1;
            DialogResult = OpenFileDialogService.ShowDialog();
            if (DialogResult)
            {
                IFileInfo file = OpenFileDialogService.Files.First();
                importFileName = file.GetFullName();

                if (Filter.CSV == (Filter)OpenFileDialogService.FilterIndex)
                {
                    ImportCSV(importFileName);
                }
                else if (Filter.XLSX == (Filter)OpenFileDialogService.FilterIndex)
                {
                    //ImportXLSX(importFileName);
                }
                else
                {

                }
            }
        }

        public void ImportCSV(string importFile)
        {

            // Set the busy flag so the cursor in the UI will spin to indicate something is happening.
            this.IsBusy = true;
            if (File.Exists(importFile))
            {
                int row = -1;
                int oID = 0;
                try
                {
                    ObservableCollection<UnmatchedBalance> unmatchedBalances = new ObservableCollection<UnmatchedBalance>();
                    string rawData = String.Empty;

                    using (StreamReader sr = new StreamReader(importFile))
                    {
                        // Interate through the CSV file one line at a time.  The raw string will be parsed into an array
                        // of substrings to represent the columns: <date>,<time>,<lot/customer>,<meter reading>.
                        decimal balance = 0m;
                        while (sr.Peek() >= 0)
                        {
                            rawData = sr.ReadLine();
                            String[] subStrings = rawData.Split(',');
                            row++;

                            if (row > 0)
                            {
                                Owner reading = new Owner();

                                string id = subStrings[(int)CSVcolumn.Customer].Replace("\"", "");
                                Int32.TryParse(id, out oID);
                                balance = System.Convert.ToDecimal(subStrings[(int)CSVcolumn.Balance]);

                                v_OwnerDetail owner = (from x in dc.v_OwnerDetails
                                                       where x.OwnerID == oID
                                                       select x).FirstOrDefault();

                                if (null == owner)
                                {
                                    string msg = String.Format("Owner {0:000000} is not active", oID);
                                    MessageBox.Show(msg, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                                else
                                {
                                    if (owner.Balance != balance)
                                    {
                                        UnmatchedBalance umb = new UnmatchedBalance();
                                        umb.OwnerID = oID;
                                        umb.MailTo = owner.MailTo;
                                        umb.QBbalance = balance;
                                        umb.HVbalance = (decimal)owner.Balance;

                                        unmatchedBalances.Add(umb);
                                    }
                                }
                            }
                        }
                        OwnersUpdated = unmatchedBalances;
                    }
                }
                catch (Exception ex)
                {
                    IsBusy = false;
                    MessageBoxService.ShowMessage("An error occured with the import", "ERROR", MessageButton.OK, MessageIcon.Error);
                    return;
                }
            }
            this.IsBusy = false;
        }

        /// <summary>
        /// Imports QuickBooks balances from an Excel (XSLS) file.
        /// </summary>
        /// <param name="importFileName"></param>
        //public void ImportXLSX(string importFileName)
        //{
        //    // We are only interested in (3) columns in the import file.
        //    int[] colArray = new int[3];

        //    // Set the busy flag so the cursor in the UI will spin to indicate something is happening.
        //    RaisePropertyChanged("IsBusy");

        //    //List<Relationship> OwnerList = new List<Relationship>();

        //    // Process the excel sprea-sheet to import and update the property records
        //    try
        //    {
        //        SpreadsheetControl spreadsheetControl1 = new SpreadsheetControl();
        //        IWorkbook workbook = spreadsheetControl1.Document;
        //        // Load a workbook from a stream. 
        //        using (FileStream stream = new FileStream(importFileName, FileMode.Open))
        //        {
        //            workbook.LoadDocument(stream, DocumentFormat.OpenXml);
        //            Worksheet sheet = workbook.Worksheets[1];
        //            RowCollection rowCollection = sheet.Rows;
        //            int rowCount = rowCollection.LastUsedIndex;

        //            for (int row = 0; row <= rowCount; row++)
        //            {
        //                RowNum = row + 1; // need to account for the zero offset in the spread-sheet
        //                Row currentRow = rowCollection[row];
        //                Range cellRange = currentRow.GetRangeWithAbsoluteReference();

        //                // Row[0] is the header row. We read the header to determin what the offsets
        //                // are for the Customer, Bill To and Balance columns. This way, if there are
        //                // more columns included in the import file it can handle the file format.
        //                string cellData = String.Empty;
        //                if (0 == row)
        //                {
        //                    for (int cell = 0; cell <= 25; cell++)
        //                    {
        //                        cellData = cellRange[cell].Value.ToString();
        //                        switch (cellData)
        //                        {
        //                            case "Customer":
        //                                colArray[(int)XSLcolumn.Customer] = cell;
        //                                break;
        //                            case "Balance":
        //                                colArray[(int)XSLcolumn.BillTo] = cell;
        //                                break;
        //                            case "Balance Total":
        //                                colArray[(int)XSLcolumn.Balance] = cell;
        //                                break;
        //                            default:
        //                                break;
        //                        }
        //                    }
        //                    if (0 == colArray[(int)XSLcolumn.Customer] ||
        //                         0 == colArray[(int)XSLcolumn.BillTo] ||
        //                         0 == colArray[(int)XSLcolumn.Balance])
        //                    {
        //                        throw new System.FormatException("Import file is invalid or missing required columns");
        //                    }
        //                }
        //                else
        //                {
        //                    // Get the 'Customer' data from the cell.  Using 'ConvertCustomerToProperty()', we can
        //                    // divide up the string into section-block-lot-sublot so it populates the 'importProperty'
        //                    // 
        //                    string customer = cellRange[colArray[(int)XSLcolumn.Customer]].Value.ToString();
        //                    Property importProperty = Helper.ConvertCustomerToProperty(customer);
        //                    importProperty.Customer = customer;
        //                    //importProperty.BillTo = cellRange[colArray[(int)Column.BillTo]].Value.ToString();
        //                    importProperty.Balance = Decimal.Parse(cellRange[colArray[(int)XSLcolumn.Balance]].Value.ToString());

        //                    // Look up PropertyID. If it is not-null then update the property record with the new value(s).
        //                    // Otherwise Insert it as a new property record.
        //                    Property foundProperty = dc.Properties.Where(x => x.Section == importProperty.Section &&
        //                                                                        x.Block == importProperty.Block &&
        //                                                                        x.Lot == importProperty.Lot &&
        //                                                                        x.SubLot == importProperty.SubLot).SingleOrDefault();

        //                    // In theroy, we should never find a property that isn't already in the database.
        //                    if (null == foundProperty)
        //                    {
        //                        MessageBoxService.ShowMessage("Warnning: A new property is about to be added " + importProperty.Customer);
        //                        // TO-DO:  Add handeling of user input (messagebox result)

        //                        //this.PropertiesUpdated.Add(importProperty);
        //                        //this.dc.Properties.InsertOnSubmit(importProperty);
        //                    }
        //                    else // update existing record with new/changed value(s)
        //                    {
        //                        // Check to see if the Balance amount needs to be updated
        //                        if (foundProperty.Balance != importProperty.Balance)
        //                        {
        //                            // Assign the new (updated) value to the foundProperty with the values from the import
        //                            // spread-sheet. The previous balance is kept for comparison in the datagrid.
        //                            // Balance values are inverse. Therefore, a positive balance value indicates
        //                            // the member owes money.
        //                            foundProperty.PreviousBalance = (decimal)foundProperty.Balance;
        //                            foundProperty.Balance = importProperty.Balance;
        //                            if (foundProperty.Balance > 0)
        //                            {
        //                                //foundProperty.IsInGoodStanding = false;
        //                                foundProperty.Status = "Past Due";
        //                            }
        //                            else
        //                            {
        //                                //foundProperty.IsInGoodStanding = true;
        //                                foundProperty.Status = String.Empty;
        //                            }
        //                            this.OwnersUpdated.Add(foundProperty);
        //                        }
        //                    }
        //                }
        //            }

        //            // Get the change set for the inport.
        //            ChangeSet cs = this.dc.GetChangeSet();

        //            this.IsBusy = false;
        //            workbook.Dispose();
        //            RaisePropertyChanged("IsNotBusy");
        //            CanSaveExecute = true;
        //            RaisePropertyChanged("DataChanged");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBoxService.Show("Error importing data at row " + RowNum + " Message: " + ex.Message);
        //    }
        //}

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
            UnmatchedBalance p = parameter as UnmatchedBalance;
            Owner owner = new Owner();
            owner.OwnerID = p.OwnerID;
            IsBusy = true;
            Host.Execute(HostVerb.Open, "FinancialTransaction", owner);

        }

        #endregion

    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    public partial class OwnerBalanceUpdatedViewModel : IDisposable
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
        ~OwnerBalanceUpdatedViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }

}