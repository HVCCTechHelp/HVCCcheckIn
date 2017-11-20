namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using DevExpress.Xpf.Docking;
    using System.Data.Linq;
    using HVCC.Shell.Common;
    using DevExpress.Mvvm;
    using DevExpress.Mvvm.DataAnnotations;
    using HVCC.Shell.Models;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Helpers;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Spreadsheet;
    using System.Collections.Generic;
    using System.Windows;
    using System.Text;
    using System.Windows.Input;
    using Resources;

    [POCOViewModel]
    public partial class PropertiesViewModel : ViewModelBase, INotifyPropertyChanged
    {

        /* -------------------------------- Interfaces ------------------------------------------------ */
        #region Interfaces
        public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }
        public virtual ISaveFileDialogService SaveFileDialogService { get { return null; } }
        protected virtual IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        public virtual IExportService ExportService { get { return null; } }
        //public virtual IGridControlService GridControlService { get { return null; } }

        [ServiceProperty(Key = "PropertyEditDialogService")]
        public virtual IDialogService PropertyEditDialogService { get { return null; } }
        [ServiceProperty(Key = "RelationshipDialogService")]
        public virtual IDialogService RelationshipDialogService { get { return null; } }
        [ServiceProperty(Key = "WaterSystemEditDialogService")]
        public virtual IDialogService WaterSystemEditDialogService { get { return null; } }

        #endregion

        /* ------------------------------ ViewModel Constructor --------------------------------------- */
        /// <summary>
        /// ViewModel Constructor
        /// </summary>
        public PropertiesViewModel()
        {
        }

        private ObservableCollection<Object> _viewModels = new ObservableCollection<Object>();
        public ObservableCollection<Object> ViewModels
        {
            get
            {
                return _viewModels;
            }
            set
            {
                if (value != _viewModels)
                {
                    _viewModels = value;
                }
            }
        }

        /* ------------------------------ Public Variables and Types ----------------------------------------- */
        #region Public Variables
        int RowNum;

        public enum Column : int
        {
            Customer = 0,
            BillTo = 1,
            Balance = 2
        }

        public enum ExportType
        {
            PDF,
            XLSX
        }
        public enum PrintType
        {
            PREVIEW,
            PRINT
        }
        public enum DialogType
        {
            ADD,
            EDIT,
            VIEW
        }
        public enum UserRole
        {
            NA,
            DBO,
            Permanent,
            Seasonal,
            BoardMember
        }
        public virtual bool DialogResult { get; protected set; }
        public virtual string ResultFileName { get; protected set; }
        #endregion

        /* -------------------------------- DataBase Roles -------------------------------------------- */
        #region DataBase Roles/Permissions
        public UserRole DBRole
        {
            get
            {
                //return UserRole.Seasonal;
                if (this.IsMember(new DatabaseRoleInfo("Staff-Seasonal", "HVCC")))
                {
                    return UserRole.Seasonal;
                }
                else if (this.IsMember(new DatabaseRoleInfo("Staff-Permanent", "HVCC")))
                {
                    return UserRole.Permanent;
                }
                else if (this.IsMember(new DatabaseRoleInfo("BoardMember", "HVCC")))
                {
                    return UserRole.BoardMember;
                }
                else if (this.IsMember(new DatabaseRoleInfo("db_owner", "HVCC")))
                {
                    return UserRole.DBO;
                }
                else
                {
                    return UserRole.NA;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private ApplicationPermission _appPermissions = null;
        public ApplicationPermission ApplPermissions
        {
            get
            {
                if (null == this._appPermissions)
                {
                    try
                    {
                        //// Get the list of "ApplicationPermissions" from the database
                        object perms = (from a in this.dc.ApplicationPermissions
                                        where a.RoleIndex == (int)this.DBRole //(int)UserRole.Permanent //
                                        select a).FirstOrDefault();

                        _appPermissions = perms as ApplicationPermission;

                        return _appPermissions;
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }
                return _appPermissions;
            }
            set
            {
                if (this._appPermissions != value)
                {
                    this._appPermissions = value;
                    RaisePropertyChanged("ApplPermissions");
                }
            }
        }

        private ApplicationDefault _applDefault = null;
        public ApplicationDefault ApplDefault
        {
            get
            {
                if (null == this._applDefault)
                {
                    try
                    {
                        //// Get the list of "ApplicationPermissions" from the database
                        object defaults = (from a in this.dc.ApplicationDefaults
                                           select a).FirstOrDefault();

                        _applDefault = defaults as ApplicationDefault;

                        return _applDefault;
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }
                return _applDefault;
            }
        }

        #endregion

        /* ----------------------------------View Model Entities ------------------------------------ */
        #region ViewModel Entities

        /// <summary>
        /// Primary document group for the Main Window
        /// </summary>
        private DocumentGroup _docGroup = null;
        public DocumentGroup DocGroup
        {
            get
            {
                return _docGroup;
            }
            set
            {
                if (this._docGroup != value)
                {
                    this._docGroup = value;
                }
            }
        }

        /// <summary>
        /// The active document panel (has focus)
        /// </summary>
        private BaseLayoutItem _activeDocPanel = null;
        public BaseLayoutItem ActiveDocPanel
        {
            get
            {
                return this._activeDocPanel;
            }
            set
            {
                if (this._activeDocPanel != value)
                {
                    this._activeDocPanel = value;

                    ////
                    //// Enable/Disable the common ribbon buttons
                    ////

                    // Do we enable/disable the Import function
                    if ((this._activeDocPanel.Caption.ToString().Contains("Properties") ||
                        this._activeDocPanel.Caption.ToString().Contains("Water")) &&
                        this._appPermissions.CanExport)
                    {
                        this.IsEnabledImport = true;
                    }
                    else
                    {
                        this.IsEnabledImport = false;
                    }

                    // Do we enable/disable the Export function
                    if ((this._activeDocPanel.Caption.ToString().Contains("Properties") ||
                        this._activeDocPanel.Caption.ToString().Contains("Water") ||
                        this._activeDocPanel.Caption.ToString().Contains("Golf") ||
                        this._activeDocPanel.Caption.ToString().Contains("Well") ||
                        this._activeDocPanel.Caption.ToString().Contains("Import")) &&
                        this._appPermissions.CanExport)
                    {
                        this.IsEnabledExport = true;
                    }
                    else
                    {
                        this.IsEnabledExport = false;
                    }

                    // Do we enable/disable the Print function
                    if ((this._activeDocPanel.Caption.ToString().Contains("Properties") ||
                        this._activeDocPanel.Caption.ToString().Contains("Water") ||
                        this._activeDocPanel.Caption.ToString().Contains("Golf") ||
                        this._activeDocPanel.Caption.ToString().Contains("Well") ||
                        this._activeDocPanel.Caption.ToString().Contains("Import")) &&
                        this._appPermissions.CanPrint)
                    {
                        this.IsEnabledPrint = true;
                    }
                    else
                    {
                        this.IsEnabledPrint = false;
                    }

                    ////
                    //// Enable/disable Property specific ribbon buttons
                    ////
                    if (this._activeDocPanel.Caption.ToString().Contains("Properties"))
                    {
                        this.IsPropertyRibbonVisible = true;
                        this.IsWaterRibbonVisible = false;
                    }

                    // Do we enable/disable the Add Relationshiip funcation
                    if (this._activeDocPanel.Caption.ToString().Contains("Properties") &&
                        (null != this.SelectedProperty) &&
                        this._appPermissions.CanAddRelationship)
                    {
                        this.IsEnabledAddRelationship = true;
                    }
                    else
                    {
                        this.IsEnabledAddRelationship = false;
                    }

                    // Do we enable/disable the ChangeOwner funcation
                    if (this._activeDocPanel.Caption.ToString().Contains("Properties") &&
                         this._activeDocPanel.Caption.ToString().Contains("Import") ||
                       (null != this.SelectedProperty) &&
                        this._appPermissions.CanChangeOwner)
                    {
                        this.IsEnabledChangeOwner = true;
                    }
                    else
                    {
                        this.IsEnabledChangeOwner = false;
                    }


                    ////
                    //// Enable/disable Water specific ribbon buttons
                    ////
                    if (this._activeDocPanel.Caption.ToString().Contains("Water"))
                    {
                        this.IsWaterRibbonVisible = true;
                        this.IsPropertyRibbonVisible = false;
                    }
                }
                RaisePropertyChanged("ActiveDocPanel");
            }
        }

        /// <summary>
        /// a grid table view used for exporting/printing 
        /// </summary>
        private TableView _gridTableView = null;
        public TableView GridTableView
        {
            get
            {
                return this._gridTableView;
            }
            set
            {
                if (value != this._gridTableView)
                {
                    this._gridTableView = value;
                }
            }
        }

        /// <summary>
        /// Facility usage collection
        /// </summary>
        private ObservableCollection<FacilityUsage> _usageList = null;
        public ObservableCollection<FacilityUsage> UsagesList
        {
            get
            {
                return this._usageList;
            }
            set
            {
                if (this._usageList != value)
                {
                    this._usageList = value;
                }
            }
        }

        #region Property entities
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
                    this._propertiesList = GetProperties();
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
        /// Register for Property Changes to the ViewModel's SelectedProperty entity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectedProperty_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // We listen for changes to the Balance value of the SelectedProperty.  The 'IsInGoodStading' flag is bound to
            // the value of Balance.  When a balance is owed, we consider the member to not be in good standing.  Therefore
            // we toggle the IsInGoodStanding flag based on Balance value.   Also keep in mind a positive balance means there
            // is a balance owed.
            if (e.PropertyName == "Balance")
            {
                if (this.SelectedProperty.Balance > 0)
                {
                    this.SelectedProperty.IsInGoodStanding = false;
                }
                else
                {
                    this.SelectedProperty.IsInGoodStanding = true;
                }
            }

            if (e.PropertyName == "IsGolf" || e.PropertyName == "IsPool")
            {
                int foo = 0;
                foo++;
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
                return this._selectedProperty;
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

                        // When we change the selected property, we need to null out the Relationships to process collection
                        // so it is clean in case the user performs a change of ownership.
                        this.RelationshipsToProcess = null;  // Used by ChangeOwner
                        this._selectedProperty.PropertyComments = GetPropertyNotes();
                    }

                    // Once a property has been selected, we enable the ChangeOwner ribbon button if appropriate
                    if ((this.ActiveDocPanel.Caption.ToString().Contains("Properties") ||
                        (this.ActiveDocPanel.Caption.ToString().Contains("Import"))) &&
                       this.ApplPermissions.CanChangeOwner)
                    {
                        this.IsEnabledChangeOwner = true;
                    }
                    else
                    {
                        this.IsEnabledChangeOwner = false;
                    }

                    // If appropriate, enable the Add Relationship ribbon button
                    if ((this.ActiveDocPanel.Caption.ToString().Contains("Properties") &&
                        this.ApplPermissions.CanAddRelationship))
                    {
                        this.IsEnabledAddRelationship = true;
                    }
                    else
                    {
                        this.IsEnabledAddRelationship = false;

                    }

                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

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
                    this._selectedRelation = value;
                    //// The database stores the raw binary data of the image.  Before it can be
                    //// displayed in the ImageEdit control, it must be encoded into a BitmapImage
                    if (null == this.SelectedRelation.Photo)
                    {
                        this.SelectedRelation.Photo = this.ApplDefault.Photo; //DefaultBitmapImage; 
                    }
                    RaisePropertyChanged("SelectedRelation");
                }
            }
        }

        /// <summary>
        /// Collection of properties through an ownership change
        /// </summary>
        private ObservableCollection<Property> _propertiesUpdated = null;
        public ObservableCollection<Property> PropertiesUpdated
        {
            get
            {
                if (this._propertiesUpdated == null)
                {
                    this._propertiesUpdated = new ObservableCollection<Property>();
                }
                return this._propertiesUpdated;
            }
            set
            {
                if (this._propertiesList != value)
                {
                    this._propertiesList = value;
                    RaisePropertyChanged("PropertiesUpdated");
                }
            }
        }

        /// <summary>
        /// A collection of relationships to act upon, generlly related to ownership additions & deletetions
        /// </summary>
        private ObservableCollection<Relationship> _relationshipsToProcess = null;
        public ObservableCollection<Relationship> RelationshipsToProcess
        {
            get
            {
                if (this._relationshipsToProcess == null)
                {
                    this._relationshipsToProcess = new ObservableCollection<Relationship>();
                }
                return this._relationshipsToProcess;
            }
            set
            {
                if (this._relationshipsToProcess != value)
                {
                    this._relationshipsToProcess = value;
                    RaisePropertyChanged("RelationshipsToProcess");
                }
            }
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<Relationship> _relationshipsToDelete = null;
        public ObservableCollection<Relationship> RelationshipsToDelete
        {
            get
            {
                if (this._relationshipsToDelete == null)
                {
                    this._relationshipsToDelete = new ObservableCollection<Relationship>();
                }
                return this._relationshipsToDelete;
            }
            set
            {
                if (value != this._relationshipsToDelete)
                {
                    this._relationshipsToDelete = value;
                    RaisePropertyChanged("RelationshipsToDelete");
                }
            }
        }

        /// <summary>
        /// A collection of relationships to delete
        /// </summary>
        private ObservableCollection<WaterMeterException> _meterReadingExceptions = new ObservableCollection<WaterMeterException>();
        public ObservableCollection<WaterMeterException> MeterReadingExceptions
        {
            get
            {
                return _meterReadingExceptions;
            }
            set
            {
                if (value != _meterReadingExceptions)
                {
                    _meterReadingExceptions = value;
                    RaisePropertyChanged("MeterReadingExceptions");
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
        #endregion

        #endregion

        /* ----------------------------------- Boolean Properties ----------------------------------------- */
        #region Boolean Properties
        /// <summary>
        /// Indicates if the client application is connected to the database
        /// </summary>
        public bool IsConnected
        {
            get { return this.TestConnection(); }
        }

        /// <summary>
        /// Controls the visibility of the Property specific ribbon buttons
        /// </summary>
        private bool _isPropertyRibbonVisible = false;
        public bool IsPropertyRibbonVisible
        {
            get { return _isPropertyRibbonVisible; }
            set
            {
                if (value != _isPropertyRibbonVisible)
                {
                    _isPropertyRibbonVisible = value;
                    RaisePropertyChanged("IsPropertyRibbonVisible");
                }
            }
        }

        /// <summary>
        /// Controls the visibility of the Water specific ribbon buttons
        /// </summary>
        private bool _isWaterRibbonVisible = false;
        public bool IsWaterRibbonVisible
        {
            get { return _isWaterRibbonVisible; }
            set
            {
                if (value != _isWaterRibbonVisible)
                {
                    _isWaterRibbonVisible = value;
                    RaisePropertyChanged("IsWaterRibbonVisible");
                }
            }
        }

        /// <summary>
        /// Controls the enable/disable state of the Save ribbon action button
        /// </summary>
        private bool _isEnabledSave = false;  // Default: false
        public bool IsEnabledSave
        {
            get { return _isEnabledSave; }
            set
            {
                if (value != _isEnabledSave)
                {
                    _isEnabledSave = value;
                    RaisePropertyChanged("IsEnabledSave");
                }
            }
        }

        /// <summary>
        /// Controls the enable/disable state of the Import ribbon action button
        /// </summary>
        private bool _isEnabledImport = false;  // Default: false
        public bool IsEnabledImport
        {
            get { return _isEnabledImport; }
            set
            {
                if (value != _isEnabledImport)
                {
                    _isEnabledImport = value;
                    RaisePropertyChanged("IsEnabledImport");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledChangeOwner = false; // Default: false
        public bool IsEnabledChangeOwner
        {
            get { return _isEnabledChangeOwner; }
            set
            {
                if (value != _isEnabledChangeOwner)
                {
                    _isEnabledChangeOwner = value;
                    RaisePropertyChanged("IsEnabledChangeOwner");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledAddRelationship = false; // Default: false
        public bool IsEnabledAddRelationship
        {
            get { return _isEnabledAddRelationship; }
            set
            {
                if (value != _isEnabledAddRelationship)
                {
                    _isEnabledAddRelationship = value;
                    RaisePropertyChanged("IsEnabledAddRelationship");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledExport = true; // Default: true
        public bool IsEnabledExport
        {
            get { return _isEnabledExport; }
            set
            {
                if (value != _isEnabledExport)
                {
                    _isEnabledExport = value;
                    //RaisePropertyChanged("IsEnabledExport");
                }
            }
        }

        /// <summary>
        /// Controls enable/disbale state of the Add Relationship ribbion action button
        /// </summary>
        private bool _isEnabledPrint = true; // Default: true
        public bool IsEnabledPrint
        {
            get { return _isEnabledPrint; }
            set
            {
                if (value != _isEnabledPrint)
                {
                    _isEnabledPrint = value;
                    //RaisePropertyChanged("IsEnabledPrint");
                }
            }
        }

        /// <summary>
        /// Controls wether the ribbon is expanded or minimized
        /// </summary>
        private bool _isRibbonMinimized = false;  // Default: true
        public bool IsRibbonMinimized
        {
            get { return _isRibbonMinimized; }
            set
            {
                if (value != _isRibbonMinimized)
                {
                    _isRibbonMinimized = value;
                    RaisePropertyChanged("IsRibbonMinimized");
                }
            }
        }

        /// <summary>
        /// Indicates the application is busy
        /// </summary>
        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (value != _isBusy)
                {
                    _isBusy = value;
                    RaisePropertyChanged("IsBusy");
                }
            }
        }

        /// <summary>
        /// DataContext IsDirty flag
        /// </summary>
        public bool IsDirty
        {
            get
            {
                // Wrap the getter in a try/catch in case the VM has been disposed when we get here.
                try
                {
                    // Loop through the registered view models to see which, if any, are dirty.
                    foreach (Object o in this.ViewModels)
                    {
                        if (o.GetType().ToString().Contains("PropertiesViewModel"))
                        {
                            ChangeSet changeSet = this.dc.GetChangeSet();
                            if (changeSet.Inserts.Count() > 0 ||
                                changeSet.Updates.Count() > 0 ||
                                changeSet.Deletes.Count() > 0
                                )
                            {
                                this.IsEnabledSave = true;
                                return true;
                            }
                        }
                        else if (typeof(GolfCartViewModel) == o.GetType())
                        {
                            //MessageBox.Show("GolfCartViewModel");
                            GolfCartViewModel gvm = o as GolfCartViewModel;
                            ChangeSet changeSet = gvm.dc.GetChangeSet();
                            if (changeSet.Inserts.Count() > 0 ||
                                changeSet.Updates.Count() > 0 ||
                                changeSet.Deletes.Count() > 0
                                )
                            {
                                this.IsEnabledSave = true;
                                return true;
                            }
                        }
                        else
                        {
                            //MessageBox.Show("Unknown ViewModel");
                        }
                    }

                    this.IsEnabledSave = false;
                    return false;
                }
                catch (Exception ex)
                {
                    // The 'foreach' viewmodel loop will throw "collection modified" on the PropertiesVM.
                    return false;
                }
            }
        }

        #endregion

        /* ----------------------------------- Style Properteis ------------------------------------------- */
        #region Style Properties
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
                if (this.ApplPermissions.CanEditPropertyInfo)
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

        /* ----------------------------- Private Methods --------------------------------------------------- */
        #region Private Methods

        /// <summary>
        ///  Retrieve the current release version of the application
        /// </summary>
        internal static string CurrentVersion
        {
            get
            {
                try
                {
                    return System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed
                           ? System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                           : "<Unpublished>";
                }
                catch (Exception)
                {
                    return "<error>";
                }
            }
        }

        /// <summary>
        /// Tests the connection to the database
        /// </summary>
        /// <returns></returns>
        private bool TestConnection()
        {
            try
            {
                dc.CommandTimeout = 5;
                dc.Connection.Open();
                dc.Connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        /// <summary>
        /// Queries the database to get the current list of property records
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<FacilityUsage> GetFacilitiyUsages()
        {
            try
            {
                //// Get the list of "Properties" from the database
                var list = (from a in this.dc.FacilityUsages
                            select a);

                return new ObservableCollection<FacilityUsage>(list);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Queries the database to get the current list of property records
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<Property> GetProperties()
        {
            try
            {
                //// Get the list of "Properties" from the database
                var list = (from a in this.dc.Properties
                            select a);

                return new ObservableCollection<Property>(list);
            }
            catch (Exception ex)
            {
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

        /// <summary>
        /// Initialize a Relation to default values
        /// </summary>
        /// <param name="relation"></param>
        private void InitializeRelation(Relationship relation)
        {
            relation.Active = false;
            relation.FName = string.Empty;
            relation.LName = string.Empty;
            relation.RelationToOwner = string.Empty;
            relation.PropertyID = this.SelectedProperty.PropertyID;
            relation.RelationshipID = 0;
            relation.Photo = this.ApplDefault.Photo; //DefaultBitmapImage;
            relation.Image = Helper.ArrayToBitmapImage(relation.Photo.ToArray());
        }

        /// <summary>
        /// Ads a Relation to the Model's collection
        /// </summary>
        /// <param name="relationshipViewModel"></param>
        private void AddRelationshipToCollection(RelationshipViewModel relationshipViewModel)
        {
            if (String.IsNullOrEmpty(relationshipViewModel.Relationship.FName) ||
                String.IsNullOrEmpty(relationshipViewModel.Relationship.LName) ||
                String.IsNullOrEmpty(relationshipViewModel.Relationship.RelationToOwner) &&
                false == relationshipViewModel.Relationship.Active)
            {
                relationshipViewModel.IsValid = false;
            }
            else
            {
                relationshipViewModel.IsValid = true;
                this.SelectedProperty.Relationships.Add(relationshipViewModel.Relationship);
                this.RaisePropertyChanged("DataUpdated");
            }
        }

        /// <summary>
        /// Performs no action. Used in conjunction with the "Close" dialog 
        /// </summary>
        private void NoAction()
        { }

        /// <summary>
        /// PropertyEditDialog "Update" event handler
        /// </summary>
        /// <param name="propertyViewModel"></param>
        private void UpdatePropertyAction(PropertyViewModel propertyViewModel)
        {
            // If the user de-activated any Relationships in the dialog, then they need to be accounted
            // for in the DC.
            // (deprecatred 08/09/2017) this.dc.Relationships.DeleteAllOnSubmit(propertyViewModel.RelationshipsToRemove);

            // If the user added new Relationships in the dialog, then they need to be accounted
            // for in the DC. Iterate over the RelationshipsToProperty collection in the Property 
            // ViewModel and test for objects without a relationshipId.  THis effectively copies
            // new relationship records from the PropertyVM to the main PropertiesVM.
            foreach (Relationship r in propertyViewModel.ActiveRelationshipsToProperty)
            {
                if (0 == r.RelationshipID)
                {
                    r.Active = true;
                    this.SelectedProperty.Relationships.Add(r);
                }
            }

            foreach (Relationship r in propertyViewModel.RelationshipsToRemove)
            {
                RemoveRelationship(r);
            }

            // If a new comment was entered, we need to add it to the Note collection of the 
            // SelectedProperty in the main PropertiesVM.
            if (!string.IsNullOrEmpty(this.SelectedProperty.NewComment))
            {
                Note newNote = new Note();
                newNote.PropertyID = this.SelectedProperty.PropertyID;
                newNote.Comment = this.SelectedProperty.NewComment;
                this.SelectedProperty.Notes.Add(newNote);
                this.SelectedProperty.NewComment = string.Empty;

                // Now add it as "Pending" to the comments textblock in the UI
                StringBuilder sb = new StringBuilder();
                sb.Append("(PENDING) ");
                sb.Append(" - ");
                sb.Append(newNote.Comment);
                sb.AppendLine();
                sb.Append(this.SelectedProperty.PropertyComments);
                this.SelectedProperty.PropertyComments = sb.ToString();
            }

            this.SelectedProperty.PoolMembers = 0;
            this.SelectedProperty.PoolGuests = 0;
            this.SelectedProperty.GolfMembers = 0;
            this.SelectedProperty.GolfGuests = 0;

            RaisePropertyChanged("DataUpdated");
        }

        /// <summary>
        /// Reverts all pending changes made to Properties and Relationship
        /// </summary>
        public void CancelPropertyAction()
        {
            bool undo = false;
            ChangeSet changeSet = this.dc.GetChangeSet();

            //// First, check the change set to see if there are pending Updates. If so,
            //// iterate over the Updates collection to see if they are for the currently
            //// selected property.  If found, remove them from the change set.
            if (0 != changeSet.Updates.Count)
            {
                foreach (var v in changeSet.Updates)
                {
                    if (typeof(Property) == v.GetType())
                    {
                        undo = true;
                    }
                    if (typeof(Relationship) == v.GetType())
                    {
                        undo = true;
                    }

                    if (undo)
                    {
                        this.dc.Refresh(RefreshMode.OverwriteCurrentValues, v);
                        undo = false;
                    }
                }

                foreach (var v in changeSet.Inserts)
                {
                    if (typeof(Relationship) == v.GetType())
                    {
                        this.dc.GetTable(v.GetType()).DeleteOnSubmit(v);
                    }

                }
            }

            this.SelectedProperty.PoolMembers = 0;
            this.SelectedProperty.PoolGuests = 0;
            this.SelectedProperty.GolfMembers = 0;
            this.SelectedProperty.GolfGuests = 0;

            //// Lastly, just in case the user entered data into the New Commment field, we
            //// blank out the string.
            this.SelectedProperty.NewComment = string.Empty;
        }
        private void CancelPropertyAction(PropertyViewModel property)
        {
            bool undo = false;
            ChangeSet changeSet = this.dc.GetChangeSet();

            //// First, check the change set to see if there are pending Updates. If so,
            //// iterate over the Updates collection to see if they are for the currently
            //// selected property.  If found, remove them from the change set.
            if (0 != changeSet.Updates.Count)
            {
                foreach (var v in changeSet.Updates)
                {
                    if (typeof(Property) == v.GetType())
                    {
                        Property p = v as Property;
                        if (this.SelectedProperty.PropertyID == p.PropertyID)
                        {
                            undo = true;
                        }
                    }
                    if (typeof(Relationship) == v.GetType())
                    {
                        Relationship r = v as Relationship;
                        if (this.SelectedProperty.PropertyID == r.PropertyID)
                        {
                            undo = true;
                        }
                    }

                    if (undo)
                    {
                        this.dc.Refresh(RefreshMode.OverwriteCurrentValues, v);
                        undo = false;
                    }
                }
            }

            this.SelectedProperty.PoolMembers = 0;
            this.SelectedProperty.PoolGuests = 0;
            this.SelectedProperty.GolfMembers = 0;
            this.SelectedProperty.GolfGuests = 0;

            //// Lastly, just in case the user entered data into the New Commment field, we
            //// blank out the string.
            this.SelectedProperty.NewComment = string.Empty;
        }

        /// <summary>
        /// Adds new water meter readings to collection
        /// </summary>
        /// <param name="relationshipViewModel"></param>
        private void AddWaterMeetingReadingToCollection(WaterMeterViewModel waterSystemViewModel)
        {
            // Since the WaterMeterReading VM is virtual, we copy it back to the main (Properties) VM.
            // This updates, or copies over, changes a user has made to VM properties of the SelectedProperty
            this.SelectedProperty = waterSystemViewModel.SelectedProperty;

            // However, the MeterReadings are stored in a separate collection, so we have to iterate
            // over them and add new items to the PropertiesVM in order for them to be registered
            // in the DC's changeset.
            foreach (WaterMeterReading mr in waterSystemViewModel.MeterReadings)
            {
                if (0 == mr.RowID)
                {
                    this.SelectedProperty.WaterMeterReadings.Add(mr);
                    RaisePropertyChanged("WaterDataUpdated");
                }
            }
        }

        /// <summary>
        /// Reverts changes made to the water meter reading control is there is an update pending.
        /// </summary>
        /// <param name="relationshipViewModel"></param>
        private void CancelWaterSystemAction(WaterMeterViewModel waterSystemViewModel)
        {
            bool undo = false;
            ChangeSet changeSet = this.dc.GetChangeSet();
            foreach (var v in changeSet.Updates)
            {
                if (typeof(Property) == v.GetType())
                {
                    Property p = v as Property;
                    if (this.SelectedProperty.PropertyID == p.PropertyID)
                    {
                        undo = true;
                    }
                }
                if (typeof(WaterMeterReading) == v.GetType())
                {
                    WaterMeterReading w = v as WaterMeterReading;
                    if (this.SelectedProperty.PropertyID == w.PropertyID)
                    {
                        undo = true;
                    }
                }

                if (undo)
                {
                    this.dc.Refresh(RefreshMode.OverwriteCurrentValues, v);
                    undo = false;
                }
            }
        }

        /// <summary>
        /// Change ownership of property, called from the Save command
        /// </summary>
        private void ExecuteOwnershipChanges()
        {
            //// The Ownership changes are bound to two different properties, RelationshipsToProcess and RelationshipsToDelete.
            //// RelationshipToProcess is the full collection of Relationships to be acted on (added or removed), while
            //// RelationshipsToDelete (the selected items) are a sub-set.  This is required by the grid in order to show
            //// the full collection and the items selected. This way, the two collections (deletions/additions) are managed separately. 
            try
            {
                //// First, iterate over the RelationshipToDelete collection and add them to the data context's pending
                //// deletions collection so they will be removed on Save().  Then remove them from the RelationshipToProcess
                //// collection.  This will result in the collection just containing items that must be added to the 
                //// data context's Insert collection on Save().  Lastly, use the common IsDirty flag to indicate the
                //// data context is dirty, which will enable the Save() command on the Main ribbon.
                foreach (Relationship r in this.RelationshipsToDelete)
                {
                    // v1.3.1.x Relationships can no longer be deleted if there are FacilitiesUsage records
                    // associated to the RelationshipID due to the FK restraint. Rather, they 
                    // are now de-activated.
                    // (deprecated) this.RemoveRelationship(r);
                    RemoveRelationship(r);
                }


                foreach (Relationship r in this.RelationshipsToProcess)
                {
                    if (0 == r.RelationshipID &&
                        !this.RelationshipsToDelete.Contains(r))
                    {
                        this.AddRelationship(r);
                    }
                }

                Property foundProperty = (from x in PropertiesUpdated
                                          where (x.Section == SelectedProperty.Section &&
                                                 x.Block == SelectedProperty.Block &&
                                                 x.Lot == SelectedProperty.Lot &&
                                                 x.SubLot == SelectedProperty.SubLot)
                                          select x).FirstOrDefault();
                //
                // If we have come by way of an Import, then the collection 'PropertiesUpdated' needs to be updated.
                // The Import results grid is bound to 'PropertiesUpdates' collection, so once we remove the relationships for
                // the curented (selected) property, we remove that reference from the collection; effectively popping
                // it off the list.
                if (null != foundProperty)
                {
                    PropertiesUpdated.Remove(foundProperty);
                }

                // Lastly, null out the two collections so old data doesn't linger.
                this.RelationshipsToProcess = null;
                this.RelationshipsToDelete = null;
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error changing owner:" + ex.Message);
            }
        }

        #endregion

        /* ------------------------------------ Public Methods -------------------------------------------- */
        #region Public Methods

        /// <summary>
        /// Returns true/false if the current user is in the database role being tested.
        /// </summary>
        /// <param name="databaseRole"></param>
        /// <returns></returns>
        public bool IsMember(DatabaseRoleInfo databaseRole)
        {
            using (HVCCDataContext db = new HVCCDataContext())
            {
                return true == db.fn_IsMember(databaseRole.Role);
            }
        }

        /// <summary>
        /// Populates the RelationshipToRemove collection of the selected property
        /// </summary>
        public void PopulateRelationshipsToProcess()
        {
            if (0 == this.RelationshipsToProcess.Count())
            {
                // Add the current active relationships to the collection to be removed/replaced.  The current
                // owner records need to exist in both the toProcess and toDelete. The toProcess
                // collection is bound to the grid, which the toDelete collection is bound to the 
                // selectedItems collection of the grid.
                foreach (Relationship r in this.SelectedProperty.Relationships)
                {
                    if (true == r.Active)
                    {
                        this.RelationshipsToProcess.Add(r);
                        this.RelationshipsToDelete.Add(r);  // collection of items that are selected in the grid
                    }
                }
                // Extract the new owner name(s) from the new "BillTo" string
                List<Relationship> owners = Helper.ExtractOwner(this.SelectedProperty);
                foreach (Relationship ro in owners)
                {
                    // Check to see if the ownership is present in the current relationship collection.
                    // If it is, then we don't want to add it to the list of relationships being added.
                    // This condition accounts for when the user performs a change of ownership that is
                    // associcated with an import.
                    Relationship rx = (from x in this.SelectedProperty.Relationships
                                       where x.FName == ro.FName
                                       && x.LName == ro.LName
                                       select x).FirstOrDefault();
                    if (null == rx)
                    {
                        this.RelationshipsToProcess.Add(ro);
                    }
                }
            }
            this.IsEnabledSave = true;
            RaisePropertyChanged("DataChanged");
        }

        /// <summary>
        /// Removes a relationship from the selected property either by making it inactive or deleting it 
        /// from the Relationships table.
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public bool RemoveRelationship(Relationship relationship) // TO-DO: <?> Convert to a private method
        {
            try
            {
                // Check to see if this Relationship is in the database (has a non-zero ID), 
                // or is pending insertion (has a zero ID).
                ChangeSet changeSet = dc.GetChangeSet();  // <I> This is only for debugging.......
                if (0 != relationship.RelationshipID)
                {

                    // Test to see if the relationship being removed has any FacilitiesUage records.
                    // If not, we can delete the relationship record. Otherwise, we have to set
                    // the records to inactive.
                    FacilityUsage chkR = (from x in this.dc.FacilityUsages
                                          where x.RelationshipId == relationship.RelationshipID
                                          select x).FirstOrDefault();

                    if (null == chkR)
                    {
                        this.dc.Relationships.DeleteOnSubmit(relationship);
                    }
                    else
                    {
                        //Relationship a = (from y in this.SelectedProperty.Relationships
                        //                  where y.RelationshipID == relationship.RelationshipID
                        //                  select y).FirstOrDefault();
                        relationship.Active = false;
                    }
                }
                else
                {
                    // This is a pending insert, so we can simply remove the record from the in-memory
                    // store.  We raise a PropertiesList property change event to force any/all bound
                    // views to be updated.
                    this.SelectedProperty.Relationships.Remove(relationship);
                    RaisePropertyChanged("DataUpdated");
                }
                //}
                //this.dc.Log = System.Console.Out;
                //ChangeSet cs = dc.GetChangeSet();  // <I> This is only for debugging.......
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Adds a relationship for the selected property
        /// </summary>
        /// <param name="relationship"></param>
        /// <returns></returns>
        public bool AddRelationship(Relationship relationship) //  TO-DO: <?> Convert to a private method
        {
            try
            {
                // Check to see if this Relationship is in the database (has a non-zero ID), 
                // or is pending insertion (has a zero ID).  We only want to process new
                // relationships.
                ChangeSet changeSet = dc.GetChangeSet();  // <I> This is only for debugging.......
                if (0 == relationship.RelationshipID)
                {
                    relationship.Active = true;
                    // Add the default HVCC image to the relationship record.  
                    relationship.Photo = this.ApplDefault.Photo;
                    // Add this relationship to the pending database changes. Actual update isn't
                    // immeidate and is dependent on user clicking the 'save' button.
                    this.dc.Relationships.InsertOnSubmit(relationship);
                    // Because of the way I implemented the Relationship grid in the edit dialog,
                    // the new relationship also needs to be manually added to the VM collection of 
                    // the selected property.  [It is not automaticly added to the collection via the datacontext
                    // even after the new Relationship is added to the database.]
                    this.SelectedProperty.Relationships.Add(relationship);
                }
                else { }
                RaisePropertyChanged("DataUpdated");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Reverts changes made to the Relationship collection of the current selected Property
        /// </summary>
        public void RevertRelationshipActions()
        {
            this.RelationshipsToDelete = null;
            this.RelationshipsToProcess = null;
        }

        /// <summary>
        /// Gets a ViewModel by its type name
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns></returns>
        public Object GetViewModelByName(string vmName)
        {
            foreach (Object o in this.ViewModels)
            {
                if (vmName == o.GetType().ToString().Trim())
                {
                    return o;
                }
            }
            return null;
        }

        #endregion

        /* -------------------------------------- Command Bindings ---------------------------------------- */
        #region Command Bindings

        /// <summary>
        /// 
        /// </summary>
        public void Save()  //SaveCommand
        {
            // Loop through the registered view models to see which, if any, are dirty.
            foreach (Object o in this.ViewModels)
            {
                if (o.GetType().ToString().Contains("PropertiesViewModel"))
                {
                    try
                    {
                        this.IsBusy = true;

                        // The ViewModel's data context manages most, but not all, of the changes to the model.  The
                        // exception is related to an ownership change.  Ownership relation records are stored in two 
                        // different collections, those to be removed and those to be added.  The removals are stored
                        // in the VM's 'RelationshipsToRemove' collection which is not associated with the model, rather
                        // it is associated to the data grid's SelectedItems collection.  Whereas, the new owenership
                        // relationship records are added to the SelectedProperty.Relationships collection (which is
                        // in the model).
                        // Therefore, before we can process pending changes to the DC, we must first process the
                        // two collections so they become part of the DC's change set.
                        if (this.ActiveDocPanel.Caption.ToString().Contains("Change Ownership"))
                        {
                            this.ExecuteOwnershipChanges();
                        }

                        ChangeSet cs = dc.GetChangeSet();  // <Info> This is only for debugging.......
                        this.dc.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        MessageBoxService.Show("Error Saving data:" + ex.Message);
                        return;
                    }
                    finally
                    {
                        // In case the user added a note, and we are not importing data.... We have to reread the notes collection [into the DC] in order for it to be reflected
                        // in the PropertyEditDialog window.
                        if (true == this.ApplPermissions.CanViewPropertyNotes && (!this.ActiveDocPanel.Caption.ToString().Contains("Import")))
                        {
                            this.dc.Refresh(RefreshMode.OverwriteCurrentValues, this.SelectedProperty.Notes);
                            this.SelectedProperty.PropertyComments = GetPropertyNotes();
                        }

                        this.IsBusy = false;
                        //this.IsRibbonMinimized = true;
                        this.IsEnabledSave = false;
                        // Raise a DataUpdated property change to Main() so the ChangeOwner doc panel is closed after save.
                        RaisePropertyChanged("DataUpdated");
                        MessageBoxService.Show("Changes successfully saved!");
                    }
                }
                else if (typeof(GolfCartViewModel) == o.GetType())
                {
                    //MessageBox.Show("GolfCartViewModel");
                    try
                    {
                        GolfCartViewModel gvm = o as GolfCartViewModel;
                        ChangeSet cs = gvm.dc.GetChangeSet();  // <Info> This is only for debugging.......
                        gvm.dc.SubmitChanges();
                    }
                    catch (Exception ex)
                    {
                        MessageBoxService.Show("Error Saving data:" + ex.Message);
                        return;
                    }
                    finally
                    {
                    }
                }
                else
                {
                    //MessageBox.Show("Unknown ViewModel");
                }
            }
        }

        /// <summary>
        /// Change Ownership Cancel Button command
        /// </summary>
        public void CancelChangeOwner() // CancelChangeOwnerCommand
        {
            // Close the Change Owership document panel by removing it from the DocGroup
            this.DocGroup.Remove(this.ActiveDocPanel);
        }

        /// <summary>
        /// Refresh data context..... (NOT IMPLEMENTED)
        /// </summary>
        public void Refresh() //RefreshCommand
        {
            try
            {
                // TO-DO: I'm told this doesn't do anything......?
                //this.dc.Refresh(RefreshMode.KeepChanges, this.PropertiesList);
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error refreshing data:" + ex.Message);
            }
            finally
            {
                //this.IsRibbonMinimized = true;
            }
        }

        /// <summary>
        /// Undo database changes.....  (NOT IMPLEMENTED)
        /// </summary>
        public void Undo() //UndoCommand
        {
            try
            {
                // TO-DO:  Add logic to fetch data from the database to replace changes to the model in memory
                this.dc.Log = System.Console.Out;
                this.dc.Refresh(RefreshMode.OverwriteCurrentValues, this.PropertiesList);
                ChangeSet cs = dc.GetChangeSet();  // <I> This is only for debugging.......
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error undoing data:" + ex.Message);
            }
            finally
            {
                //this.IsRibbonMinimized = true;
            }
        }

        /// <summary>
        /// Import Property information from Quickbooks spreadsheet
        /// </summary>
        public void Import() // ImportCommand
        {
            int[] colArray = new int[3];
            // Open a file chooser dialog window. Capture the user's file selection.
            OpenFileDialogService.Filter = "XLSX files|*.xlsx";
            OpenFileDialogService.FilterIndex = 1;
            DialogResult = OpenFileDialogService.ShowDialog();
            if (!DialogResult)
            {
                ResultFileName = string.Empty;
            }
            else
            {
                IFileInfo file = OpenFileDialogService.Files.First();
                ResultFileName = file.GetFullName();
            }

            // Set the busy flag so the cursor in the UI will spin to indicate something is happening.
            this.IsBusy = true;
            List<Relationship> OwnerList = new List<Relationship>();

            // Process the excel sprea-sheet to import and update the property records
            try
            {
                SpreadsheetControl spreadsheetControl1 = new SpreadsheetControl();
                IWorkbook workbook = spreadsheetControl1.Document;
                // Load a workbook from a stream. 
                using (FileStream stream = new FileStream(ResultFileName, FileMode.Open))
                {
                    workbook.LoadDocument(stream, DocumentFormat.OpenXml);
                    Worksheet sheet = workbook.Worksheets[1];
                    RowCollection rowCollection = sheet.Rows;
                    int rowCount = rowCollection.LastUsedIndex;

                    for (int row = 0; row <= rowCount; row++)
                    {
                        RowNum = row + 1; // need to account for the zero offset in the spread-sheet
                        Row currentRow = rowCollection[row];
                        Range cellRange = currentRow.GetRangeWithAbsoluteReference();

                        // Row[0] is the header row. We read the header to determin what the offsets
                        // are for the Customer, Bill To and Balance columns. This way, if there are
                        // more columns included in the import file it can handle the file format.
                        string cellData = String.Empty;
                        if (0 == row)
                        {
                            for (int cell = 0; cell <= 25; cell++)
                            {
                                cellData = cellRange[cell].Value.ToString();
                                switch (cellData)
                                {
                                    case "Customer":
                                        Visibility v = Visibility.Hidden;
                                        colArray[(int)Column.Customer] = cell;
                                        break;
                                    case "Bill to":
                                        colArray[(int)Column.BillTo] = cell;
                                        break;
                                    case "Balance Total":
                                        colArray[(int)Column.Balance] = cell;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            if (0 == colArray[(int)Column.Customer] ||
                                 0 == colArray[(int)Column.BillTo] ||
                                 0 == colArray[(int)Column.Balance])
                            {
                                throw new System.FormatException("Import file is invalid or missing required columns");
                            }
                        }
                        else
                        {
                            // Get the 'Customer' data from the cell.  Using 'ConvertCustomerToProperty()', we can
                            // divide up the string into section-block-lot-sublot so it populates the 'importProperty'
                            // 
                            string customer = cellRange[colArray[(int)Column.Customer]].Value.ToString();
                            Property importProperty = Helper.ConvertCustomerToProperty(customer);
                            importProperty.Customer = customer;
                            importProperty.BillTo = cellRange[colArray[(int)Column.BillTo]].Value.ToString();
                            importProperty.Balance = Decimal.Parse(cellRange[colArray[(int)Column.Balance]].Value.ToString());

                            // Look up PropertyID. If it is not-null then update the property record with the new value(s).
                            // Otherwise Insert it as a new property record.
                            Property foundProperty = this.dc.Properties.Where(x => x.Section == importProperty.Section &&
                                                                                x.Block == importProperty.Block &&
                                                                                x.Lot == importProperty.Lot &&
                                                                                x.SubLot == importProperty.SubLot).SingleOrDefault();

                            // In theroy, we should never find a property that isn't already in the database.
                            if (null == foundProperty)
                            {
                                MessageBoxService.ShowMessage("Warnning: A new property is about to be added " + importProperty.Customer);
                                // TO-DO:  Add handeling of user input (messagebox result)

                                this.PropertiesUpdated.Add(importProperty);
                                //this.dc.Properties.InsertOnSubmit(importProperty);
                            }
                            else // update existing record with new/changed value(s)
                            {
                                /* -----
                                     // REVISION: BillTo is no longer updated by the Import function as of (v1.2.1.10)
                                     // Staff now will update the BillTo field in the ChangeOwner EditDialog.
                                     // (andy t. 6/9/2017)

                                                                // If a property's 'BillTo' has changed from what is in the database it indicates
                                                                // a possible/likely change in ownership. We add the changed property to the collection
                                                                // of updated properties so it populates the grid.
                                                                if (foundProperty.BillTo.TrimEnd() != importProperty.BillTo.TrimEnd())
                                                                {
                                                                    // Assign the new (updated) values to the foundProperty with the values from the import
                                                                    // spread-sheet.
                                                                    //foundProperty.Customer = importProperty.Customer;  // the customer string will not change, so no reason to assign at here?
                                                                    foundProperty.Balance = importProperty.Balance;
                                                                    //foundProperty.BillTo = importProperty.BillTo;

                                                                    // Extract the new owner name(s) from the new "BillTo" string
                                                                    List<Relationship> owners = Helper.ExtractOwner(foundProperty);

                                                                    // IF the owner count returns 0, it indicates the owner/bill to information from the import file
                                                                    // is out of date with respect to the database.
                                                                    if (0 > owners.Count)
                                                                    { 
                                                                        List<string> fNames = new List<string>();
                                                                        List<string> lNames = new List<string>();
                                                                        foreach (Relationship r in owners)
                                                                        {
                                                                            fNames.Add(r.FName);
                                                                            lNames.Add(r.LName);
                                                                        }
                                                                        List<string> firstNames = (from f in fNames
                                                                                                   select f).Distinct().ToList();
                                                                        foundProperty.OwnerFName = Helper.UniqueNames(firstNames);
                                                                        List<string> lastNames = (from l in lNames
                                                                                                  select l).Distinct().ToList();
                                                                        foundProperty.OwnerLName = Helper.UniqueNames(lastNames);

                                                                        // Add the updated property to the PropertiesUpdated collection, which is bound to the
                                                                        // import grid's data source.
                                                                        this.PropertiesUpdated.Add(foundProperty);
                                                                    }
                                                                }
                                --- */
                                // Check to see if the Balance amount needs to be updated
                                if (foundProperty.Balance != importProperty.Balance)
                                {
                                    // Assign the new (updated) value to the foundProperty with the values from the import
                                    // spread-sheet.
                                    foundProperty.Balance = importProperty.Balance;
                                    if (foundProperty.Balance > 0)
                                    {
                                        foundProperty.IsInGoodStanding = false;
                                        foundProperty.Status = "Past Due";
                                    }
                                    else
                                    {
                                        foundProperty.IsInGoodStanding = true;
                                        foundProperty.Status = String.Empty;
                                    }
                                }
                            }
                        }
                    }

                    // Get the change set for the inport.
                    ChangeSet cs = this.dc.GetChangeSet();

                    // The Import Results grid is bound to the 'PropertiesUpdated' collection. Since the 
                    // collection is used in other places, we need to clear the collection before adding
                    // the change set items.
                    if (0 > this.PropertiesUpdated.Count)
                    {
                        foreach (Property x in this.PropertiesUpdated)
                        {
                            this.PropertiesUpdated.Remove(x);
                        }
                    }

                    // Add the change set items to the 'PropertiesUpdated' collection is it is reflected
                    // in the Import Results grid.
                    foreach (Property p in cs.Updates)
                    {
                        this.PropertiesUpdated.Add(p);
                    }

                    // Have the user respond to the changes
                    MessageResult userInput = MessageBoxService.ShowMessage("Import complete. Do you want to save the results?", "Save Changes", MessageButton.YesNo, MessageIcon.Question, MessageResult.No);
                    if (MessageResult.Yes == userInput)
                    {
                        this.Save();
                        MessageBoxService.ShowMessage("Changes saved.", "", MessageButton.OK, MessageIcon.Information);
                    }
                    else
                    {
                        foreach (var v in cs.Updates)
                        {
                            if (typeof(Property) == v.GetType())
                            {
                                this.dc.Refresh(RefreshMode.OverwriteCurrentValues, v);
                            }
                        }
                    }

                    workbook.Dispose();

                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //// NOTE: This needs only be executed on the very first import to seed the database.........
                    //foreach (Property p in this.PropertiesList)
                    //{
                    //    //// Try to extract the owner name(s) from the BillTo string
                    //    AddToOwners = Helper.ExtractOwner(p);
                    //    foreach (Relationship r in AddToOwners)
                    //    {
                    //        this.dc.Relationships.InsertOnSubmit(r);
                    //    }
                    //}
                    //this.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error importing data at row " + RowNum + " Message: " + ex.Message);
            }
            finally
            {
                //this.IsRibbonMinimized = true;
                this.IsBusy = false;
            }

        }

        /// <summary>
        /// Import water meter readings from CipherLab terminal import file
        /// </summary>
        public void ImportMeterReading()
        {
            if (this.IsDirty)
            {
                MessageBoxService.ShowMessage("Import is not allowed when there are pending changes.", "Warning", MessageButton.OK, MessageIcon.Hand);
                return;
            }

            int[] colArray = new int[3];
            // Open a file chooser dialog window. Capture the user's file selection.
            OpenFileDialogService.Filter = "CSV files|*.csv";
            OpenFileDialogService.FilterIndex = 1;
            DialogResult = OpenFileDialogService.ShowDialog();
            if (!DialogResult)
            {
                ResultFileName = string.Empty;
            }
            else
            {
                IFileInfo file = OpenFileDialogService.Files.First();
                ResultFileName = file.GetFullName();
            }

            // Set the busy flag so the cursor in the UI will spin to indicate something is happening.
            this.IsBusy = true;
            if (File.Exists(ResultFileName))
            {
                try
                {
                    string rawData = String.Empty;

                    using (StreamReader sr = new StreamReader(ResultFileName))
                    {
                        // Interate through the CSV file one line at a time.  The raw string will be parsed into an array
                        // of substrings to represent the columns: <date>,<time>,<lot/customer>,<meter reading>.
                        while (sr.Peek() >= 0)
                        {
                            WaterMeterReading reading = new WaterMeterReading();

                            // RawData format: 09/20/2017,17:27:01,06-02-021,12345
                            rawData = sr.ReadLine();
                            String[] subStrings = rawData.Split(',');

                            // Concatinate the date and time into a single string, then convert it to
                            // a DateTile type.
                            string dt = String.Format("{0} {1}", subStrings[0], subStrings[1]);
                            reading.ReadingDate = DateTime.ParseExact(dt, "MM/dd/yyyy HH:mm:ss", null);

                            // Using Linq2Obj, query the Properties collection using the 'customer' string to get
                            // the unique PropertyID
                            // NOTE: Because 3of9 barcode does not support the <sp> character, I have encoded
                            //       <sp> as the '/' character in the lot barcodes. Therefore, we have to replace
                            //       the '/' with a <sp>
                            string customer = subStrings[2].Replace('/', ' ');

                            // Query the property list to get the PropertyID for the customer string
                            var pID = (from c in this.PropertiesList
                                       where customer == c.Customer
                                       select c).FirstOrDefault();

                            // If we can't match up the customer string scanned from the bar code book, we have
                            // an issue.....
                            if (null == pID)
                            {
                                reading.PropertyID = 0;
                            }
                            else
                            {
                                reading.PropertyID = pID.PropertyID;
                            }

                            // Assign the current meter reading....
                            reading.MeterReading = Int32.Parse(subStrings[3]);

                            // Get the last meter reading so the current consumption can be calculated.
                            var lmr = (from m in this.dc.WaterMeterReadings
                                       where m.PropertyID == pID.PropertyID
                                       orderby m.ReadingDate descending
                                       select m).FirstOrDefault();

                            if (null == lmr)
                            {
                                reading.Consumption = 0;
                            }
                            else
                            {
                                // Calculate the consumption between this reading and the last reading
                                reading.Consumption = reading.MeterReading - lmr.MeterReading;

                                // Check to make sure the last meter reading date is not the current date, or a date
                                // greater than the current reading date.
                                if (reading.ReadingDate <= lmr.ReadingDate)
                                {
                                    //MessageBoxService.ShowMessage("The meter read date is the same or older than the last meter read date.\nThe import will be terminated", "ERROR", MessageButton.OK, MessageIcon.Error);
                                    //return;
                                    this.MeterReadingExceptions.Add(new WaterMeterException()
                                    {
                                        Customer = customer,
                                        CurrentMeterReadingDate = reading.ReadingDate,
                                        LastMeterReadingDate = lmr.ReadingDate,
                                        CurrentMeterReading = reading.MeterReading,
                                        LastMeterReading = null
                                    });
                                }

                                // Do a bit of checking to make sure the delta value isn't wonky...
                                else if (0 > reading.Consumption ||
                                        1000 <= reading.Consumption)
                                {
                                    this.MeterReadingExceptions.Add(new WaterMeterException()
                                    {
                                        Customer = customer,
                                        CurrentMeterReadingDate = reading.ReadingDate,
                                        LastMeterReadingDate = null, //lmr.ReadingDate,
                                        CurrentMeterReading = reading.MeterReading,
                                        LastMeterReading = lmr.MeterReading
                                    });
                                }
                                else
                                {
                                    // Add this reading to pending inserts......
                                    this.dc.WaterMeterReadings.InsertOnSubmit(reading);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxService.ShowMessage("An error occured with the import", "ERROR", MessageButton.OK, MessageIcon.Error);
                    return;
                }
            }
            ChangeSet cs = dc.GetChangeSet();
            dc.SubmitChanges();
            this.IsBusy = false;
            //string message = String.Format("Import complete. {0} records imported", cs.Inserts.Count);
            //MessageBoxService.ShowMessage(message, "Information", MessageButton.OK, MessageIcon.Information);
            RaisePropertyChanged("MeterExceptions");
        }

        /// <summary>
        /// Exports data grid to Excel
        /// </summary>
        /// <param name="type"></param>
        public void Export(ExportType type) //ExportCommand
        {
            try
            {
                switch (type)
                {
                    case ExportType.PDF:
                        SaveFileDialogService.Filter = "PDF files|*.pdf";
                        if (SaveFileDialogService.ShowDialog())
                            ExportService.ExportToPDF(this.GridTableView, SaveFileDialogService.GetFullFileName());
                        break;
                    case ExportType.XLSX:
                        SaveFileDialogService.Filter = "Excel 2007 files|*.xlsx";
                        if (SaveFileDialogService.ShowDialog())
                            ExportService.ExportToXLSX(this.GridTableView, SaveFileDialogService.GetFullFileName());
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error exporting data:" + ex.Message);
            }
            finally
            {
                //this.IsRibbonMinimized = true;
            }
        }

        /// <summary>
        /// Prints the current document
        /// </summary>
        /// <param name="type"></param>
        public void Print(PrintType type) //PrintCommand
        {
            try
            {
                switch (type)
                {
                    case PrintType.PREVIEW:
                        ExportService.ShowPrintPreview(this.GridTableView);
                        break;
                    case PrintType.PRINT:
                        ExportService.Print(this.GridTableView);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.Show("Error printing data:" + ex.Message);
            }
            finally
            {
                //this.IsRibbonMinimized = true;
            }
        }

        public void About()  // AboutCommand
        {
            System.Windows.MessageBox.Show(string.Format("HVCC.exe\nversion {0}\n\nWritten By: Andy Tudhope\nHVCCTechHelp@gmail.com\n(c)2017\n\nUser Role: {1}", CurrentVersion, this.DBRole.ToString()), "About",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        #endregion

        /* ----------------------------------- Show Dialog Commands ------------------------------------ */
        #region Show Dialog Commands
        /// <summary>
        /// This command fires off the Edit WaterSystem Dialog input form
        /// </summary>
        public UICommand ShowPropertyDialog(Property selectedProperty)
        {
            PropertyViewModel propertyViewModel = null;

            if (propertyViewModel == null)
            {
                // Create a new PropertyViewModel and copy over, or set, the property values from the PropertiesViewModel
                propertyViewModel = new PropertyViewModel();
                propertyViewModel.SelectedProperty = this.SelectedProperty;
                //propertyViewModel.ActiveRelationshipsToProperty = this.ActiveRelationshipsToProperty;
                propertyViewModel.NoteCount = this.NoteCount;
                propertyViewModel.ApplPermissions = this.ApplPermissions;
                propertyViewModel.ApplDefault = this.ApplDefault;
                if (String.IsNullOrEmpty(this.SelectedProperty.PhysicalAddress))
                {
                    propertyViewModel.CanViewMap = false;
                }
                else
                {
                    propertyViewModel.CanViewMap = true;
                }
                if (String.IsNullOrEmpty(this.SelectedProperty.Parcel))
                {
                    propertyViewModel.CanViewParcel = false;
                }
                else
                {
                    propertyViewModel.CanViewParcel = true;
                }
            }

            if (this.ApplPermissions.CanEditPropertyInfo || this.ApplPermissions.CanUpdateRelationship || this.ApplPermissions.CanEditPropertyNotes)
            {
                UICommand updateCommand = new UICommand()
                {
                    Caption = "Update",
                    IsCancel = false,
                    IsDefault = false,
                    Command = new DelegateCommand<CancelEventArgs>(
                        x => UpdatePropertyAction(propertyViewModel)
                        ),
                };
                UICommand cancelCommand = new UICommand()
                {
                    Id = MessageBoxResult.Cancel,
                    Caption = "Cancel",
                    IsCancel = true,
                    IsDefault = true,
                    Command = new DelegateCommand<CancelEventArgs>(
                       x => CancelPropertyAction(propertyViewModel)
                       ),
                };
                UICommand result = PropertyEditDialogService.ShowDialog(
                    dialogCommands: new List<UICommand>() { updateCommand, cancelCommand },
                    title: "Edit Property Details",
                    viewModel: propertyViewModel);
                return result;
            }
            else
            {
                UICommand closeCommand = new UICommand()
                {
                    Id = MessageBoxResult.Cancel,
                    Caption = "Close",
                    IsCancel = true,
                    IsDefault = true,
                    Command = new DelegateCommand<CancelEventArgs>(
                        x => NoAction()
                        ),
                };
                UICommand result = PropertyEditDialogService.ShowDialog(
                    dialogCommands: new List<UICommand>() { closeCommand },
                    title: "Property Details",
                    viewModel: propertyViewModel);
                return result;
            }
        }

        #region DEPRECATED 8/9/2017
        /// <summary>
        /// This command fires off the Add Relationship Dialog input form
        /// </summary>
        //public MessageBoxResult ShowRelationshipDialog(Relationship relationship, DialogType type)
        //{
        //    try
        //    {
        //        // Set up the RelationshipView Model for the Dialog by creating (if necessary) an instance of the 
        //        // VM, then assigning the Relationship to it.  Since we don't have scope to the SelectedProperty
        //        // associated to the relationship, we copy the information we need into a string property of the 
        //        // RelationshipViewModel.
        //        RelationshipViewModel relationshipViewModel = null;
        //        relationshipViewModel = new RelationshipViewModel();
        //        if (null == relationship)
        //        {
        //            relationshipViewModel.Relationship = new Relationship();
        //            this.InitializeRelation(relationshipViewModel.Relationship);
        //        }
        //        else
        //        {
        //            relationshipViewModel.Relationship = relationship;
        //        }
        //        string lotInfo = "Lot: " + this.SelectedProperty.Section + "-" +
        //            this.SelectedProperty.Block + "-" +
        //            this.SelectedProperty.Lot + "-" +
        //            this.SelectedProperty.SubLot +
        //            "\t\t Owner: " + this.SelectedProperty.OwnerLName + ", " + this.SelectedProperty.OwnerFName;
        //        relationshipViewModel.LotInformation = lotInfo;

        //        relationshipViewModel.CanAddRelationship = this.ApplPermissions.CanAddRelationship;

        //        // Define the "actions" the dialog can perform.  These are the action buttons at the bottom
        //        // of the dialog:  Add, Update, Close, & Cancel.
        //        UICommand addCommand = new UICommand()
        //        {
        //            Id = MessageBoxResult.None,
        //            Caption = "Add",
        //            IsCancel = false,
        //            IsDefault = false,
        //            Command = new DelegateCommand<CancelEventArgs>(
        //                    x => AddRelationshipToCollection(relationshipViewModel)
        //                    ),
        //        };
        //        UICommand updateCommand = new UICommand()
        //        {
        //            Id = MessageBoxResult.Yes,
        //            Caption = "Update",
        //            IsCancel = false,
        //            IsDefault = true,
        //            Command = new DelegateCommand<CancelEventArgs>(
        //                    x => AddRelationshipToCollection(relationshipViewModel)
        //                    ),
        //        };
        //        UICommand closeCommand = new UICommand()
        //        {
        //            Id = MessageBoxResult.Yes,
        //            Caption = "Close",
        //            IsCancel = false,
        //            IsDefault = true,
        //            Command = new DelegateCommand<CancelEventArgs>(
        //                    x => NoAction()
        //                    ),
        //        };
        //        UICommand cancelCommand = new UICommand()
        //        {
        //            Id = MessageBoxResult.Cancel,
        //            Caption = "Cancel",
        //            IsCancel = true,
        //            IsDefault = false,
        //            Command = new DelegateCommand<CancelEventArgs>(
        //                x => RevertRelationshipActions()
        //                ),
        //        };

        //        // Based on the user selection, we create and execute the dialog.
        //        switch (type)
        //        {
        //            case DialogType.ADD:
        //                // RelationshipDialogService is defined in the "Interfaces" section at the top
        //                // of this file.
        //                UICommand resultA = RelationshipDialogService.ShowDialog(
        //                    dialogCommands: new List<UICommand>() { addCommand, closeCommand },
        //                    title: "Add Relationship",
        //                    viewModel: relationshipViewModel);
        //                return (MessageBoxResult)resultA.Id;
        //            case DialogType.EDIT:
        //                // RelationshipDialogService is defined in the "Interfaces" section at the top
        //                // of this file.
        //                UICommand resultE = RelationshipDialogService.ShowDialog(
        //                    dialogCommands: new List<UICommand>() { updateCommand, cancelCommand },
        //                    title: "Edit Relationship",
        //                    viewModel: relationshipViewModel);
        //                return (MessageBoxResult)resultE.Id;
        //            case DialogType.VIEW:
        //                // RelationshipDialogService is defined in the "Interfaces" section at the top
        //                // of this file.
        //                UICommand resultV = RelationshipDialogService.ShowDialog(
        //                    dialogCommands: new List<UICommand>() { closeCommand },
        //                    title: "View Relationship",
        //                    viewModel: relationshipViewModel);
        //                return (MessageBoxResult)resultV.Id;
        //        }

        //        // Although this code should be unreacheable, we add a dummy return value to satisfy the compiler.
        //        return MessageBoxResult.Cancel;
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle the null reference expection when a user clicks on the Close ("X")......
        //        if (null == ex.InnerException)
        //        {
        //            return MessageBoxResult.Cancel;
        //        }
        //        MessageBoxService.ShowMessage("RDialog Error: " + ex.Message);
        //        return MessageBoxResult.Cancel;
        //    }
        //}
        #endregion

        /// <summary>
        /// This command fires off the Edit WaterSystem Dialog input form
        /// </summary>
        public void ShowWaterSystemDialog(Property selectedProperty)
        {
            // The WaterSystem dialog is mostly view only.  However, new meter readings
            // are entered through the meter reading grid.
            WaterMeterViewModel waterSystemViewModel = null;

            if (waterSystemViewModel == null)
            {
                // Create a new WaterSystemViewModel and copy over the property values from the PropertiesViewModel
                waterSystemViewModel = new WaterMeterViewModel();
                waterSystemViewModel.SelectedProperty = this.SelectedProperty;
                waterSystemViewModel.MeterReadings = this.SelectedProperty.MeterReadings;
                waterSystemViewModel.ApplPermissions = this.ApplPermissions;
                if (String.IsNullOrEmpty(this.SelectedProperty.Parcel))
                {
                    waterSystemViewModel.CanViewParcel = false;
                }
                else
                {
                    waterSystemViewModel.CanViewParcel = true;
                }
            }

            if (this.ApplPermissions.CanEditWater)
            {
                UICommand updateCommand = new UICommand()
                {
                    Caption = "Update",
                    IsCancel = false,
                    IsDefault = true,
                    Command = new DelegateCommand<CancelEventArgs>(
                        x => AddWaterMeetingReadingToCollection(waterSystemViewModel)
                        ),
                };
                UICommand cancelCommand = new UICommand()
                {
                    Id = MessageBoxResult.Cancel,
                    Caption = "Cancel",
                    IsCancel = true,
                    IsDefault = false,
                    Command = new DelegateCommand<CancelEventArgs>(
                        x => CancelWaterSystemAction(waterSystemViewModel)
                        ),
                };
                UICommand result = WaterSystemEditDialogService.ShowDialog(
                    dialogCommands: new List<UICommand>() { updateCommand, cancelCommand },
                    title: "Edit Water Record",
                    viewModel: waterSystemViewModel);
            }
            else
            {
                UICommand cancelCommand = new UICommand()
                {
                    Id = MessageBoxResult.Cancel,
                    Caption = "Cancel",
                    IsCancel = true,
                    IsDefault = false,
                };
                UICommand result = WaterSystemEditDialogService.ShowDialog(
                    dialogCommands: new List<UICommand>() { cancelCommand },
                    title: "Water System",
                    viewModel: waterSystemViewModel);
            }
        }
        #endregion


        /*================================================================================================================================================*/

        /* --------------------------- INotify Property Change Implementation ----------------------------- */
        #region INotifyPropertyChagned implementaiton
        /// <summary>
        /// INotifyPropertyChanged Implementation
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
    #region public partial class PropertiesViewModel : IDisposable
    public partial class PropertiesViewModel : IDisposable
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
        ~PropertiesViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
}
#endregion