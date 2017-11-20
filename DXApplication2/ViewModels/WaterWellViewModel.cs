namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Linq;
    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;
    using HVCC.Shell.Models;
    using DevExpress.Spreadsheet;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Input;
    using Resources;
    using System.Globalization;

    [POCOViewModel]
    public partial class WaterWellViewModel : ViewModelBase, INotifyPropertyChanged
    {
        /* ------------------------------ ViewModel Constructor --------------------------------------- */
        /// <summary>
        /// ViewModel Constructor
        /// </summary>
        public WaterWellViewModel()
        {

        }

        /* -------------------------------- Interfaces ------------------------------------------------ */
        #region Interfaces
        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }

        #endregion // Interfaces

        #region Enumberables 
        public enum ExportType
        {
            PDF,
            XLSX
        }
        public enum PrintType
        {
            PREVIEW,
            PRINT
        }

        #endregion //Types

        /* ------------------------------------- Boolean Properties ------------------------------------------ */
        #region Booleans

        /// <summary>
        /// Controls the enable/disable state of the Save action button
        /// </summary>
        private bool _isEnabledSave = true;  // Default: false
        public bool IsEnabledSave
        {
            get { return this._isEnabledSave; }
            set
            {
                if (value != this._isEnabledSave)
                {
                    this._isEnabledSave = value;
                    RaisePropertyChanged("IsEnabledSave");
                }
            }
        }

        /// <summary>
        /// Well Meter Validation
        /// </summary>
        public string IsMeterReadingsValid
        {
            get
            {
                foreach (WellMeterReading m in WellMeterReadings)
                {
                    // Rule-1: Make sure all rows have data
                    if (null == m.ThroughputInGallons)
                    {
                        return "All values must be entered";
                    }
                    // Rule-2: Make sure there are not already entries for the month being entered.
                    var r2 = (from r in this.WellMeterReadingHistory
                              where m.MeterReadingDate.Month == r.MeterReadingDate.Month
                              && m.MeterReadingDate.Year == r.MeterReadingDate.Year
                              select r).FirstOrDefault();
                    if (null != r2)
                    {
                        return "There are already entries for this Month and Year";
                    }
                }
                return String.Empty;
            }
        }
        #endregion // Booleans

        /* ------------------------------------- Water Well Properties ------------------------ */
        #region Properties
        public PropertiesViewModel ParentViewModel
        { get; set; }

        /// <summary>
        /// The list of unit types used in the grid drop-down control
        /// </summary>
        private ObservableCollection<WaterWell> _wellList = new ObservableCollection<WaterWell>();
        public ObservableCollection<WaterWell> WellList
        {
            get
            {
                if (0 == this._wellList.Count)
                {
                    var list = (from w in this.dc.WaterWells
                                select w);
                    if (null != list)
                    {
                        foreach (var i in list)
                        {
                            _wellList.Add(new WaterWell() { WellNumber = i.WellNumber, ServiceArea = i.ServiceArea });
                        }
                    }
                }
                return _wellList;
            }
        }

        private List<Unit> _unitList = new List<Unit>();
        public List<Unit> UnitList
        {
            get
            {
                if (0 == _unitList.Count)
                {
                    _unitList.Add(new Unit() { Description = "CubicFt", Value = 7.48 });
                    _unitList.Add(new Unit() { Description = "Gallons", Value = 1.00 });
                }
                return _unitList;
            }
        }

        /// <summary>
        /// Common date of well meter reading
        /// </summary>
        private DateTime _readingDate = DateTime.Now;
        public DateTime ReadingDate
        {
            get
            {
                return _readingDate;
            }
            set
            {
                if (this._readingDate != value)
                {
                    this._readingDate = value;
                    UpdateMeterReadings();
                }
                RaisePropertyChanged("ReadingDate");
            }

        }

        /// <summary>
        /// Collection of well meter readings
        /// </summary>
        private ObservableCollection<WellMeterReading> _wellMeterReadings = new ObservableCollection<WellMeterReading>();
        public ObservableCollection<WellMeterReading> WellMeterReadings
        {
            get
            {
                if (0 == _wellMeterReadings.Count())
                {
                    SeedMeterReadings(_wellMeterReadings);
                }
                return _wellMeterReadings;
            }
            set
            {
                if (this._wellMeterReadings != value)
                {
                    this._wellMeterReadings = value;
                }
                RaisePropertyChanged("WellMeterReadings");
            }
        }

        /// <summary>
        /// Collection of past well meter readings
        /// </summary>
        private ObservableCollection<WellMeterHistory> _wellMeterReadingHistory = new ObservableCollection<WellMeterHistory>();
        public ObservableCollection<WellMeterHistory> WellMeterReadingHistory
        {
            get
            {
                if (null != _wellMeterReadingHistory &&
                    0 <= _wellMeterReadingHistory.Count())
                {
                    _wellMeterReadingHistory.Clear();
                }

                // Query the WellMeterReadings table to get the raw data ordered by and grouped by the MeterReadingDate
                var query = from s in dc.WellMeterReadings
                            orderby s.MeterReadingDate.Date
                            group s by new { s.MeterReadingDate.Date } into gss
                            select new
                            {
                                gss.Key.Date,
                                WellMeterReadings = gss.ToArray(),
                            };

                // Query the WellList or WellMeterReadings table to get the unique list well numbers
                var wellList = this.WellList //dc.WellMeterReadings
                    .Select(r => r.WellNumber)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToArray();

                // Now create Key/Value pairs <Date, MeterReading> using the WellNumber as the Key, and the Readings as the value.
                // The resulting output array will be ordered/indexed by the Well# [0=W3, 1=W5, 2=W7, 3=W8, 4=W10]
                var wellReadings = from q in query.ToArray()
                                   let lookup = q.WellMeterReadings
                                   .ToLookup(s => s.WellNumber)
                                   select new
                                   {
                                       q.Date,
                                       Readings = wellList
                                       .Select(n => lookup[n])
                                       .ToArray(),
                                   }
                              ;

                // Now pivot the data into a collection the grid can display.....
                var reading = new WellMeterReading();

                foreach (var x in wellReadings)
                {
                    WellMeterHistory wmh = new WellMeterHistory();
                    wmh.MeterReadingDate = x.Date;
                    wmh.Month = x.Date.ToString("MMM", CultureInfo.InvariantCulture);
                    wmh.Year = x.Date.Year.ToString();
                    reading = x.Readings[0].SingleOrDefault<WellMeterReading>();

                    if (null != reading) { wmh.Well3 = reading.ThroughputInGallons; }
                    else { wmh.Well3 = 0; }
                    reading = x.Readings[1].SingleOrDefault<WellMeterReading>();
                    if (null != reading) { wmh.Well5 = reading.ThroughputInGallons; }
                    else { wmh.Well5 = 0; }
                    reading = x.Readings[2].SingleOrDefault<WellMeterReading>();
                    if (null != reading) { wmh.Well7 = reading.ThroughputInGallons; }
                    else { wmh.Well7 = 0; }
                    reading = x.Readings[3].SingleOrDefault<WellMeterReading>();
                    if (null != reading) { wmh.Well8 = reading.ThroughputInGallons; }
                    else { wmh.Well8 = 0; }
                    reading = x.Readings[4].SingleOrDefault<WellMeterReading>();
                    if (null != reading) { wmh.Well10 = reading.ThroughputInGallons; }
                    else { wmh.Well10 = 0; }
                    wmh.Total = wmh.Well3 + wmh.Well5 + wmh.Well7 + wmh.Well8 + wmh.Well10;

                    _wellMeterReadingHistory.Add(wmh);
                }

                return _wellMeterReadingHistory;
            }
            set
            {
                if (this._wellMeterReadingHistory != value)
                {
                    this._wellMeterReadingHistory = value;
                }
                RaisePropertyChanged("WellMeterReadingHistory");
            }
        }

        #endregion // Properties

        // ************************************* Public Methods *****************************************************
        #region Public Methods
            public void MeterReadingToGallons(object sender, DevExpress.Xpf.Grid.GridCellValidationEventArgs e)
        {
            int wellNumber = this.WellMeterReadings[e.RowHandle].WellNumber;
            double units = 1.0;
            double gallons = 0;
            int xf = 0;
            double value = 0;

            try
            {
                WellMeterReading wmr = (WellMeterReading)e.Row;
                Int32.TryParse(e.Value.ToString(), out xf);
                value = (double)xf;

                WellMeterReading lastReading = (from r in this.dc.WellMeterReadings
                                   where wellNumber == r.WellNumber
                                   orderby r.MeterReadingDate descending
                                   select r).FirstOrDefault();

                if (null == lastReading)
                {
                    MessageBoxService.ShowMessage("No previous readings, please enter the gallons output mannually.");
                }
                else
                {
                    if ("CubicFt" == wmr.MeterUnitOfMeasure)
                    {
                        units = this.UnitList.Find(Unit => Unit.Description == wmr.MeterUnitOfMeasure).Value;
                        gallons = (double)(value * units);
                        this.WellMeterReadings[e.RowHandle].ThroughputInGallons = (long)Math.Round(gallons);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("No previous readings, please enter the gallons output mannually.");
            }
            finally
            {

            }
        }

        #endregion

        // *********************************** Private Methods *******************************************************
        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void SeedMeterReadings(ObservableCollection<WellMeterReading> MeterReadings)
        {
            MeterReadings.Clear();
            foreach (WaterWell w in WellList)
            {
                MeterReadings.Add(new WellMeterReading()
                {
                    WellNumber = w.WellNumber,
                    MeterReadingDate = this.ReadingDate,
                    MeterReading = null,
                    MeterUnitOfMeasure = "CubicFt",
                    ThroughputInGallons = null
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void UpdateMeterReadings()
        {
            foreach (WellMeterReading m in this.WellMeterReadings)
            {
                m.MeterReadingDate = this.ReadingDate;
            }
            RaisePropertyChanged("WellMeterReadings");
        }

        #endregion

        /* ------------------------------------------------ Commands ------------------------------------------------ */
        #region Commands

        /// <summary>
        /// Save Command
        /// </summary>
        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                string error = String.Empty; return _saveCommand ?? (_saveCommand = new CommandHandler(() => SaveAction(), this.IsEnabledSave));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveAction()
        {
            // Not the best implementation....  Run validation on the data entered before
            // trying to commit it to the database.
            if (String.IsNullOrEmpty(this.IsMeterReadingsValid))
            {
                foreach (WellMeterReading wmr in this.WellMeterReadings)
                {
                    this.dc.WellMeterReadings.InsertOnSubmit(wmr);
                }

                //ChangeSet cs = dc.GetChangeSet();  // <Info This is only for debugging.......
                this.dc.SubmitChanges();

                // After saving the reading entries, we clear the collections and raise a property changed event
                // on the history collection so it will reflect the update in the UI
                this.WellMeterReadings.Clear();
                this.WellMeterReadingHistory.Clear();
                this.IsEnabledSave = false;
                RaisePropertyChanged("WellMeterReadingHistory");
            }
            else
            {
                string msg = String.Format("Input not valid: {0}", this.IsMeterReadingsValid);
                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion // commands

        /*================================================================================================================================================*/

        /* --------------------------- INotify Property Change Implementation ----------------------------- */
        #region INotifyPropertyChagned implementaiton
        /// <summary>
        /// INotifyPropertyChanged Implementation
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// EventHandler: OnPropertyChanged raises a handler to notify a property has changed.
        /// </summary>
        /// <param name="propertyName">The name of the property being changed</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }

    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class WaterWellViewModel : IDisposable
    {
        // Resources that must be disposed:
        private HVCCDataContext dc = new HVCCDataContext();

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
            // No op.
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TableForm"/> class.  (a.k.a. destructor)
        /// </summary>
        ~WaterWellViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}
