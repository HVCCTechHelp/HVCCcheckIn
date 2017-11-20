namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using DevExpress.Mvvm;
    using HVCC.Shell.Models;
    using System.Windows.Input;
    using System.Diagnostics;
    using HVCC.Shell.Resources;


    // This ViewModel serves as a bridge for the primary Properties VM.  It is required by the
    // IDialogService. Therefore we create virtual properties for the SelecrtedProperty and
    // WaterMeterReading collection so we can have a reference to the data elements we need.
    public partial class WaterMeterViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public WaterMeterViewModel()
        {
            //_canViewParcel = true;
        }

        #region Interfaces
        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public virtual ApplicationPermission ApplPermissions
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual Property SelectedProperty { get; set; }
        public virtual ObservableCollection<WaterMeterReading> MeterReadings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private bool _canViewParcel;
        public bool CanViewParcel
        {
            get { return this._canViewParcel; }
            set
            {
                if (value != this._canViewParcel)
                {
                    this._canViewParcel = value;
                    RaisePropertyChanged("CanViewParcel");
                }
            }
        }

        #region Public Methods
        /// <summary>
        /// Calculates difference between current and last water meter readings
        /// </summary>
        /// <param name="theProperty"></param>
        /// <returns></returns>
        public int? CalculateWaterConsumption()
        {
            int count = this.MeterReadings.Count;
            if (1 >= count)
            {
                return 0;
            }
            else
            {
                int lastReading = (int)this.MeterReadings[count - 2].MeterReading;
                int currentReading = (int)this.MeterReadings[count - 1].MeterReading;
                return currentReading - lastReading;
            }
        }
        #endregion

        #region Private Methods

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
                string absoluteUri = "http://parcels.lewiscountywa.gov/" + this.SelectedProperty.Parcel;
                Process.Start(new ProcessStartInfo(absoluteUri));
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ex.Message);
            }
            finally
            {
            }
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        #region INotifyPropertyChagned implementaiton
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
}
