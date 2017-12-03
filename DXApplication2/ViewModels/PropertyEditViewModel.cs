﻿namespace HVCC.Shell.ViewModels
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

    public partial class PropertyEditViewModel : CommonViewModel, ICommandSink
    {

        public PropertyEditViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            if (null != this.Host.Parameter)
            {
                int pID;
                Int32.TryParse(this.Host.Parameter.ToString(), out pID);
                this.SelectedProperty = GetProperty(pID);
            }
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
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
        private Property _selectedProperty = null;
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

                        if (null == this._selectedRelationship.Photo)
                        {
                            this._selectedRelationship.Photo = this.ApplDefault.Photo;
                        }
                        _selectedRelationship.Image = Helper.ArrayToBitmapImage(this._selectedRelationship.Photo.ToArray());
                        RaisePropertyChanged("SelectedRelationship");
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
            RaisePropertyChanged("DataChanged");
        }

        /// <summary>
        /// Relationship PropertyChanged event handler
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
                if (null != p)
                {
                    this.SelectedRelationship = (from r in p.Relationships
                                                 where r.RelationToOwner == "Owner"
                                                 select r).FirstOrDefault();
                }
                return p;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowMessage("Can't retrieve property from database\n" + ex.Message, "Error", MessageButton.OK, MessageIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Builds a history of property notes
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
        #endregion
    }
    /*================================================================================================================================================*/

    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class PropertyEditViewModel : CommonViewModel, ICommandSink
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
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            RaisePropertiesChanged("IsBusy");
            ChangeSet cs = dc.GetChangeSet();
            //this.dc.SubmitChanges(); (DEBUG)
            this.IsBusy = false;
            RaisePropertiesChanged("IsNotBusy");
            Host.Execute(HostVerb.Close, this.Caption);
        }

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
                                    MessageBoxService.ShowMessage(msg, "Warning", MessageButton.OK, MessageIcon.Information, MessageResult.OK);
                                }

                                // Flip the Golf & Pool bits so they aren't left in a true state
                                r.IsPool = false;
                                r.IsGolf = false;
                            }
                        //}

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

                        ChangeSet cs = dc.GetChangeSet();
                        //dc.SubmitChanges(); (DEBUG)
                        MessageBox.Show("Check In Complete");
                    }
                }
                else
                {
                    MessageBox.Show("Check In was canceled. No data was recorded.");
                    foreach (Relationship r in this.SelectedProperty.Relationships)
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
        /// Print Command
        /// </summary>
        private ICommand _imageEditValueChangedCommand;
        public ICommand ImageEditValueChangedCommand
        {
            get
            {
                return _imageEditValueChangedCommand ?? (_imageEditValueChangedCommand = new CommandHandlerWparm((object parameter) => ImageEditValueChangedAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void ImageEditValueChangedAction(object parameter)
        {
            EditValueChangedEventArgs e = parameter as EditValueChangedEventArgs;
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
