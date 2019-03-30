using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using DevExpress.XtraEditors.DXErrorProvider;
using HVCC.Shell.Models.Financial;
using HVCC.Shell.Common.Interfaces;
using System.Collections;

namespace HVCC.Shell.Models
{
    //partial class Invoice
    //{
    //}

    partial class HVCCDataContext : IDataContext
    {
    }

    /// <summary>
    /// Extends definition of Invoice
    /// </summary>
 #region Invoice
    public partial class Invoice : ICloneable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        /// <summary>
        /// PaymentAmount is only used for referential calculations associated to an invoice
        /// </summary>
        private decimal _paymentAmount = 0;
        public decimal PaymentAmount
        {
            get
            {
                return _paymentAmount;
            }
            set
            {
                if (_paymentAmount != value)
                {
                    _paymentAmount = value;
                    RaisePropertyChanged("PaymentAmount");
                }
            }
        }

        /// <summary>
        /// Indicates if a payment has been applied to the invoice
        /// </summary>
        private bool _isPaymentApplied = false;
        public bool IsPaymentApplied
        {
            get
            {
                return _isPaymentApplied;
            }
            set
            {
                if (_isPaymentApplied != value)
                {
                    _isPaymentApplied = value;
                    RaisePropertyChanged("IsPaymentApplied");
                }
            }
        }

        /// <summary>
        /// I collection of billable items that comprise the invoice
        /// </summary>
        private ObservableCollection<InvoiceItem> _invoiceItems = null;
        public ObservableCollection<InvoiceItem> InvoiceItems
        {
            get
            {
                return _invoiceItems;
            }
            set
            {
                if (_invoiceItems != value)
                {
                    _invoiceItems = value;
                    RaisePropertyChanged("InvoiceItems");
                }
            }
        }

        public Season Season { get; set; }

        /// <summary>
        /// Method to provide a memberwise clone of the object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        /* --------------------------- INotify Property Change Implementation ----------------------------- */
        /// <summary>
        /// INotifyPropertyChanged Implementation
        /// </summary>
        #region INotifyPropertyChagned implementaiton
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

#endregion

    public partial class Payment : ICloneable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        private PaymentMessage _paymentMsg = new PaymentMessage() { Visibility = System.Windows.Visibility.Hidden, Header = string.Empty, Label = string.Empty, TextBlock = string.Empty };
        public PaymentMessage PaymentMsg
        {
            get
            {
                return _paymentMsg;
            }
            set
            {
                if (value != _paymentMsg)
                {
                    _paymentMsg = value;
                }
            }
        }

        /* --------------------------- INotify Property Change Implementation ----------------------------- */
        /// <summary>
        /// INotifyPropertyChanged Implementation
        /// </summary>
        #region INotifyPropertyChagned implementaiton
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
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }

    #region Extend Owner Model

    public partial class Owner : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private int _poolMembers = 0;
        public int PoolMembers
        {
            get
            {
                return this._poolMembers;
            }
            set
            {
                if (value != _poolMembers)
                {
                    _poolMembers = value;
                }
                RaisePropertyChanged("PoolMembers");
            }
        }

        private int _poolGuests = 0;
        public int PoolGuests
        {
            get
            {
                return this._poolGuests;
            }
            set
            {
                if (value != _poolGuests)
                {
                    _poolGuests = value;
                }
                RaisePropertyChanged("PoolGuests");
            }
        }

        private int _golfMembers = 0;
        public int GolfMembers
        {
            get
            {
                return this._golfMembers;
            }
            set
            {
                if (value != this._golfMembers)
                {
                    this._golfMembers = value;
                }
                RaisePropertyChanged("GolfMembers");
            }
        }

        private int _golfGuests = 0;
        public int GolfGuests
        {
            get
            {
                return this._golfGuests;
            }
            set
            {
                if (value != this._golfGuests)
                {
                    this._golfGuests = value;
                }
                RaisePropertyChanged("GolfGuests");
            }
        }


        /* --------------------------- INotify Property Change Implementation ----------------------------- */
        /// <summary>
        /// INotifyPropertyChanged Implementation
        /// </summary>
        #region INotifyPropertyChagned implementaiton
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
    #endregion

    #region Extend Property Model
    public partial class Property : ICloneable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        public WaterMeterReading LastMeterEntry
        {
            get
            {
                return this.WaterMeterReadings.Select(x => x).LastOrDefault();
            }
        }

        public ObservableCollection<WaterMeterReading> MeterReadings
        {
            get
            {
                List<WaterMeterReading> list = this.WaterMeterReadings.Select(x => x).ToList();
                ObservableCollection<WaterMeterReading> readings = new ObservableCollection<WaterMeterReading>();
                foreach (WaterMeterReading r in list)
                {
                    readings.Add(r);
                }
                return readings;
            }
        }



        /* --------------------------- INotify Property Change Implementation ----------------------------- */
        /// <summary>
        /// INotifyPropertyChanged Implementation
        /// </summary>
        #region INotifyPropertyChagned implementaiton
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

    public partial class Property : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public partial class Property : IDXDataErrorInfo
    {
        public void GetError(ErrorInfo info)
        {
            //throw new NotImplementedException();
        }

        public void GetPropertyError(string propertyName, ErrorInfo info)
        {
            //throw new NotImplementedException();
        }
    }

    #endregion

    #region Extend Relationship Model

    /// <summary>
    /// Extends the Relationship Class
    /// </summary>
    public partial class Relationship : ICloneable
    {
        public bool HasMutipleAssociations = false;

        private bool _isPool = false;
        public bool IsPool
        {
            get
            {
                return this._isPool;
            }
            set
            {
                if (value != _isPool)
                {
                    _isPool = value;

                    // Pool and Golf are mutually exclusive, so when one is true, the
                    // other must be false.  Additonally, when the value is true, the count
                    // is incremented, or when false the count is decremented.
                    //if (_isPool)
                    //{
                    //    this.IsGolf = false;
                    //}
                }
                RaisePropertyChanged("IsPool");
            }
        }

        private bool _isGolf = false;
        public bool IsGolf
        {
            get
            {
                return this._isGolf;
            }
            set
            {
                if (value != _isGolf)
                {
                    _isGolf = value;
                    //if (_isGolf)
                    //{
                    //    this.IsPool = false;
                    //}
                }
                RaisePropertyChanged("IsGolf");
            }
        }

        /* --------------------------- INotify Property Change Implementation ----------------------------- */
        /// <summary>
        /// INotifyPropertyChanged Implementation
        /// </summary>
        #region INotifyPropertyChagned implementaiton
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


    public partial class Relationship : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public partial class Relationship : IDXDataErrorInfo
    {
        public void GetError(ErrorInfo info)
        {
            //throw new NotImplementedException();
        }

        public void GetPropertyError(string propertyName, ErrorInfo info)
        {
            switch (propertyName)
            {
                case "FName":
                    if (String.IsNullOrEmpty(FName))
                    {
                        SetErrorInfo(info, "First Name field can't be empty", ErrorType.Critical);
                    }
                    break;
                case "LName":
                    if (String.IsNullOrEmpty(LName))
                    {
                        SetErrorInfo(info, "Last Name field can't be empty", ErrorType.Critical);
                    }
                    break;

                case "RelationToOwner":
                    if (String.IsNullOrEmpty(RelationToOwner))
                    {
                        SetErrorInfo(info, "Field can't be empty, please make a selection", ErrorType.Critical);
                    }
                    break;

            }
        }

        void SetErrorInfo(ErrorInfo info, string errorText, ErrorType errorType)
        {
            info.ErrorText = errorText;
            info.ErrorType = errorType;
        }
    }
    #endregion

    #region Extend WellMeterReading Model

    /// <summary>
    /// Set up validated columns
    /// </summary>
    public partial class WellMeterReading : INotifyPropertyChanging, INotifyPropertyChanged, IDataErrorInfo
    {
        string IDataErrorInfo.Error
        {
            get
            {
                //if (string.IsNullOrEmpty(MeterReading.ToString()))
                //    return "invalid";
                //return null;

                if (string.IsNullOrEmpty(ThroughputInGallons.ToString()))
                    return "invalid";

                return null;
            }
        }

        /// <summary>
        /// Registers the grid columns that require data validation
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                //if (columnName == "MeterReading" && string.IsNullOrEmpty(MeterReading.ToString()))
                //    return "Invalid Value";
                //if (columnName == "MeterReading" && 0 > MeterReading)
                //    return "Invalid Value";

                if (columnName == "ThroughputInGallons" && string.IsNullOrEmpty(ThroughputInGallons.ToString()))
                    return "Invalid Value";
                if (columnName == "ThroughputInGallons" && 0 > ThroughputInGallons)
                    return "Invalid Value";

                return null;
            }
        }
    }

    #endregion

    #region ********************* Class: Unit {Used By WellWater<View/ViewModel> ********************************
    /// <summary>
    /// Class to describe a month as descriptive text and integer index
    /// </summary>
    public class Unit : INotifyPropertyChanging, INotifyPropertyChanged
    {

        private string _description;
        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                if ((this._description != value))

                {
                    this.SendPropertyChanging();
                    this._description = value;
                    this.SendPropertyChanged("Description");
                }
            }
        }

        public double Value { get; set; }

        private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises a Property Changing event
        /// </summary>
        protected virtual void SendPropertyChanging()
        {
            if ((this.PropertyChanging != null))
            {
                this.PropertyChanging(this, emptyChangingEventArgs);
            }
        }

        /// <summary>
        /// Raises a Property Changed event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void SendPropertyChanged(String propertyName)
        {
            if ((this.PropertyChanged != null))
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
    #endregion

}