// TO-DO:  Plumb the Save button to the IsEnabledSave
////////////////////////////////////////////////////////////////////////////////////////////
namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Linq;
    using HVCC.Shell.Common;
    using DevExpress.Mvvm;
    using HVCC.Shell.Models;
    using DevExpress.Spreadsheet;
    using System.Windows.Input;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Resources;
    using DevExpress.Xpf.Editors;
    using System.Windows;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using DevExpress.Xpf.Grid;
    using System.Diagnostics;
    using HVCC.Shell.Validation;

    public partial class PropertyEditViewModel : CommonViewModel, ICommandSink
    {

        public PropertyEditViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            Property p = parameter as Property;
            if (null != p)
            {
                SelectedProperty = GetProperty(p.PropertyID);
                Owner = SelectedProperty.Owner;

                Relationships = GetRelationships();
                if (0 < Relationships.Count())
                {
                    SelectedRelationship = Relationships[0];
                }
            }
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            CanSaveExecute = false;
            this.RegisterCommands();
        }

        /* -------------------------------- Interfaces ------------------------------------------------ */
        #region Interfaces
        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }
        public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
        public virtual ISaveFileDialogService SaveFileDialogService { get { return GetService<ISaveFileDialogService>(); } }
        #endregion

        public enum ExportType { PDF, XLSX }
        public enum PrintType { PREVIEW, PRINT }

        /* ------------------------------------- Properties --------------------------- */
        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

        public override bool IsValid { get { return CkIsValid(); } }

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

        /// <summary>
        /// Returns boolean value to indicate if the selected property has a registered golf cart
        /// </summary>
        public bool HasRegisteredCart
        {
            get
            {
                // Using the PropertyXOwner Xref to get the OwnerID, we can
                // then query the GolfCartXOwner to see if the owner owns a golf cart.
                var cart = (from a in this.dc.GolfCarts
                           where a.OwnerID == SelectedProperty.Owner.OwnerID
                           select a).FirstOrDefault();
                if (null == cart) { return false; }
                else { return true; }
            }
        }

        private string _headerText = string.Empty;
        public string HeaderText
        {
            get
            {
                return _headerText;
            }
            set
            {
                if (_headerText != value)
                {
                    _headerText = value;
                }
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

        #region Properties

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        private Property _selectedProperty = null;
        public Property SelectedProperty
        {
            get
            {
                _headerText = String.Format("Lot# {0}", _selectedProperty.Customer);
                return _selectedProperty;
            }
            set
            {
                //// wrap the setter with a check for a null value.  This condition happens when
                //// a Relationship is selected from the Relationship grid. Therefore, when
                //// a Relationship is selected we won't null out the SelectedProperty.
                if (null != value)
                {
                    if (value != this._selectedProperty)
                    {
                        // When the selected property is change; a new selection is made, we unregister the previous PropertyChanged
                        // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                        if (this._selectedProperty != null)
                        {
                            this._selectedProperty.PropertyChanged -= SelectedProperty_PropertyChanged;
                        }

                        this._selectedProperty = value;
                        // Once the new value is assigned, we register a new PropertyChanged event listner.
                        this._selectedProperty.PropertyChanged += SelectedProperty_PropertyChanged;

                        this._selectedProperty.PropertyComments = GetPropertyNotes();
                    }
                    RaisePropertyChanged("SelectedProperty");
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
                if (value != this._selectedRelationship)
                {
                    // When the selected relationship is change; a new selection is made, we unregister the previous PropertyChanged
                    // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                    // We use this PropertyChanged trigger to handle Image changes, and to manage the Golf/Pool check ins.
                    if (this._selectedRelationship != null)
                    {
                        this._selectedRelationship.PropertyChanged -= SelectedRelation_PropertyChanged;
                    }

                    this._selectedRelationship = value;
                    if (null != this._selectedRelationship)
                    {
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
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<Relationship> _relationships = null;
        public ObservableCollection<Relationship> Relationships
        {
            get
            {
                this._relationships.CollectionChanged += _relationshipsToProcess_CollectionChanged;
                return this._relationships;
            }
            set
            {
                if (_relationships != value)
                {
                    _relationships = value;
                }
            }
        }

        private Owner _owner = null;
        public Owner Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                if (value != _owner)
                {
                    // When the selected relationship is change; a new selection is made, we unregister the previous PropertyChanged
                    // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                    // We use this PropertyChanged trigger to handle Image changes, and to manage the Golf/Pool check ins.
                    if (_owner != null)
                    {
                        _owner.PropertyChanged -= SelectedProperty_PropertyChanged;
                    }

                    _owner = value;
                    if (null != _owner)
                    {
                        // Once the new value is assigned, we register a new PropertyChanged event listner.
                        _owner.PropertyChanged += SelectedProperty_PropertyChanged;

                        RaisePropertyChanged("Owner");
                    }
                }
            }
        }

        #endregion

        #region Property_Changed
        /// <summary>
        /// Summary
        ///     Raises a property changed event when the SelectedCart data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedProperty_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {            
            if (CkIsValid() && IsDirty)
            {
                CanSaveExecute = true;
                RaisePropertyChanged("DataChanged");
            }
        }

        /// <summary>
        /// Relationship PropertyChanged event handler
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
                this.SelectedProperty.GolfMembers = 0;
                this.SelectedProperty.PoolMembers = 0;

                foreach (Relationship r in this.Relationships)
                {
                    if (r.IsGolf) { this.SelectedProperty.GolfMembers += 1; }
                    if (r.IsPool) { this.SelectedProperty.PoolMembers += 1; }
                }
            }
            RaisePropertyChanged("DataChanged");
        }

        /// <summary>
        /// Executes when the RelationsToProcess collection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _relationshipsToProcess_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
                            // If the FName string is empty, than we landed here as a result of an 'Add' to the 
                            // Relationship collection firing on a collection change event.  We can ignore this
                            // request for now.
                            if (!string.IsNullOrEmpty(r.FName)
                                && !string.IsNullOrEmpty(r.LName)
                                && !string.IsNullOrEmpty(r.RelationToOwner))
                            {
                                bool result = Helper.AddRelationship(this.dc, SelectedProperty.Owner, r);
                            }
                        }
                    }
                    break;
                case "Remove":
                    var oldItems = e.OldItems;
                    foreach (Relationship r in oldItems)
                    {
                        bool result = Helper.AddRelationship(this.dc, SelectedProperty.Owner, r);
                    }
                    break;
            }
            //if (0 < _relationshipsToProcess.Count()) { CanSaveExecute = true; } else { CanSaveExecute = false; }
            CanSaveExecute = IsDirty;
            RaisePropertyChanged("DataChanged");
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

        /* ---------------------------------- Public/Private Methods ------------------------------------------ */
        #region Methods
        private Property GetProperty(int pID)
        {
            try
            {
                Property p = (from x in dc.Properties
                              where x.PropertyID == pID
                              select x).FirstOrDefault();
                return p;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("Can't retrieve property from database\n" + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Returns the Notes for a given Property
        /// </summary>
        /// <returns></returns>
        private string GetPropertyNotes()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var notes = (from n in this.dc.Notes
                             where n.PropertyID == this.SelectedProperty.PropertyID
                             orderby n.Entered descending
                             select n);

                this._noteCount = notes.Count();

                // Iterate through the notes collection and build a string of the notes in 
                // decending order.  This string will be reflected in the UI as a read-only
                // history of all note entries.
                foreach (Note n in notes)
                {
                    sb.Append(n.Entered.ToShortDateString()).Append(" ");
                    sb.Append(n.EnteredBy).Append(" - ");
                    sb.Append(n.Comment);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a collection of Relationships for a given Property
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<Relationship> GetRelationships ()
        {
            try
            {
                // Get the list of Relationships for the Property owner using the OxP xref
                var list = (from x in dc.Relationships
                                        where x.OwnerID == SelectedProperty.OwnerID
                                        && x.Active == true
                                        select x);

                return new ObservableCollection<Relationship>(list);
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("Can't retrieve property from database\n" + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
                return null;
            }
        }
        #endregion
    }
    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class PropertyEditViewModel : CommonViewModel, ICommandSink
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
        private bool CanSaveExecute { get; set; }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            RaisePropertyChanged("IsBusy");
            ChangeSet cs = dc.GetChangeSet();
            this.dc.SubmitChanges();             
            this.IsBusy = false;
            RaisePropertyChanged("Refresh");
            RaisePropertyChanged("IsNotBusy");
            Host.Execute(HostVerb.Close, this.Caption);
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
    /// <summary>
    /// 
    /// </summary>
    public partial class PropertyEditViewModel
    {

        /// <summary>
        /// Print Command
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

                if (!this.SelectedProperty.IsInGoodStanding)
                {
                    results = MessageBox.Show("This member is not is good standing.\nAsk them to make a payment before allowing them to check in.\n Click OK to continue Checking In, or Cancel to not Check In"
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
                    string activity = string.Empty;

                    //using (dc)
                    //{
                    List<FacilityUsage> usages = new List<FacilityUsage>();

                    // Register the members what are checking in.
                    foreach (Relationship r in Relationships)
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
                                MessageBoxService.ShowMessage(msg, "Warning", MessageButton.OK, MessageIcon.Information, MessageResult.OK);
                            }

                            // Flip the Golf & Pool bits so they aren't left in a true state
                            r.IsPool = false;
                            r.IsGolf = false;
                        }

                        // If there are guests of the members, add them last.
                        if (0 < this.SelectedProperty.PoolGuests || 0 < this.SelectedProperty.GolfGuests)
                        {
                            Relationship guest = (from q in this.dc.Relationships
                                                  where q.RelationToOwner == "Guest"
                                                  select q).FirstOrDefault();

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
                this.SelectedProperty.PoolMembers = 0;
                this.SelectedProperty.PoolGuests = 0;
                this.SelectedProperty.GolfMembers = 0;
                this.SelectedProperty.GolfGuests = 0;

                Host.Execute(HostVerb.Close, this.Caption);
            }
        }

        /// <summary>
        /// Drop Command
        /// </summary>
        private ICommand _dropCommand;
        public ICommand DropCommand
        {
            get
            {
                return _dropCommand ?? (_dropCommand = new CommandHandlerWparm((object parameter) => DropAction(parameter), true));
            }
        }

        /// <summary>
        /// ImageEdit (drag &)drop event to command action
        /// </summary>
        /// <param name="type"></param>
        public void DropAction(object parameter)
        {
            DragEventArgs e = parameter as DragEventArgs;
            try
            {
                // Only supports file drag & drop
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    return;
                }

                //Drag the file access
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Only supports single file drag & drop
                if (1 < files.Count())
                {
                    MessageBox.Show("You can only drop a single image on this control");
                    return;
                }

                //Note that, because the program supports both pulled also supports drag the past, then ListBox can receive its drag and drop files
                //In order to prevent conflict mouse clicking and dragging, need to be shielded from the program itself to drag the file
                //Here to determine whether a document from outside the program drag in, is to determine the image is in the working directory
                if (files.Length > 0 && (e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }

                foreach (string file in files)
                {
                    try
                    {
                        //If the image is from the external drag in, make a backup copy of the file to the working directory
                        ////string destFile = path + System.IO.Path.GetFileName(file);

                        switch (e.Effects)
                        {
                            case DragDropEffects.Copy:
                                ////File.Copy(file, destFile, false);
                                if ((Path.GetExtension(file)).Contains(".png") ||
                                    (Path.GetExtension(file)).Contains(".PNG") ||
                                    (Path.GetExtension(file)).Contains(".jpg") ||
                                    (Path.GetExtension(file)).Contains(".JPG") ||
                                    (Path.GetExtension(file)).Contains(".jpeg") ||
                                    (Path.GetExtension(file)).Contains(".JPEG") ||
                                    (Path.GetExtension(file)).Contains(".gif") ||
                                    (Path.GetExtension(file)).Contains(".GIF"))
                                {
                                    SelectedRelationship.Photo = this.ApplDefault.Photo; // Helper.LoadImage(file);
                                }
                                else
                                {
                                    MessageBox.Show("Only JPG, GIF and PNG files are supported");
                                    return;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Already exists in this file or import the non image files！");
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing image file. " + ex.Message);
            }
        }


        /// <summary>
        /// Drop Command
        /// </summary>
        private ICommand _validateRowCommand;
        public ICommand ValidateRowCommand
        {
            get
            {
                return _validateRowCommand ?? (_validateRowCommand = new CommandHandlerWparm((object parameter) => ValidateRowAction(parameter), true));
            }
        }

        /// <summary>
        /// ImageEdit (drag &)drop event to command action
        /// </summary>
        /// <param name="type"></param>
        public void ValidateRowAction(object parameter)
        {
            GridRowValidationEventArgs e = parameter as GridRowValidationEventArgs;
            try
            {
                if (e.IsValid)
                {
                    Helper.AddRelationship(dc, SelectedProperty.Owner, e.Row as Relationship);
                    if (IsDirty)
                    {
                        CanSaveExecute = true;
                        RaisePropertyChanged("DataChanged");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing image file. " + ex.Message);
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
                MessageBoxService.Show(ex.Message);
            }
            finally
            {
            }
        }

        private bool _canViewMap = true;
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


    }

    /*======================================================= Validation ==============================================================================*/
    public partial class PropertyEditViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CkIsValid()
        {
            StringBuilder message = new StringBuilder();

            if (
                   !String.IsNullOrEmpty(SelectedProperty.Owner.MailTo)
                && !String.IsNullOrEmpty(SelectedProperty.Owner.Address)
                && !String.IsNullOrEmpty(SelectedProperty.Owner.City)
                && !String.IsNullOrEmpty(SelectedProperty.Owner.State)
                && !String.IsNullOrEmpty(SelectedProperty.Owner.Zip)
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
                IDataErrorInfo iDataErrorInfo = (IDataErrorInfo)this;
                string error = String.Empty;

                //// The following properties must contain data in order to pass basic validation
                error =
                      iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedProperty.Owner.MailTo)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedProperty.Owner.Address)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedProperty.Owner.City)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedProperty.Owner.State)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => SelectedProperty.Owner.Zip)];

                if (!string.IsNullOrEmpty(error))
                {
                    return "Please check input data.";
                    //return error;
                }
                return null;

            }
        }

        /// <summary>
        /// Runs the validation rules on the FileInformationView based on the property name
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                // If invoked, will throw validation error if property is null/blank

                StringBuilder errorMsg = new StringBuilder();

                if (columnName == BindableBase.GetPropertyName(() => SelectedProperty.Owner.MailTo))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "MailTo", SelectedProperty.Owner.MailTo));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedProperty.Owner.Address))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "Address", SelectedProperty.Owner.Address));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedProperty.Owner.City))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "City", SelectedProperty.Owner.City));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedProperty.Owner.State))
                {
                    errorMsg.Append(RequiredValidationRule.CkStateAbbreviation(() => "State", SelectedProperty.Owner.State));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => SelectedProperty.Owner.Zip))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "Zip", SelectedProperty.Owner.Zip));
                    return errorMsg.ToString();
                }
                //// No errors found......
                return null;
            }
        }
        #region IsValid
        ///// <summary>
        ///// Runs validation on the view model
        ///// </summary>
        ///// <returns></returns>
        //public string IsValid()
        //{
        //    StringBuilder message = new StringBuilder();

        //    message.Append(this.EnableValidationAndGetError());


        //    if (!String.IsNullOrEmpty(SelectedProperty.OwnerFName))
        //    {
        //        message.Append(RequiredValidationRule.CheckNullInput(SelectedProperty.OwnerFName, SelectedProperty.OwnerFName));
        //    }

        //    return message.ToString();
        //}
        #endregion


    }

    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertyEditViewModel : IDisposable
    public partial class PropertyEditViewModel : IDisposable
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
        ~PropertyEditViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
