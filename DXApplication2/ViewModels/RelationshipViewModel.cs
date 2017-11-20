namespace HVCC.Shell.ViewModels
{
    using HVCC.Shell.Models;
    using System;
    using System.ComponentModel;
    using System.Windows;
    public partial class RelationshipViewModel
    {
        /// <summary>
        /// Relationship object to act on edit/add
        /// </summary>
        public virtual Relationship Relationship { get; set; }

        /// <summary>
        /// Information about the lot associated to the relationship
        /// </summary>
        //
        //  I had to add the LotInformation string to this VM because of the way the DialogService
        //  interacts with this VM.  In short, the VM doesn't allow for accessing model relationships.
        //  Therefore, from the Relationship, the Property object related is not in scope.
        private string _lotInformation = string.Empty;
        public string LotInformation
        {
            get
            {
                return this._lotInformation;
            }
            set
            {
                if (value != this._lotInformation)
                {
                    this._lotInformation = value;
                }
            }
        }

        /// <summary>
        /// Determines if the current user has the rights to enable/execute this feature
        /// </summary>
        private bool _canAddRelationship = false; // Default: false
        public bool CanAddRelationship
        {
            get { return this._canAddRelationship; }
            set
            {
                if (value != this._canAddRelationship)
                {
                    this._canAddRelationship = value;
                    RaisePropertyChanged("CanAddRelationship");
                }
            }
        }

        /// <summary>
        /// Get the dirty state of the ViewModel's model
        /// </summary>
        public bool IsDirty
        {
            get
            {
                var changeSet = this.dc.GetChangeSet();
                if (0 == changeSet.Inserts.Count + changeSet.Updates.Count + changeSet.Deletes.Count)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Indicates if form data is Valid
        /// </summary>
        private bool _isValid = false;
        public bool IsValid
        {
            get { return this._isValid; }
            set
            {
                if (value != this._isValid)
                {
                    this._isValid = value;
                    RaisePropertyChanged("IsRelationshipValid");
                }
            }
        }

        /* -------------
        /// <summary>
        /// Get TextEdit control adornments
        /// </summary>
        private Style _teStyle = (Style)App.Current.MainWindow.Resources["TextEditDisplayStyle"];
        public Style TeStyle
        {
            get
            {
                if (this.IsEditable)
                {
                    Style st = (Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                    return (Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                }
                else
                {
                    return (Style)App.Current.MainWindow.Resources["TextEditDisplayStyle"];
                }
            }
        }

        /// <summary>
        /// Get ComboBoxEdit control adornments
        /// </summary>
        private Style _cbStyle = (Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
        public Style CbStyle
        {
            get
            {
                if (this.IsEditable)
                {
                    Style st = (Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                    return (Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                }
                else
                {
                    return (Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
                }
            }
        }
        ---------------------- */

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
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class RelationshipViewModel : IDisposable
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
        /// Finalizes an instance of the class.  (a.k.a. destructor)
        /// </summary>
        ~RelationshipViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(true);  // originally set to 'false'
        }
    }
    #endregion
}
