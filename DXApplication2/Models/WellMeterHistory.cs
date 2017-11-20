namespace HVCC.Shell.Models
{
    using System;
    using System.ComponentModel;

    public class WellMeterHistory : INotifyPropertyChanging, INotifyPropertyChanged
    {
        DateTime _meterReadingDate;
        public DateTime MeterReadingDate
        {
            get
            {
                return _meterReadingDate;
            }
            set
            {
                if ((this._meterReadingDate != value))

                {
                    this.SendPropertyChanging();
                    this._meterReadingDate = value;
                    this.SendPropertyChanged("MeterReadingDate");
                }
            }
        }

        private string _month;
        public string Month
        {
            get
            {
                return _month;
            }
            set
            {
                if ((this._month != value))
                {
                    this.SendPropertyChanging();
                    this._month = value;
                    this.SendPropertyChanged("Month");
                }
            }
        }

        private string _year;
        public string Year
        {
            get
            {
                return _year;
            }
            set
            {
                if ((this._year != value))
                {
                    this.SendPropertyChanging();
                    this._year = value;
                    this.SendPropertyChanged("Year");
                }
            }
        }

        private long? _well3;
        public long? Well3
        {
            get
            {
                return this._well3;
            }
            set
            {
                if ((this._well3 != value))

                {
                    this.SendPropertyChanging();
                    this._well3 = value;
                    this.SendPropertyChanged("Well3");
                }
            }
        }

        private long? _well5;
        public long? Well5
        {
            get
            {
                return this._well5;
            }
            set
            {
                if ((this._well5 != value))

                {
                    this.SendPropertyChanging();
                    this._well5 = value;
                    this.SendPropertyChanged("Well5");
                }
            }
        }

        private long? _well7;
        public long? Well7
        {
            get
            {
                return this._well7;
            }
            set
            {
                if ((this._well7 != value))

                {
                    this.SendPropertyChanging();
                    this._well7 = value;
                    this.SendPropertyChanged("Well7");
                }
            }
        }

        private long? _well8;
        public long? Well8
        {
            get
            {
                return this._well8;
            }
            set
            {
                if ((this._well8 != value))

                {
                    this.SendPropertyChanging();
                    this._well8 = value;
                    this.SendPropertyChanged("Well8");
                }
            }
        }

        private long? _well10;
        public long? Well10
        {
            get
            {
                return this._well10;
            }
            set
            {
                if ((this._well10 != value))

                {
                    this.SendPropertyChanging();
                    this._well10 = value;
                    this.SendPropertyChanged("Well10");
                }
            }
        }

        private long? _total;
        public long? Total
        {
            get
            {
                return this._total;
            }
            set
            {
                if ((this._total != value))

                {
                    this.SendPropertyChanging();
                    this._total = value;
                    this.SendPropertyChanged("Total");
                }
            }
        }

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
}
