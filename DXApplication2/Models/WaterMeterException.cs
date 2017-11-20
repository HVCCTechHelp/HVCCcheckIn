namespace HVCC.Shell.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class WaterMeterException : INotifyPropertyChanging, INotifyPropertyChanged
    {

        /// <summary>
        /// 
        /// </summary>
        private string _customer;
        public string Customer
        {
            get
            {
                return _customer;
            }
            set
            {
                if ((_customer != value))

                {
                    _customer = value;
                    //SendPropertyChanged("Customer");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private DateTime? _currentMeterReadingDate;
        public DateTime? CurrentMeterReadingDate
        {
            get
            {
                return _currentMeterReadingDate;
            }
            set
            {
                if ((_currentMeterReadingDate != value))

                {
                    _currentMeterReadingDate = value;
                    //SendPropertyChanged("CurrentMeterReadingDate");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private DateTime? _lastMeterReadingDate;
        public DateTime? LastMeterReadingDate
        {
            get
            {
                return _lastMeterReadingDate;
            }
            set
            {
                if ((_lastMeterReadingDate != value))

                {
                    _lastMeterReadingDate = value;
                    //SendPropertyChanged("LastMeterReadingDate");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private int? _currentMeterReading;
        public int? CurrentMeterReading
        {
            get
            {
                return _currentMeterReading;
            }
            set
            {
                if ((_currentMeterReading != value))

                {
                    _currentMeterReading = value;
                    //SendPropertyChanged("CurrentMeterReading");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private int? _lastMeterReading;
        public int? LastMeterReading
        {
            get
            {
                return _lastMeterReading;
            }
            set
            {
                if ((_lastMeterReading != value))

                {
                    _lastMeterReading = value;
                    //SendPropertyChanged("LastMeterReading");
                }
            }
        }

        #region INotifyProperty
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
        #endregion
    }
}
