namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common;
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
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    public partial class OwnerEditViewModel : CommonViewModel, ICommandSink
    {
        public OwnerEditViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;

            // This V/VM can be invoked by several different Views. Therefore, we have conditions for
            // each invocation possibility.  In all cases, the OwnerID value will be set to find the selected
            // owner record to edit.  Secondarliy, the RelationshipID may be set to set the SelectedRelationship.
            int ownerID = 0;
            int relationshipID = 0;
            if (parameter is v_OwnerDetail)
            {
                v_OwnerDetail p = parameter as v_OwnerDetail;
                ownerID = (int)p.OwnerID;
            }
            else if (parameter is Owner)
            {
                Owner p = parameter as Owner;
                ownerID = p.OwnerID;
            }
            else if (parameter is v_ActiveRelationship)
            {
                v_ActiveRelationship p = parameter as v_ActiveRelationship;
                ownerID = p.OwnerID;
                relationshipID = p.RelationshipID;
            }
            else if (parameter is v_PropertyDetail)
            {
                v_PropertyDetail p = parameter as v_PropertyDetail;
                ownerID = p.OwnerID;
            }
            else
            {
                MessageBox.Show("No Owner information passed in to edit form");
            }

            try
            {
                /// Fetch the Owner record and its Relationships from the database so the 
                /// datacontext will be scoped to this ViewModel.
                /// 
                SelectedOwner = (from x in this.dc.Owners
                                 where x.OwnerID == ownerID
                                 select x).FirstOrDefault();

                /// Set the focused row in the Properties grid to the first item.
                /// 
                SelectedProperty = Properties[0];

                // Fetch the collection of avtive Relationships for this owner
                var rList = (from x in this.dc.Relationships
                             where x.OwnerID == SelectedOwner.OwnerID
                             && x.Active == true
                             select x);

                Relationships = new ObservableCollection<Relationship>(rList);
                CkFacilityUsage();

                /// Set the focus to either the first relationship in the collection, or to the
                /// RelationshipID if one was passed in.
                /// 
                if (0 == relationshipID)
                {
                    /// Set the focused row in the Relationships grid to the first item in the Owner's
                    /// Relationship collection.
                    /// 
                    SelectedRelationship = Relationships[0];
                }
                else
                {
                    Relationship r = (from x in Relationships
                                      where x.RelationshipID == relationshipID
                                      select x).FirstOrDefault();
                    SelectedRelationship = r;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Owner Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            /// Register an event handler for SelectedOwner property changed.
            /// 
            SelectedOwner.PropertyChanged +=
                 new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);

            IsBusy = false;
        }

        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

        public override bool IsValid => throw new NotImplementedException();

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
                Caption = caption[0].TrimEnd(' ') + "*";
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

        /// <summary>
        /// Should the Check-In button be enabled/disabled
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

        public string HeaderText
        {
            get
            {
                return string.Format("Edit Owner #{0:000000}", SelectedOwner.OwnerID);
            }
        }

        private Owner _selectedOwner = null;
        public Owner SelectedOwner
        {
            get
            {
                return _selectedOwner;
            }
            set
            {
                // wrap the setting in a null check.  When the master row is expanded, and a detail row
                // is selected, it causes a propertychanged event and sets the value to null, which we want to ignore.
                if (null != value && _selectedOwner != value)
                {
                    _selectedOwner = value;
                    AllNotes = GetOwnerNotes();
                    RaisePropertyChanged("SelectedOwner");
                }
            }
        }

        private Property _selectedProperty = null;
        public Property SelectedProperty
        {
            get
            {
                return _selectedProperty;
            }
            set
            {
                if (value != _selectedProperty)
                {
                    _selectedProperty = value;
                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

        /// <summary>
        /// A collection of Properties 
        /// </summary>
        public ObservableCollection<Property> Properties
        {
            get
            {
                var list = (from x in SelectedOwner.Properties
                            select x);
                return new ObservableCollection<Property>(list);
            }
        }

        /// <summary>
        /// A collection of financial transactions 
        /// </summary>
        //public ObservableCollection<FinancialTransaction> FinancialTransactions
        //{
        //    get
        //    {
        //        var list = (from x in dc.FinancialTransactions
        //                    where x.OwnerID == SelectedOwner.OwnerID
        //                    select x);

        //        ObservableCollection<FinancialTransaction> ftCollection = new ObservableCollection<FinancialTransaction>(list);
        //        AccountBalance = ftCollection[ftCollection.Count() - 1].Balance;
        //        return ftCollection;
        //    }
        //}
        public ObservableCollection<v_OwnerTransaction> FinancialTransactions
        {
            get
            {
                var list = (from x in dc.v_OwnerTransactions
                            where x.OwnerID == SelectedOwner.OwnerID
                            select x);

                return new ObservableCollection<v_OwnerTransaction>(list);
            }
        }

        //private FinancialTransaction _selectedTransaction = null;
        //public FinancialTransaction SelectedTransaction
        //{
        //    get
        //    {
        //        return _selectedTransaction;
        //    }
        //    set
        //    {
        //        if (_selectedTransaction != value)
        //        {
        //            _selectedTransaction = value;
        //            RaisePropertyChanged("SelectedTransaction");
        //        }
        //    }
        //}

        private v_OwnerTransaction _selectedTransaction = null;
        public v_OwnerTransaction SelectedTransaction
        {
            get
            {
                return _selectedTransaction;
            }
            set
            {
                if (_selectedTransaction != value)
                {
                    _selectedTransaction = value;
                    RaisePropertyChanged("SelectedTransaction");
                }
            }
        }

        private Relationship _selectedRelationship = null;
        public Relationship SelectedRelationship
        {
            get
            {
                return this._selectedRelationship;
            }
            set
            {
                // When the selected relationship is change; a new selection is made, we unregister the previous PropertyChanged
                // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                // We use this PropertyChanged trigger to handle Image changes, and to manage the Golf/Pool check ins.
                if (this._selectedRelationship != null)
                {
                    this._selectedRelationship.PropertyChanged -= SelectedRelation_PropertyChanged;
                }

                if (value != this._selectedRelationship)
                {
                    this._selectedRelationship = value;
                    // Once the new value is assigned, we register a new PropertyChanged event listner.
                    this._selectedRelationship.PropertyChanged += SelectedRelation_PropertyChanged;

                    //// The database stores the raw binary data of the image.  Before it can be
                    //// displayed in the ImageEdit control, it must be encoded into a BitmapImage
                    if (null == this._selectedRelationship.Photo && null != ApplDefault)
                    {
                        this._selectedRelationship.Photo = this.ApplDefault.Photo;
                    }
                    RaisePropertyChanged("SelectedRelationship");
                }
            }
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<Relationship> _relationships = null;
        public ObservableCollection<Relationship> Relationships
        {
            get
            {
                return this._relationships;
            }
            set
            {
                if (_relationships != value)
                {
                    if (null != _relationships)
                    {
                        _relationships.CollectionChanged -= _relationships_CollectionChanged;
                    }
                    _relationships = value;
                    _relationships.CollectionChanged += _relationships_CollectionChanged;
                }
            }
        }

        public string NotesHeader
        {
            get
            {
                return string.Format("HVCC Notes [{0}]", NoteCount);
            }
        }

        /// <summary>
        /// An integer representing the count of notes associated to the selected property
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

        private string _newNote = null;
        public string NewNote
        {
            get
            {
                return _newNote;
            }
            set
            {
                if (_newNote != value)
                {
                    _newNote = value;
                    RaisePropertyChanged("NewNote");
                }
            }
        }

        private string _allNotes = string.Empty;
        public string AllNotes
        {
            get
            {
                return this._allNotes;
            }
            set
            {
                if (value != this._allNotes)
                {
                    this._allNotes = value;
                }
                RaisePropertyChanged("AllNotes");
            }
        }

        private decimal? _accountBalance = null;
        public decimal? AccountBalance
        {
            get
            {
                if (null == _accountBalance)
                {
                    try
                    {
                        var b = SelectedOwner.FinancialTransactions.Select(x => x).LastOrDefault();

                        if (b.Balance > 0)
                        {
                            TextColor = new SolidColorBrush(Colors.DarkRed);
                        }
                        _accountBalance = b.Balance;
                        RaisePropertyChanged("AccountBalance");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        _accountBalance = null;
                    }
                }
                return _accountBalance;
            }
            set
            {
                if (value != _accountBalance)
                {
                    _accountBalance = value;
                    RaisePropertyChanged("AccountBalance");
                }
            }
        }

        private SolidColorBrush _textColor = new SolidColorBrush(Colors.Black);
        public SolidColorBrush TextColor
        {
            get
            {
                return _textColor;
            }
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    RaisePropertyChanged("TextColor");
                }
            }
        }

        public bool IsInGoodStanding
        {
            get
            {
                if (SelectedOwner.AccountBalance <= 0) return true;
                else return false;
            }
        }

        private GolfCart _golfCart = null;
        public GolfCart GolfCart
        {
            get
            {
                return this._golfCart;
            }
            set
            {
                // When GolfCart is changed, we unregister the previous PropertyChanged
                // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                // We use this PropertyChanged trigger to handle GolfCart changes.
                if (this._golfCart != null)
                {
                    this._golfCart.PropertyChanged -= GolfCart_PropertyChanged;
                }

                if (value != this._golfCart)
                {
                    this._golfCart = value;
                    // Once the new value is assigned, we register a new PropertyChanged event listner.
                    this._golfCart.PropertyChanged += GolfCart_PropertyChanged;
                    RaisePropertyChanged("GolfCart");
                }
            }
        }

        /// </summary>
        public bool HasRegisteredCart
        {
            get
            {
                try
                {
                    var cart = (from a in SelectedOwner.GolfCarts
                                where a.OwnerID == SelectedOwner.OwnerID
                                select a).LastOrDefault();
                    if (null != cart)
                    {
                        GolfCart = cart;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Getting Cart Info: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                return false;
            }
        }

        public string CartImage
        {
            get
            {
                string _image = String.Empty;
                if (GolfCart.Year == CurrentSeason.TimePeriod && GolfCart.IsPaid)
                {
                    _image = @"/Images/Icons/GolfCart_Icon.png";
                }
                else
                {
                    _image = @"/Images/Icons/GolfCart_Red_Icon.png";
                }
                return _image;
            }
        }

        public Season CurrentSeason
        {
            get
            {
                Season _currentSeason = (from x in dc.Seasons
                                         where x.IsCurrent == true
                                         select x).First();
                return _currentSeason;
            }
        }
        /* ------------------------------------ Public Methods -------------------------------------------- */
        #region Public Methods
        #endregion

        /* ------------------------------------ Private Methods -------------------------------------------- */
        #region Private Methods

        /// <summary>
        /// Builds a history of property notes
        /// </summary>
        /// <returns></returns>
        private string GetOwnerNotes()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var notes = (from n in SelectedOwner.Notes
                             where n.OwnerID == this.SelectedOwner.OwnerID
                             orderby n.Entered descending
                             select n);

                NoteCount = SelectedOwner.Notes.Count();

                // Iterate through the notes collection and build a string of the notes in 
                // decending order.  This string will be reflected in the UI as a read-only
                // history of all note entries.
                foreach (Note n in notes)
                {
                    //n.EnteredBy.Remove(0, "HIGHVALLEYCC\\".Count());

                    sb.Append(n.Entered.ToShortDateString()).Append(" ");
                    sb.Append(n.EnteredBy.Remove(0, "HIGHVALLEYCC\\".Count())).Append(" - ");
                    sb.Append(n.Comment);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Checks to see if Relationships have checked in at the clubhouse already so the UI values
        /// are set correctly.
        /// </summary>
        private void CkFacilityUsage()
        {
            SelectedOwner.GolfMembers = 0;
            SelectedOwner.GolfGuests = 0;
            SelectedOwner.PoolMembers = 0;
            SelectedOwner.PoolGuests = 0;

            DateTime dt1 = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0);
            DateTime dt2 = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 23, 59, 59);

            var zList = (from p in dc.FacilityUsages
                         where (p.OwnerID == SelectedOwner.OwnerID)
                         && (p.Date >= dt1 && p.Date <= dt2)
                         select p);

            foreach (FacilityUsage f in zList)
            {
                Relationship r = (from x in Relationships
                                  where x.RelationshipID == f.RelationshipId
                                  select x).FirstOrDefault();

                if (null != r && r.RelationToOwner != "Guest")
                {
                    r.IsGolf = Convert.ToBoolean(f.GolfRoundsMember);
                    r.IsPool = Convert.ToBoolean(f.PoolMember);
                    SelectedOwner.GolfMembers += f.GolfRoundsMember;
                    SelectedOwner.PoolMembers += f.PoolMember;
                }
                else
                {
                    SelectedOwner.GolfGuests = f.GolfRoundsGuest;
                    SelectedOwner.PoolGuests = f.PoolGuest;
                }
            }
        }

        /// <summary>
        /// Summary
        ///     Raises a property changed event when the NewOwner data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Property_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            if (CkIsValid())
            {
                this.CanSaveExecute = IsDirty;
                RaisePropertyChanged("DataChanged");
            }
        }

        /// <summary>
        /// Relationship PropertyChanged event handler.  When a property of the SelectedRelationship is
        /// changed, the PropertyChanged event is fired, and processed by this handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedRelation_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var x = e.PropertyName;

            // We listen for changes to the IsGolf/IsPool value of the SelectedProperty so we can update the 
            // Check-In counts for the property.

            if (e.PropertyName == "IsGolf" || e.PropertyName == "IsPool")
            {
                // Initialize the Check-In property counters to zero. Then iterate over the collection to
                // get current counts.
                SelectedOwner.GolfMembers = 0;
                SelectedOwner.PoolMembers = 0;

                foreach (Relationship r in this.Relationships)
                {
                    if (r.IsGolf) { SelectedOwner.GolfMembers += 1; }
                    if (r.IsPool) { SelectedOwner.PoolMembers += 1; }
                }
            }
            else
            {
                RaisePropertyChanged("DataChanged");
            }
        }

        /// <summary>
        /// Executes when the RelationsToProcess collection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _relationships_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            string action = e.Action.ToString();

            switch (action)
            {
                case "Reset":
                    break;

                // When items are "selected" (for removal) they will result in an "Add" action to the Relationships collection.
                // When new names are added to the Relationships collection, they too result in an "Add" action.
                // Therefore, the logic needs to determine which asction (Add or Remove) needs to happen.
                case "Add":
                    var newItems = e.NewItems;
                    foreach (Relationship r in newItems)
                    {
                        if (0 != r.RelationshipID)
                        {
                            bool result = Helper.RemoveRelationship(this.dc, r);
                        }
                        else
                        {
                            bool result = Helper.AddRelationship(this.dc, SelectedOwner, r);
                        }
                    }
                    break;
                case "Remove":
                    var oldItems = e.OldItems;
                    foreach (Relationship r in oldItems)
                    {
                        bool result = Helper.AddRelationship(this.dc, SelectedOwner, r);
                    }
                    break;
            }
            //int i = Relationships.Count();
            //ChangeSet cs = this.dc.GetChangeSet();
            CanSaveExecute = IsDirty;
            RaisePropertyChanged("DataChanged");
        }

        /// <summary>
        /// GolfCart PropertyChanged event handler.  When a property of the GolfCart object is
        /// changed, the PropertyChanged event is fired, and processed by this handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GolfCart_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var x = e.PropertyName;

            // We listen for changes to the IsPaid/IsReceived value of the GolfCart object 
            switch (e.PropertyName)
            {
                case "IsPaid":
                    GolfCart.PaymentDate = DateTime.Now;
                    break;
                case "IsReceived":
                    GolfCart.ReceivedDate = DateTime.Now;
                    break;
                default:
                    break;
            }
                RaisePropertyChanged("DataChanged");
        }

        #endregion

        /* ----------------------------------- Style Propertes ---------------------------------------- */
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
                if (this.ApplPermissions.CanEditProperty)
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
                if (this.ApplPermissions.CanEditProperty || this.ApplPermissions.CanEditOwnerNotes)
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
                if (this.ApplPermissions.CanEditProperty)
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
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class OwnerEditViewModel : CommonViewModel, ICommandSink
    {
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
            this.dc.SubmitChanges();
            dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Notes);
            AllNotes = GetOwnerNotes();
            NewNote = string.Empty;
            this.IsBusy = false;
            RaisePropertyChanged("DataChanged");
            CanSaveExecute = IsDirty;
            //Host.Execute(HostVerb.Close, this.Caption);
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
    /// <summary>
    /// ViewModel Commands
    /// </summary>
    public partial class OwnerEditViewModel
    {

        /// <summary>
        /// CheckIn Command
        /// </summary>
        private ICommand _checkInCommand;
        public ICommand CheckInCommand
        {
            get
            {
                return _checkInCommand ?? (_checkInCommand = new CommandHandler(() => CheckInAction(), true));
            }
        }

        /// <summary>
        /// Check-In button click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void CheckInAction()
        {
            try
            {
                MessageBoxResult results = MessageBoxResult.Cancel;

                if (SelectedOwner.AccountBalance > 0)
                {
                    results = MessageBox.Show("This member's privileges are suspended.\nAsk them to make a payment before allowing them to check in.\n Click OK to continue Checking In, or Cancel to not Check In"
                        , "Warning"
                        , MessageBoxButton.OKCancel
                        , MessageBoxImage.Exclamation
                        );
                }
                else if ((SelectedOwner.GolfGuests+SelectedOwner.PoolGuests) > 10)
                {
                    results = MessageBox.Show("Maximum number of guests per day has been exceeded.\nInform member the maximum number of guests per day is 10.\n Click OK to continue Checking In, or Cancel to not Check In"
                        , "Warning"
                        , MessageBoxButton.OKCancel
                        , MessageBoxImage.Exclamation
                        );
                }
                else
                {
                    results = MessageBox.Show("Proceed with Check In?"
                        , "Proceed"
                        , MessageBoxButton.OKCancel
                        , MessageBoxImage.Question
                        );
                    //results = MessageResult.OK;
                }

                // If the property is in good standing, or staff allows member to check in, then proceed
                if (MessageBoxResult.OK == results)
                {
                    FacilityUsage usage = null;

                    DateTime dt1 = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 0, 0, 0);
                    DateTime dt2 = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 23, 59, 59);

                    // Query to see if any Owner relationships have checked-in and have facility usage records for today. 
                    var zList = (from p in dc.FacilityUsages
                             where (p.OwnerID == SelectedOwner.OwnerID)
                             && (p.Date >= dt1 && p.Date <= dt2)
                             select p);


                    // Iterate over the Relationship records. If a relationship has checked-in previously we will update their
                    // record if there is a change. For example, in the morning they check-in for golf, and later in the day
                    // they check-in for the pool.  Otherwise, if this is the first check-in we create a new record.  Simliar
                    // logic applies for Guests. If the number of guests changes we record the change.
                    foreach (Relationship r in Relationships)
                    {
                        FacilityUsage xUsage = (from u in zList
                                                where u.RelationshipId == r.RelationshipID
                                                select u).FirstOrDefault();
                        if (null != xUsage)
                        {
                            usage = xUsage;
                            usage.GolfRoundsMember = Convert.ToInt32(r.IsGolf);
                            usage.PoolMember = Convert.ToInt32(r.IsPool);
                        }
                        else
                        {
                            usage = new FacilityUsage();

                            if (r.IsGolf || r.IsPool)
                            {

                                usage.OwnerID = SelectedOwner.OwnerID;
                                usage.RelationshipId = r.RelationshipID;
                                usage.Date = DateTime.Now;
                                usage.GolfRoundsMember = Convert.ToInt32(r.IsGolf);
                                usage.PoolMember = Convert.ToInt32(r.IsPool);

                                usage.GolfRoundsGuest = 0;
                                usage.PoolGuest = 0;
                                dc.FacilityUsages.InsertOnSubmit(usage);

                                // Flip the Golf & Pool bits so they aren't left in a true state
                                r.IsPool = false;
                                r.IsGolf = false;
                            }
                        }
                    }

                    // If there are guests of the members, add them last.
                    if (0 < this.SelectedOwner.PoolGuests || 0 < this.SelectedOwner.GolfGuests)
                    {
                        FacilityUsage gUsage = null;
                        Relationship guest = (from q in this.dc.Relationships
                                              where q.RelationToOwner == "Guest"
                                              select q).FirstOrDefault();

                        gUsage = (from x in zList
                                  where x.OwnerID == SelectedOwner.OwnerID
                                  select x).FirstOrDefault();

                        if (null != gUsage)
                        {
                            gUsage.GolfRoundsGuest = SelectedOwner.GolfGuests;
                            gUsage.PoolGuest = SelectedOwner.PoolGuests;
                        }
                        else
                        {
                            gUsage = new FacilityUsage();
                            gUsage.OwnerID = SelectedOwner.OwnerID;
                            gUsage.RelationshipId = guest.RelationshipID;
                            gUsage.Date = DateTime.Now;
                            gUsage.GolfRoundsGuest = SelectedOwner.GolfGuests;
                            gUsage.PoolGuest = SelectedOwner.PoolGuests;

                            dc.FacilityUsages.InsertOnSubmit(gUsage);
                        }
                    }

                    ChangeSet cs = dc.GetChangeSet();
                    dc.SubmitChanges();
                    MessageBox.Show("Check In Complete");
                }
                else
                {
                    MessageBox.Show("Check In was canceled. No data was recorded.");
                    foreach (Relationship r in Relationships)
                    {
                        r.IsPool = false;
                        r.IsGolf = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Check in Error: " + ex.Message);
            }
            finally
            {
                IsCheckinEnabled = false;
                this.SelectedOwner.PoolMembers = 0;
                this.SelectedOwner.PoolGuests = 0;
                this.SelectedOwner.GolfMembers = 0;
                this.SelectedOwner.GolfGuests = 0;

                Host.Execute(HostVerb.Close, this.Caption);
            }
        }

        /// <summary>
        /// AddRelationship Command
        /// </summary>
        private ICommand _addRelationshipCommand;
        public ICommand AddRelationshipCommand
        {
            get
            {
                return _addRelationshipCommand ?? (_addRelationshipCommand = new CommandHandlerWparm((object parameter) => AddRelationshipAction(parameter), true));
            }
        }

        /// <summary>
        /// AddRelationshipCommand Action
        /// </summary>
        /// <param name="parameter"></param>
        public void AddRelationshipAction(object parameter)
        {
            NavigationStyle = GridViewNavigationStyle.Row;
            NewItemPosition = NewItemRowPosition.Top;
        }

        /// <summary>
        /// RemoveRelationship Command
        /// </summary>
        private ICommand _removeRelationshipCommand;
        public ICommand RemoveRelationshipCommand
        {
            get
            {
                return _removeRelationshipCommand ?? (_removeRelationshipCommand = new CommandHandlerWparm((object parameter) => RemoveRelationshipAction(parameter), true));
            }
        }

        /// <summary>
        /// AddRelationshipCommand Action
        /// </summary>
        /// <param name="parameter"></param>
        public void RemoveRelationshipAction(object parameter)
        {
            Relationship r = parameter as Relationship;
            Helper.RemoveRelationship(this.dc, r);
            this.Relationships.Remove(r);
            CanSaveExecute = IsDirty;
        }

        /// <summary>
        /// TextEdit LostFocus Event to Command
        /// </summary>
        private ICommand _teLostFocusCommand;
        public ICommand TELostFocusCommand
        {
            get
            {
                return _teLostFocusCommand ?? (_teLostFocusCommand = new CommandHandlerWparm((object parameter) => TELostFocusAction(parameter), true));
            }
        }

        /// <summary>
        /// TextEdit (Notes) LostFocus Event Action
        /// </summary>
        /// <param name="parameter"></param>
        public void TELostFocusAction(object parameter)
        {
            Note n = new Note();
            n.Owner = SelectedOwner;
            n.Comment = NewNote;
            dc.Notes.InsertOnSubmit(n);
            CanSaveExecute = IsDirty;
            RaisePropertyChanged("DataChanged");
        }

        /// <summary>
        /// Property grid row double-click command
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
        /// Property Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void RowDoubleClickAction(object parameter)
        {
            Property p = parameter as Property;
            IsBusy = true;
            Host.Execute(HostVerb.Open, "PropertyEdit", p);
        }

        /// <summary>
        /// Financial Transaction Command
        /// </summary>
        private ICommand _financialTransactionCommand;
        public ICommand FinancialTransactionCommand
        {
            get
            {
                return _financialTransactionCommand ?? (_financialTransactionCommand = new CommandHandlerWparm((object parameter) => FinancialTransactionAction(parameter), true));
            }
        }

        /// <summary>
        /// Financial Transaction Action
        /// </summary>
        /// <param name="type"></param>
        public void FinancialTransactionAction(object parameter)
        {
            Owner p = parameter as Owner;
            IsBusy = true;
            Host.Execute(HostVerb.Open, "FinancialTransaction", p);
        }

        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _waterShutoffCommand;
        public ICommand WaterShutoffCommand
        {
            get
            {
                return _waterShutoffCommand ?? (_waterShutoffCommand = new CommandHandlerWparm((object parameter) => WaterShutoffAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void WaterShutoffAction(object parameter)
        {
            Owner p = parameter as Owner;
            IsBusy = true;
            Host.Execute(HostVerb.Open, "WaterShutoffEdit", p);
        }

        /// <summary>
        /// (ImageEdit) Drop Event to Command
        /// </summary>
        private ICommand _dropCommand;
        public ICommand DropCommand                // Currently not working......
        {
            get
            {
                return _dropCommand ?? (_dropCommand = new CommandHandlerWparm((object parameter) => DropAction(parameter), true));
            }
        }

        /// <summary>
        /// ImageEdit Drop Event Action
        /// </summary>
        /// <param name="parameter"></param>
        public void DropAction(object parameter)
        {
            // Extract the data from the DataObject-Container into a string list
            string[] FileList = null; // (string[])e.Data.GetData(DataFormats.FileDrop, false);

            // Do something with the data...

            // For example add all files into a simple label control:
            //foreach (string File in FileList)
            //    this.label.Text += File + "\n";
        }

        /// <summary>
        /// Print Command
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
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void RefreshAction(object parameter)
        {
            RaisePropertyChanged("AccountBalance");
            RaisePropertyChanged("FinancialTransactions");
            RaisePropertyChanged("NotesHeader");
            RaisePropertyChanged("AllNotes");
        }

        ///// <summary>
        /////  Command Template
        ///// </summary>
        //private ICommand _templateCommand;
        //public ICommand TemplateCommand
        //{
        //    get
        //    {
        //        return _templateCommand ?? (_templateCommand = new CommandHandlerWparm((object parameter) => TemplateAction(parameter), ApplPermissions.CanImport));
        //    }
        //}

        ///// <summary>
        ///// Facility Usage by date range report
        ///// </summary>
        ///// <param name="type"></param>
        //public void TemplateAction(object parameter)
        //{
        //}
    }

    /*===============================================================================================================================================*/
    /// <summary>
    /// ViewModel Validation
    /// </summary>

    public partial class OwnerEditViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CkIsValid()
        {
            StringBuilder message = new StringBuilder();

            if (
                   !String.IsNullOrEmpty(SelectedOwner.MailTo)
                && !String.IsNullOrEmpty(SelectedOwner.Address)
                && !String.IsNullOrEmpty(SelectedOwner.City)
                && !String.IsNullOrEmpty(SelectedOwner.State)
                && !String.IsNullOrEmpty(SelectedOwner.Zip)
                )
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers the required Properties to be validated
        /// </summary>
        string IDataErrorInfo.Error
        {
            get
            {
                //if (!allowValidation) return null;

                IDataErrorInfo iDataErrorInfo = (IDataErrorInfo)this;
                string error = String.Empty;

                //// The following properties must contain data in order to pass basic validation
                error =
                    iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedOwner.MailTo)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedOwner.Address)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedOwner.City)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedOwner.State)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedOwner.Zip)];

                if (!string.IsNullOrEmpty(error))
                {
                    return "Please check input data.";
                    //return error;
                }
                return null;
            }
        }

        /// <summary>
        /// Assign the validation rule on based on the property name
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        string IDataErrorInfo.this[string columnName]
        {
            get
            { // is hit...
                //// If invoked, will throw validation error if property is null/blank

                StringBuilder errorMsg = new StringBuilder();

                if (columnName == BindableBase.GetPropertyName(() => SelectedOwner.MailTo))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "MailTo", SelectedOwner.MailTo));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedOwner.Address))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "Address", SelectedOwner.Address));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedOwner.City))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "City", SelectedOwner.City));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedOwner.State))
                {
                    errorMsg.Append(RequiredValidationRule.CkStateAbbreviation(() => "State", SelectedOwner.State));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedOwner.Zip))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "Zip", SelectedOwner.Zip));
                    return errorMsg.ToString();
                }
                //// No errors found......
                return null;
            }
        }
        #region IsValid
        /// <summary>
        /// Runs validation on the view model
        /// </summary>
        /// <returns></returns>
        //public bool IsValid()
        //{
        //    StringBuilder message = new StringBuilder();

        //    message.Append(this.EnableValidationAndGetError());


        //    if (!String.IsNullOrEmpty(SelectedProperty.OwnerFName))
        //    {
        //        message.Append(RequiredValidationRule.CheckNullInput(SelectedProperty.OwnerFName, SelectedProperty.OwnerFName));
        //    }

        //    if (!string.IsNullOrEmpty(message.ToString()))
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        #endregion


    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class OwnersEditViewModel : IDisposable
    public partial class OwnerEditViewModel : IDisposable
    {
        // Resources that must be disposed:
        private HVCCDataContext dc = null;

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
        ~OwnerEditViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}
