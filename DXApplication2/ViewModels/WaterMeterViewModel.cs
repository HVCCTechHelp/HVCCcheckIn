namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Spreadsheet;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Models;
    using HVCC.Shell.Resources;
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    public partial class WaterMeterViewModel : CommonViewModel, ICommandSink
    {

        public WaterMeterViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            this.RegisterCommands();

            PropertiesList = GetPropertiesList();
        }

        int RowNum;
        public enum Column : int
        {
            Date = 0,
            Time = 1,
            Lot = 2,
            Reading = 3
        }

        /* -------------------------------------------------------------------------------------------- */

        public virtual bool DialogResult { get; protected set; }

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
                    CanSaveExecute = false;
                    return false;
                }
                Caption = caption[0].TrimEnd(' ') + "* ";
                CanSaveExecute = true;
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
        /// Collection of properties
        /// </summary>
        private ObservableCollection<v_WaterMeterReading> _propertiesList = null;
        public ObservableCollection<v_WaterMeterReading> PropertiesList
        {
            get
            {
                return this._propertiesList;
            }
            set
            {
                if (this._propertiesList != value)
                {
                    _propertiesList = null;
                    this._propertiesList = value;
                    RaisePropertyChanged("PropertiesList");
                }
            }
        }

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        private v_WaterMeterReading _selectedProperty = null;
        public v_WaterMeterReading SelectedProperty
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

        #endregion


        /* ---------------------------------- Public/Private Methods ------------------------------------------ */
        #region Private Methods

        /// <summary>
        /// Queries the database to get the current list of property records
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<v_WaterMeterReading> GetPropertiesList()
        {
            try
            {
                //var list = (from a in this.dc.Properties
                //            select a);
                var list = (from a in this.dc.v_WaterMeterReadings
                            select a);
                return new ObservableCollection<v_WaterMeterReading>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving Property data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        #endregion
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class WaterMeterViewModel : CommonViewModel, ICommandSink
    {
        public void RegisterCommands()
        {
            this.RegisterSaveHandler();

            IsBusy = false;
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
            this.IsBusy = true;
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

    /*================================================================================================================================================*/
    public partial class WaterMeterViewModel
    {

        protected virtual IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        public virtual ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
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
        /// Refresh Command
        /// </summary>
        private ICommand _refreshCommand;
        public ICommand RefreshCommand
        {
            get
            {
                return _refreshCommand ?? (_refreshCommand = new CommandHandlerWparm((object parameter) => RefreshAction(parameter), true));
            }
        }

        /// <summary>
        /// Refresh data sources
        /// </summary>
        /// <param name="type"></param>
        public void RefreshAction(object parameter)
        {
            RaisePropertyChanged("IsBusy");
            PropertiesList = null;
            PropertiesList = GetPropertiesList();
            RaisePropertyChanged("IsNotBusy");
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
            v_WaterMeterReading p = parameter as v_WaterMeterReading;
            Host.Execute(HostVerb.Open, "WaterMeterEdit", p);
        }

        /// <summary>
        /// Import water meter readings from csv file
        /// </summary>
        private ICommand _importCommand;
        public ICommand ImportCommand
        {
            get
            {
                return _importCommand ?? (_importCommand = new CommandHandler(() => ImportReadingAction(), true));
            }
        }

        /// <summary>
        /// Import meter readings command action
        /// </summary>
        /// <param name="type"></param>
        public void ImportReadingAction()
        {
            string importFileName = string.Empty;

            // Open a file chooser dialog window. Capture the user's file selection.
            OpenFileDialogService.Filter = "CSV files|*.csv|Text file|*.txt";
            OpenFileDialogService.FilterIndex = 1;
            DialogResult = OpenFileDialogService.ShowDialog();
            if (DialogResult)
            {
                IFileInfo file = OpenFileDialogService.Files.First();
                importFileName = file.GetFullName();
            }

            if (!String.IsNullOrEmpty(importFileName))
            {
                // Set the busy flag so the cursor in the UI will spin to indicate something is happening.
                RaisePropertyChanged("IsBusy");

                // Process the CVS file to import new meter readings records
                try
                {
                    // Read each line of the file into a string array. Each element
                    // of the array is one line of the file.
                    string[] lines = System.IO.File.ReadAllLines(importFileName);

                    RowNum = 0;
                    // Process the file contents by using a foreach loop.
                    foreach (string line in lines)
                    {
                        WaterMeterReading newReading = new WaterMeterReading();
                        RowNum++;
                        string[] columns = line.Split(',');

                        /// Lets deal with the first two columns where are Date and Time. We will use the short date
                        /// format later to compare this reading to the last reading. Therefore, we always set the time
                        /// to 12:00:00AM so the time of the two records will always be the same.
                        /// 
                        string date = columns[(int)Column.Date];
                        string time = "00:00:00"; // columns[(int)Column.Time];
                        string datetime = String.Format("{0} {1}", date, time);

                        // We need to fetch the last meter reading for the property to calculate
                        // the consumption between readings
                        int pID = (from x in dc.Properties
                                   where x.Customer == columns[(int)Column.Lot]
                                   select x.PropertyID).SingleOrDefault();

                        if (0 != pID)
                        {
                            WaterMeterReading lastWMR = (from x in dc.WaterMeterReadings
                                                         where x.PropertyID == pID
                                                         orderby x.ReadingDate descending
                                                         select x).FirstOrDefault();

                            newReading.PropertyID = pID;
                            newReading.ReadingDate = DateTime.ParseExact(datetime, "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                            newReading.MeterReading = Int32.Parse(columns[(int)Column.Reading]); ;

                            if (null == lastWMR)
                            {
                                newReading.Consumption = 0;
                                dc.WaterMeterReadings.InsertOnSubmit(newReading);
                            }
                            else
                            {
                                newReading.Consumption = newReading.MeterReading - (int)lastWMR.MeterReading;
                                // Check to make sure this isn't a duplicate reading.....
                                if (newReading.ReadingDate > lastWMR.ReadingDate
                                   && newReading.MeterReading >= lastWMR.MeterReading)
                                {
                                    // Add this reading to the data context's change set
                                    dc.WaterMeterReadings.InsertOnSubmit(newReading);
                                }
                            }
                        }
                    }

                    ChangeSet cs = dc.GetChangeSet();
                    dc.SubmitChanges();
                    RaisePropertyChanged("IsNotBusy");

                    IFileInfo file = OpenFileDialogService.Files.First();
                    string processedFileName = String.Format("{0}\\Processed\\{1}", file.DirectoryName, file.Name);
                    File.Move(importFileName, processedFileName);

                    string msg = String.Format("Import complete. {0} records imported", cs.Inserts.Count());
                    MessageBox.Show(msg, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    RaisePropertyChanged("IsNotBusy");
                    MessageBox.Show("Error importing data at row " + RowNum + "\nMessage: " + ex.Message);
                }
            }
        }


        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _deleteRowCommand;
        public ICommand DeleteRowCommand
        {
            get
            {
                return _deleteRowCommand ?? (_deleteRowCommand = new CommandHandlerWparm((object parameter) => DeleteRowCommandAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void DeleteRowCommandAction(object parameter)
        {
            v_WaterMeterReading reading = parameter as v_WaterMeterReading;
            WaterMeterReading toDelete = (from x in dc.WaterMeterReadings
                                          where x.RowID == reading.RowID
                                          select x).FirstOrDefault();

            dc.WaterMeterReadings.DeleteOnSubmit(toDelete);
            CanSaveExecute = IsDirty;
        }

    }
    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class WaterMeterViewModel : IDisposable
    public partial class WaterMeterViewModel : IDisposable
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
        ~WaterMeterViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}