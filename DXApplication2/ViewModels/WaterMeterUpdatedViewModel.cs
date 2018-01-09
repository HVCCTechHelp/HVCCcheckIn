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
    using System.IO;

    public partial class WaterMeterUpdatedViewModel : CommonViewModel, ICommandSink
    {

        public WaterMeterUpdatedViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            this.RegisterCommands();

            IsBusy = false;
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

        /* ------------------------------------- Properties and Commands --------------------------- */

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
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<WaterMeterException> _meterReadingExceptions = new ObservableCollection<WaterMeterException>();
        public ObservableCollection<WaterMeterException> MeterReadingExceptions
        {
            get
            {
                return _meterReadingExceptions;
            }
            set
            {
                if (value != _meterReadingExceptions)
                {
                    _meterReadingExceptions = value;
                    RaisePropertyChanged("MeterReadingExceptions");
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

        /// <summary>
        /// Summary
        ///     Raises a property changed event when the SelectedCart data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _selectedCart_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("DataChanged");
        }
        public virtual bool DialogResult { get; protected set; }
        public virtual string ResultFileName { get; protected set; }

        #endregion

        /* ---------------------------------- Public/Private Methods ------------------------------------------ */
        #region Methods
        /// <summary>
        /// TO-DO: Move to another VM....
        /// Adds new water meter readings to collection
        /// </summary>
        /// <param name="relationshipViewModel"></param>
        private void AddWaterMeetingReadingToCollection(WaterMeterViewModel waterSystemViewModel)
        {
            // Since the WaterMeterReading VM is virtual, we copy it back to the main (Properties) VM.
            // This updates, or copies over, changes a user has made to VM properties of the SelectedProperty
            this.SelectedProperty = waterSystemViewModel.SelectedProperty;

            // However, the MeterReadings are stored in a separate collection, so we have to iterate
            // over them and add new items to the PropertiesVM in order for them to be registered
            // in the DC's changeset.
            //foreach (WaterMeterReading mr in waterSystemViewModel.MeterReadings)
            //{
            //    if (0 == mr.RowID)
            //    {
            //        this.SelectedProperty.WaterMeterReadings.Add(mr);
            //        RaisePropertyChanged("WaterDataUpdated");
            //    }
            //}
        }

        /// <summary>
        /// Reverts changes made to the water meter reading control is there is an update pending.
        /// </summary>
        /// <param name="relationshipViewModel"></param>
        private void CancelWaterSystemAction(WaterMeterViewModel waterSystemViewModel)
        {
            bool undo = false;
            ChangeSet changeSet = this.dc.GetChangeSet();
            foreach (var v in changeSet.Updates)
            {
                if (typeof(Property) == v.GetType())
                {
                    Property p = v as Property;
                    if (this.SelectedProperty.PropertyID == p.PropertyID)
                    {
                        undo = true;
                    }
                }
                if (typeof(WaterMeterReading) == v.GetType())
                {
                    WaterMeterReading w = v as WaterMeterReading;
                    if (this.SelectedProperty.PropertyID == w.PropertyID)
                    {
                        undo = true;
                    }
                }

                if (undo)
                {
                    this.dc.Refresh(RefreshMode.OverwriteCurrentValues, v);
                    undo = false;
                }
            }
        }

        #endregion
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class WaterMeterUpdatedViewModel : CommonViewModel, ICommandSink
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
    public partial class WaterMeterUpdatedViewModel : CommonViewModel, ICommandSink
    {

        /// <summary>
        /// Import water meter readings from CipherLab terminal import file
        /// </summary>
        public void ImportMeterReading()
        {
            if (this.IsDirty)
            {
                MessageBoxService.ShowMessage("Import is not allowed when there are pending changes.", "Warning", MessageButton.OK, MessageIcon.Hand);
                return;
            }

            int[] colArray = new int[3];
            // Open a file chooser dialog window. Capture the user's file selection.
            OpenFileDialogService.Filter = "CSV files|*.csv";
            OpenFileDialogService.FilterIndex = 1;
            DialogResult = OpenFileDialogService.ShowDialog();
            if (!DialogResult)
            {
                ResultFileName = string.Empty;
            }
            else
            {
                IFileInfo file = OpenFileDialogService.Files.First();
                ResultFileName = file.GetFullName();
            }

            // Set the busy flag so the cursor in the UI will spin to indicate something is happening.
            this.IsBusy = true;
            if (File.Exists(ResultFileName))
            {
                try
                {
                    string rawData = String.Empty;

                    using (StreamReader sr = new StreamReader(ResultFileName))
                    {
                        // Interate through the CSV file one line at a time.  The raw string will be parsed into an array
                        // of substrings to represent the columns: <date>,<time>,<lot/customer>,<meter reading>.
                        while (sr.Peek() >= 0)
                        {
                            WaterMeterReading reading = new WaterMeterReading();

                            // RawData format: 09/20/2017,17:27:01,06-02-021,12345
                            rawData = sr.ReadLine();
                            String[] subStrings = rawData.Split(',');

                            // Concatinate the date and time into a single string, then convert it to
                            // a DateTile type.
                            string dt = String.Format("{0} {1}", subStrings[0], subStrings[1]);
                            reading.ReadingDate = DateTime.ParseExact(dt, "MM/dd/yyyy HH:mm:ss", null);

                            // Using Linq2Obj, query the Properties collection using the 'customer' string to get
                            // the unique PropertyID
                            // NOTE: Because 3of9 barcode does not support the <sp> character, I have encoded
                            //       <sp> as the '/' character in the lot barcodes. Therefore, we have to replace
                            //       the '/' with a <sp>
                            string customer = subStrings[2].Replace('/', ' ');

                            // Query the Properties table to get the PropertyID for the customer string
                            var pID = (from c in dc.Properties
                                       where customer == c.Customer
                                       select c).FirstOrDefault();

                            // If we can't match up the customer string scanned from the bar code book, we have
                            // an issue.....
                            if (null == pID)
                            {
                                reading.PropertyID = 0;
                            }
                            else
                            {
                                reading.PropertyID = pID.PropertyID;
                            }

                            // Assign the current meter reading....
                            reading.MeterReading = Int32.Parse(subStrings[3]);

                            // Get the last meter reading so the current consumption can be calculated.
                            var lmr = (from m in this.dc.WaterMeterReadings
                                       where m.PropertyID == pID.PropertyID
                                       orderby m.ReadingDate descending
                                       select m).FirstOrDefault();

                            if (null == lmr)
                            {
                                reading.Consumption = 0;
                            }
                            else
                            {
                                // Calculate the consumption between this reading and the last reading
                                reading.Consumption = reading.MeterReading - lmr.MeterReading;

                                // Check to make sure the last meter reading date is not the current date, or a date
                                // greater than the current reading date.
                                if (reading.ReadingDate <= lmr.ReadingDate)
                                {
                                    //MessageBoxService.ShowMessage("The meter read date is the same or older than the last meter read date.\nThe import will be terminated", "ERROR", MessageButton.OK, MessageIcon.Error);
                                    //return;
                                    this.MeterReadingExceptions.Add(new WaterMeterException()
                                    {
                                        Customer = customer,
                                        CurrentMeterReadingDate = reading.ReadingDate,
                                        LastMeterReadingDate = lmr.ReadingDate,
                                        CurrentMeterReading = reading.MeterReading,
                                        LastMeterReading = null
                                    });
                                }

                                // Do a bit of checking to make sure the delta value isn't wonky...
                                else if (0 > reading.Consumption ||
                                        1000 <= reading.Consumption)
                                {
                                    this.MeterReadingExceptions.Add(new WaterMeterException()
                                    {
                                        Customer = customer,
                                        CurrentMeterReadingDate = reading.ReadingDate,
                                        LastMeterReadingDate = null, //lmr.ReadingDate,
                                        CurrentMeterReading = reading.MeterReading,
                                        LastMeterReading = lmr.MeterReading
                                    });
                                }
                                else
                                {
                                    // Add this reading to pending inserts......
                                    this.dc.WaterMeterReadings.InsertOnSubmit(reading);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxService.ShowMessage("An error occured with the import", "ERROR", MessageButton.OK, MessageIcon.Error);
                    return;
                }
            }
            // TO-DO:  Move to the SaveCommand.....
            ChangeSet cs = dc.GetChangeSet();
            dc.SubmitChanges();
            this.IsBusy = false;

            //string message = String.Format("Import complete. {0} records imported", cs.Inserts.Count);
            //MessageBoxService.ShowMessage(message, "Information", MessageButton.OK, MessageIcon.Information);
            RaisePropertyChanged("MeterExceptions");
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
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    public partial class WaterMeterUpdatedViewModel : IDisposable
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
        ~WaterMeterUpdatedViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
}

