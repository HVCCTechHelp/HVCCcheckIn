namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using DevExpress.Xpf.Docking;
    using System.Data.Linq;
    using HVCC.Shell.Common;
    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;
    using DevExpress.Mvvm.POCO;
    using HVCC.Shell.Models;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Helpers;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Spreadsheet;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using DevExpress.Xpf.Core;
    using System.Diagnostics;
    using System.Windows.Navigation;
    using HVCC.Shell.Resources;
    using System.Runtime.InteropServices;


    // This ViewModel serves as a bridge for the primary CanChec VM.  It is required by the
    // IDialogService. Therefore we create virtual properties for the SelecrtedProperty and
    // Relationships collection so we can have a reference to the data elements we need.
    [POCOViewModel]
    public partial class PropertyViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public PropertyViewModel()
        {
        }


        /// <summary>
        /// Register for Property Changes to the ViewModel's SelectedProperty entity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedRelation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // We listen for changes to the IsGolf/IsPool value of the SelectedProperty so we can update the 
            // Check-In counts for the property.

            if (e.PropertyName == "IsGolf" || e.PropertyName == "IsPool")
            {
                // Initialize the Check-In property counters to zero. Then iterate over the collection to
                // get current counts.
                this.SelectedProperty.GolfMembers = 0;
                this.SelectedProperty.PoolMembers = 0;

                foreach (Relationship r in this.SelectedProperty.Relationships)
                {
                    if (r.IsGolf) { this.SelectedProperty.GolfMembers += 1; }
                    if (r.IsPool) { this.SelectedProperty.PoolMembers += 1; }
                }
            }
        }

        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }

        /* ------------------------------- ViewModel Properties ---------------------------------------- */
        #region ViewModel Properties
        public virtual Property SelectedProperty
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual ApplicationPermission ApplPermissions
        {
            get;
            set;
        }

        public virtual ApplicationDefault ApplDefault
        {
            get;
            set;
        }

        //public virtual ObservableCollection<Relationship> ActiveRelationshipsToProperty
        //{
        //    get;
        //    set;
        //}

        /// <summary>
        /// Currently selected relationship
        /// </summary>
        private Relationship _selectedRelation = new Relationship();
        public Relationship SelectedRelation
        {
            get
            {
                return this._selectedRelation;
            }
            set
            {
                if (value != this._selectedRelation)
                {
                    // When the selected relationship is change; a new selection is made, we unregister the previous PropertyChanged
                    // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                    if (this._selectedRelation != null)
                    {
                        this._selectedRelation.PropertyChanged -= SelectedRelation_PropertyChanged;
                    }

                    this._selectedRelation = value;
                    if (null != this._selectedRelation)
                    {
                        // Once the new value is assigned, we register a new PropertyChanged event listner.
                        this._selectedRelation.PropertyChanged += SelectedRelation_PropertyChanged;
                        //// The database stores the raw binary data of the image.  Before it can be
                        //// displayed in the ImageEdit control, it must be encoded into a BitmapImage

                        if (null == this._selectedRelation.Photo)
                        {
                            this._selectedRelation.Photo = this.ApplDefault.Photo;
                        }
                        this.Image = Helper.ArrayToBitmapImage(this._selectedRelation.Photo.ToArray());
                        RaisePropertyChanged("SelectedRelation");
                    }
                }
            }
        }

        /// <summary>
        /// A collection of relationships to display in the Relationships grid of the view
        /// </summary>
        private ObservableCollection<Relationship> _activeRelationshipsToProperty = null;
        public ObservableCollection<Relationship> ActiveRelationshipsToProperty
        {
            get
            {
                if (this._activeRelationshipsToProperty == null)
                {
                    this._activeRelationshipsToProperty = new ObservableCollection<Relationship>();
                    foreach (Relationship r in this.SelectedProperty.Relationships)
                    {
                        // We only want to display active relationships for the property
                        if (true == r.Active)
                            _activeRelationshipsToProperty.Add(r);
                    }
                }
                // Before returning the collection, set the SelectedRelation to the first relationship
                // in the collection so the image edit control will have something displayed.
                //this.SelectedRelation = this._relationshipsToProperty[0];
                return this._activeRelationshipsToProperty;
            }
            set
            {
                if (this._activeRelationshipsToProperty != value)
                {
                    this._activeRelationshipsToProperty = value;
                }
                RaisePropertyChanged("RelationshipsToProperty");
            }
        }

        /// <summary>
        /// A collection of relationships to act upon, generlly related to additions & deletetions
        /// </summary>
        private ObservableCollection<Relationship> _relationshipsToRemove = null;
        public ObservableCollection<Relationship> RelationshipsToRemove
        {
            get
            {
                if (this._relationshipsToRemove == null)
                {
                    this._relationshipsToRemove = new ObservableCollection<Relationship>();
                }
                return this._relationshipsToRemove;
            }
            set
            {
                if (this._relationshipsToRemove != value)
                {
                    this._relationshipsToRemove = value;
                }
                RaisePropertyChanged("RelationshipsToRemove");
            }
        }

        /// <summary>
        /// Returns boolean value to indicate if the selected property has a registered golf cart
        /// </summary>
        public bool HasRegisteredCart
        {
            get
            {
                var xxx = (from a in this.dc.GolfCarts
                           where a.PropertyID == this.SelectedProperty.PropertyID
                           select a).FirstOrDefault();
                if (null == xxx) { return false; }
                else { return true; }
            }
        }

        private BitmapImage _image = null;
        public BitmapImage Image
        {
            get
            {
                // Check to make sure the Photo field is not null before trying to convert
                // it to a BitMap, otherwise a null reference expection will be thrown.
                if (null != this._selectedRelation.Photo)
                {
                    return Helper.ArrayToBitmapImage(this.SelectedRelation.Photo.ToArray());
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (this._image != value)
                {
                    this._image = value;
                }
                RaisePropertyChanged("Image");
            }
        }

        public string HeaderText
        {
            get
            {
                return string.Format("Property Details: {0}", this.SelectedProperty.Customer);
            }
        }

        /// <summary>
        /// Count of note records associated with the selected property
        /// </summary>
        private int _noteCount = 0;
        public int NoteCount
        {
            get
            {
                return this._noteCount;
            }
            set
            {
                if (this._noteCount != value)
                {
                    this._noteCount = value;
                }
            }
        }
        #endregion

        /* ------------------------------ Boolean Properties ------------------------------------------- */
        #region Boolean Properties
        /// 
        /// </summary>
        private bool _canViewParcel = false;
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

        /// <summary>
        /// 
        /// </summary>
        private bool _canViewMap = false;
        public bool CanViewMap
        {
            get { return this._canViewMap; }
            set
            {
                if (value != this._canViewMap)
                {
                    this._canViewMap = value;
                    RaisePropertyChanged("CanViewMap");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private bool _isCheckinEnabled = false;
        public bool IsCheckinEnabled
        {
            get
            {
                return _isCheckinEnabled;
            }
            set
            {
                if (value != _isCheckinEnabled)
                {
                    this._isCheckinEnabled = value;
                    RaisePropertyChanged("IsCheckinEnabled");
                }
            }
        }

        #endregion

        /* ----------------------------------- Style Properteis ---------------------------------------- */
        #region Style Properties

        private GridViewNavigationStyle _navigationStyle = GridViewNavigationStyle.Cell;
        public GridViewNavigationStyle NavigationStyle
        {
            get
            {
                return _navigationStyle;
            }
            set
            {
                if (value != _navigationStyle)
                {
                    this._navigationStyle = value;
                    RaisePropertyChanged("NavigationStyle");
                }
            }
        }

        private NewItemRowPosition _newItemPosition = NewItemRowPosition.None;
        public NewItemRowPosition NewItemPosition
        {
            get
            {
                return _newItemPosition;
            }
            set
            {
                if (value != _newItemPosition)
                {
                    this._newItemPosition = value;
                    RaisePropertyChanged("NewItemPosition");
                }
            }
        }

        /// <summary>
        /// Get TextBox control adornments
        /// </summary>
        private System.Windows.Style _tbStyle = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
        public System.Windows.Style TbStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditPropertyInfo)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
                }
            }
        }

        /// <summary>
        /// Get TextEdit control adornments
        /// </summary>
        private System.Windows.Style _teStyle = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
        public System.Windows.Style TeStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditPropertyInfo || this.ApplPermissions.CanEditPropertyNotes)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextEditDisplayStyle"];
                }
            }
        }

        /// <summary>
        /// Get ComboBoxEdit control adornments
        /// </summary>
        private System.Windows.Style _cbStyle = (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
        public System.Windows.Style CbStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditPropertyInfo)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
                }
            }
        }
        #endregion

        /* ---------------------------------- Commands & Actions --------------------------------------- */
        #region Commands

        /// <summary>
        /// Check In Command
        /// </summary>
        private ICommand _checkInCommand;
        public ICommand CheckInCommand
        {
            get
            {
                return _checkInCommand ?? (_checkInCommand = new CommandHandler(() => CheckInAction(), this.ApplPermissions.CanCheckIn));
            }
        }

        // This section of code sets up a 'key-pressed' event to trigger the MVVM behavior to trigger the 
        // CloseByEscapeBehavior. This is used by CheckInAction() to close the Edit Dialog window
        // after the user checks in a member/guest.
        private const int WM_KEYDOWN = 0x0100;
        const int VK_F22 = 0x85;  // Virtual Key F-22
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        public void CheckInAction()
        {
            try
            {
                MessageResult results = MessageResult.Cancel;

                if (!this.SelectedProperty.IsInGoodStanding)
                {
                    results = MessageBoxService.ShowMessage("This member is not is good standing.\nAsk them to make a payment before allowing them to check in.\n Click OK to continue Checking In, or Cancel to not Check In"
                        , "Warning"
                        , MessageButton.OKCancel
                        , MessageIcon.Exclamation
                        );
                }
                else
                {
                    results = MessageBoxService.ShowMessage("Proceed with Check In?"
                        , "Proceed"
                        , MessageButton.OKCancel
                        , MessageIcon.Question
                        );
                    //results = MessageResult.OK;
                }

                // If the property is in good standing, or staff allows member to check in, then proceed
                if (MessageResult.OK == results)
                {
                    string activity = string.Empty;

                    using (HVCCDataContext dc = new HVCCDataContext())
                    {
                        List<FacilityUsage> usages = new List<FacilityUsage>();

                        // Register the members what are checking in.
                        foreach (Relationship r in this.SelectedProperty.Relationships)
                        {
                            if (r.IsGolf || r.IsPool)
                            {
                                FacilityUsage usage = new FacilityUsage();

                                usage.PropertyID = SelectedProperty.PropertyID;
                                usage.RelationshipId = r.RelationshipID;
                                usage.Date = DateTime.Now;
                                if (r.IsGolf)
                                {
                                    usage.GolfRoundsMember = 1;
                                    activity = "Golf";
                                }
                                else
                                {
                                    usage.GolfRoundsMember = 0;
                                }
                                if (r.IsPool)
                                {
                                    usage.PoolMember = 1;
                                    activity = "the Pool";
                                }
                                else
                                {
                                    usage.PoolMember = 0;
                                }
                                usage.GolfRoundsGuest = 0;
                                usage.PoolGuest = 0;

                                // Before committing the data, we check to make sure the member(s)
                                // have not already been checked in for the day.
                                // 
                                DateTime dt1 = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0);
                                DateTime dt2 = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 23, 59, 59);

                                var z = (from p in dc.FacilityUsages
                                         where p.RelationshipId == r.RelationshipID
                                         && p.GolfRoundsMember == usage.GolfRoundsMember
                                         && p.PoolMember == usage.PoolMember
                                         && (p.Date >= dt1 && p.Date <= dt2)
                                         select p).FirstOrDefault();

                                if (null == z)
                                {
                                    dc.FacilityUsages.InsertOnSubmit(usage);
                                }
                                else
                                {
                                    string msg = String.Format("Member {0} {1} has already checked in for {2} today.", r.FName, r.LName, activity);
                                    MessageBox.Show(msg, "Warning", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                                }

                                // Flip the Golf & Pool bits so they aren't left in a true state
                                r.IsPool = false;
                                r.IsGolf = false;
                            }
                        }

                        // If there are guests of the members, add them last.
                        if (0 < this.SelectedProperty.PoolGuests || 0 < this.SelectedProperty.GolfGuests)
                        {
                            Relationship guest = (from r in this.dc.Relationships
                                                  where r.RelationToOwner == "Guest"
                                                  select r).FirstOrDefault();

                            FacilityUsage gUsage = new FacilityUsage();
                            gUsage.PropertyID = SelectedProperty.PropertyID;
                            gUsage.RelationshipId = guest.RelationshipID;
                            gUsage.Date = DateTime.Now;
                            gUsage.GolfRoundsMember = 0;
                            gUsage.PoolMember = 0;
                            gUsage.GolfRoundsGuest = SelectedProperty.GolfGuests;
                            gUsage.PoolGuest = SelectedProperty.PoolGuests;

                            dc.FacilityUsages.InsertOnSubmit(gUsage);
                        }

                        ChangeSet cs = dc.GetChangeSet();
                        dc.SubmitChanges();
                        MessageBoxService.ShowMessage("Check In Complete");
                    }
                }
                else
                {
                    MessageBoxService.ShowMessage("Check In was canceled. No data was recorded.");
                    foreach (Relationship r in this.SelectedProperty.Relationships)
                    {
                        r.IsPool = false;
                        r.IsGolf = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Check in Error: " + ex.Message);
            }
            finally
            {
                IsCheckinEnabled = false;
                this.SelectedProperty.PoolMembers = 0;
                this.SelectedProperty.PoolGuests = 0;
                this.SelectedProperty.GolfMembers = 0;
                this.SelectedProperty.GolfGuests = 0;

                // Find the process for the model dialog box by the assembely name.  Once we have
                // the process we can raise a message event to trigger the MVVM interaction behavior
                // to close the dialog window.
                Process[] processes = Process.GetProcessesByName("HVCC Membership");
                foreach (Process proc in processes)
                {
                    bool x = PostMessage(proc.MainWindowHandle, WM_KEYDOWN, VK_F22, 0);
                }
            }
        }

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

        /// <summary>
        /// View Parcel Command
        /// </summary>
        private ICommand _viewMapCommand;
        public ICommand ViewMapCommand
        {
            get
            {
                return _viewMapCommand ?? (_viewMapCommand = new CommandHandler(() => ViewMapAction(), _canViewMap));
            }
        }

        public void ViewMapAction()
        {
            try
            {
                StringBuilder absoluteUri = new StringBuilder();
                absoluteUri.Append("https://www.google.com/maps/place/");
                absoluteUri.Append(this.SelectedProperty.PhysicalAddress.Replace(" ", "+"));
                absoluteUri.Append(" Packwood, WA 98361");
                Process.Start(new ProcessStartInfo(absoluteUri.ToString()));
            }
            catch (Exception ex)
            {
                MessageBoxService.Show(ex.Message);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Delete Relationship Command
        /// </summary>
        private ICommand _deleteRelationshipCommand;
        public ICommand DeleteRelationshipCommand
        {
            get
            {
                return _deleteRelationshipCommand ?? (_deleteRelationshipCommand = new CommandHandler(() => DeleteRelationshipAction(), this.ApplPermissions.CanDeleteRelationship));
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public void DeleteRelationshipAction()
        {
            if ("Owner" != this.SelectedRelation.RelationToOwner)
            {
                this.RelationshipsToRemove.Add(SelectedRelation);
                this.ActiveRelationshipsToProperty.Remove(SelectedRelation);
            }
            else
            {
                MessageBox.Show("Owner records may not be removed here.");
            }
        }
        /// <summary>
        /// Add Relationship Command
        /// </summary>
        private ICommand _addRelationshipCommand;
        public ICommand AddRelationshipCommand
        {
            get
            {
                return _addRelationshipCommand ?? (_addRelationshipCommand = new CommandHandler(() => AddRelationshipAction(), this.ApplPermissions.CanAddRelationship));
            }

        }

        public void AddRelationshipAction()
        {
            this.NavigationStyle = GridViewNavigationStyle.Row;
            this.NewItemPosition = NewItemRowPosition.Top;
        }

        #endregion

        /* ----------------------------------- Public Methods ------------------------------------------ */
        #region Public Methods
        /// <summary>
        /// Assigns the default values to a newly created relationship
        /// </summary>
        public void AssignDefaultValues(Relationship r)
        {
            // Test to see if the name being added to this property already exists, but
            // is inactive. We also use a found Relatinship to see if it has been updated.
            Relationship chkR = (from x in this.SelectedProperty.Relationships
                                 where (x.FName == r.FName
                                 && x.LName == r.LName)
                                 select x).FirstOrDefault();
            // If no matching record exists, then we add it to the collection.
            if (null == chkR)
            {
                r.Active = true;
                this.SelectedProperty.Relationships.Add(r);
            }
            // Otherwise, we present the user with a message.
            else
            {
                string message = String.Format("It appears the name {0} {1} is already defined for this property.\nDo you want to activate the existing relationship record?", r.FName, r.LName);
                MessageBoxResult result =
                    MessageBox.Show(message, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);

                // If they answer "Yes", all we have to do is update/set the Active flag to true.
                if (MessageBoxResult.Yes == result)
                {
                    r.RelationshipID = chkR.RelationshipID;
                    chkR.Active = true;
                }
            }

            int ndx = this.ActiveRelationshipsToProperty.Count() - 1;
            this.ActiveRelationshipsToProperty[ndx].Active = true;
            this.ActiveRelationshipsToProperty[ndx].Photo = this.ApplDefault.Photo;
            this.ActiveRelationshipsToProperty[ndx].Image = Helper.ArrayToBitmapImage(this.ApplDefault.Photo.ToArray());
        }

        #endregion

        /* ----------------------------------- Private Methods -------------------------------------------- */
        #region Private Methods
        #endregion

        /* ------------------------------- INotify Implementation --------------------------------------------- */
        #region INotifyPropertyChagned implementaiton
        /// <summary>
        /// 
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
    #region public partial class PropertyViewModel : IDisposable
    public partial class PropertyViewModel : IDisposable
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
        ~PropertyViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
