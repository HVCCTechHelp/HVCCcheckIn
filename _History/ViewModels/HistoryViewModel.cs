namespace HVCC.History.ViewModels
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DevExpress.Xpf.Docking;

    public partial class HistoryViewModel : INotifyPropertyChanged
    {

        /// <summary>
        /// 
        /// </summary>
        private BaseLayoutItem activeDocPanel = null;
        public BaseLayoutItem ActiveDocPanel
        {
            get
            {
                return this.activeDocPanel;
            }
            set
            {
                if (this.activeDocPanel != value)
                {
                    this.activeDocPanel = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private ObservableCollection<v_PropertyHistory> properties = null;
        public ObservableCollection<v_PropertyHistory> Properties
        {
            get
            {
                this.properties = GetPropertiesHistory();
                return this.properties;
            }
            set
            {
                if (this.properties != value)
                {
                    this.properties = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<v_PropertyHistory> GetPropertiesHistory()
        {
            try
            {
                var list = (from a in this.dc.v_PropertyHistories
                            orderby a.Year
                            select a);

                return new ObservableCollection<v_PropertyHistory>(list);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private v_PropertyHistory selectedProperty = new v_PropertyHistory();
        public v_PropertyHistory SelectedProperty
        {
            get
            {
                return this.selectedProperty;
            }
            set
            {
                if (value != this.selectedProperty)
                {
                    this.selectedProperty = value;
                }
                RaisePropertyChanged("SelectedProperty");
            }
        }

        /// <summary>,
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


    #region public partial class HistoryViewModel : IDisposable
    public partial class HistoryViewModel : IDisposable
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
        ~HistoryViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}
