namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Models;
    using HVCC.Shell.Resources;
    using HVCC.Shell.Validation;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    public partial class WaterMeterEditViewModel : CommonViewModel, ICommandSink
    {

        // The WaterMeterEditViewModel should only ever be created by a RowDoubleClick event.
        // 'parameter' will be a reference to the SelectedProperty of the WaterMeterViewModel
        // and registered against the WaterMeterViewModel's data context.  Threfore, any change
        // to SelectedProperty => 'parameter' will be reflected in WaterMeterViewModel's DC (pDC).
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public WaterMeterEditViewModel(IDataContext dc, object parameter, IDataContext pDC = null)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            v_WaterMeterReading r = parameter as v_WaterMeterReading;
            Property p = (from x in this.dc.Properties
                          where x.PropertyID == r.PropertyID
                          select x).FirstOrDefault();

            if (null != p)
            {
                this.SelectedProperty = p as Property;
                _originalProperty = (Property)p.Clone();
            }
            if (null != pDC)
            {
                dc = pDC;
            }
            this.RegisterCommands();

            var list = (from x in this.dc.WaterMeterReadings
                        where x.PropertyID == SelectedProperty.PropertyID
                        select x);

            MeterReadings = new ObservableCollection<WaterMeterReading>(list);

            IsBusy = false;
        }

        /* ------------------------------------- Properties --------------------------- */

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
            set
            {
                if (this._propertiesList != value)
                {
                    this._propertiesList = value;
                    RaisePropertyChanged("PropertiesList");
                }
            }
        }

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        private Property _originalProperty = new Property();
        private Property _selectedProperty = new Property();
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
                    if (null != _selectedProperty)
                    {
                        _selectedProperty.PropertyChanged -= _selectedProperty_PropertyChanged;
                    }
                    _selectedProperty = value;
                    _selectedProperty.PropertyChanged += _selectedProperty_PropertyChanged;
                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }


        private ObservableCollection<WaterMeterReading> _meterReadings = null;
        public ObservableCollection<WaterMeterReading> MeterReadings
        {
            get
            {
                return _meterReadings;
            }
            set
            {
                if (value != _meterReadings)
                {
                    _meterReadings = value;
                    RaisePropertyChanged("MeterReadings");
                }
            }
        }

        private WaterMeterReading _selectedMeterReading = null;
        public WaterMeterReading SelectedMeterReading
        {
            get
            {
                return _selectedMeterReading;
            }
            set
            {
                if (value != _selectedMeterReading)
                {
                    _selectedMeterReading = value;
                    RaisePropertyChanged("SelectedMeterReading");
                }
            }
        }

        /// <summary>
        /// Summary
        ///     Raises a property changed event when the SelectedCart data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _selectedProperty_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged("DataChanged");
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
    public partial class WaterMeterEditViewModel : CommonViewModel, ICommandSink
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

    public partial class WaterMeterEditViewModel : CommonViewModel
    {

        private bool _canViewParcel = true;
        /// <summary>
        /// View Parcel Command
        /// </summary>
        private ICommand _viewParcelCommand;
        public ICommand ViewParcelCommand
        {
            get
            {
                return _viewParcelCommand ?? (_viewParcelCommand = new CommandHandler(() => ViewParcelAction(), _canViewParcel));
            }
        }

        public void ViewParcelAction()
        {
            try
            {
                string absoluteUri = "http://parcels.lewiscountywa.gov/" + SelectedProperty.Parcel;
                Process.Start(new ProcessStartInfo(absoluteUri));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
            }
        }


        /// <summary>
        /// RowUpdated Command
        /// </summary>
        private ICommand _rowUpdatedCommand;
        public ICommand RowUpdatedCommand
        {
            get
            {
                return _rowUpdatedCommand ?? (_rowUpdatedCommand = new CommandHandlerWparm((object parameter) => RowUpdatedAction(parameter), true));
            }
        }

        /// <summary>
        /// RowUpdatedAction Action
        /// </summary>
        /// <param name="parameter"></param>
        public void RowUpdatedAction(object parameter)
        {
            GridRowValidationEventArgs e = parameter as GridRowValidationEventArgs;
            WaterMeterReading r = e.Row as WaterMeterReading;
            for (int i = 1; i < MeterReadings.Count(); i++)
            {
                MeterReadings[i].Consumption = MeterReadings[i].MeterReading - MeterReadings[i - 1].MeterReading;
            }
            CanSaveExecute = IsDirty;
        }

        /// <summary>
        /// RemoveReading Command
        /// </summary>
        private ICommand _removeReadingCommand;
        public ICommand RemoveReadingCommand
        {
            get
            {
                return _removeReadingCommand ?? (_removeReadingCommand = new CommandHandlerWparm((object parameter) => RemoveReadingAction(parameter), true));
            }
        }

        /// <summary>
        /// RemoveReadingAction Action
        /// </summary>
        /// <param name="parameter"></param>
        public void RemoveReadingAction(object parameter)
        {
            WaterMeterReading r = parameter as WaterMeterReading;
            MeterReadings.Remove(r);
            this.dc.WaterMeterReadings.DeleteOnSubmit(r);
            CanSaveExecute = IsDirty;

            // If we have more than 1 MeterReading and we delete one, we have to recalculate the consumption
            // for the set of readings.
            if (1 < MeterReadings.Count())
            {
                MeterReadings[0].Consumption = 0;
                for (int i = 1; i < MeterReadings.Count(); i++)
                {
                    MeterReadings[i].Consumption = MeterReadings[i].MeterReading - MeterReadings[i - 1].MeterReading;
                }
            }
        }
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class WaterMeterEditViewModel : IDisposable
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
        ~WaterMeterEditViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}