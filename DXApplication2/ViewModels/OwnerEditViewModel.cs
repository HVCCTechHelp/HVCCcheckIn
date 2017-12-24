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
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;

    public partial class OwnerEditViewModel : CommonViewModel, ICommandSink
    {
        public OwnerEditViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            Owner p = parameter as Owner;

            try
            {
                // Fetch the Owner record and its Relationships from the database so the 
                // datacontext will be scoped to this ViewModel.
                SelectedOwner = (from x in this.dc.Owners
                                 where x.OwnerID == p.OwnerID
                                 select x).FirstOrDefault();

                var rList = (from x in this.dc.Relationships
                            where x.OwnerID == SelectedOwner.OwnerID
                            select x);

                Relationships = new ObservableCollection<Relationship>(rList);
                // Set the focused row in the Relationships grid to the first item in the Owner's
                // Relationship collection.
                SelectedRelationship = Relationships[0];

                HeaderText = string.Format("HVCC Notes [{0}]", NoteCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Owner Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            SelectedOwner.PropertyChanged +=
                 new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);
        }

        #region Interfaces
        //public IMessageBoxService MessageBoxService { get { return GetService<IMessageBoxService>(); } }
        //public virtual ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        ////protected virtual IOpenFileDialogService OpenFileDialogService { get { return this.GetService<IOpenFileDialogService>(); } }
        //public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
        //public enum ExportType { PDF, XLSX }
        //public enum PrintType { PREVIEW, PRINT }

        #endregion

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
                    this._selectedRelationship = value;
                    if (null != this._selectedRelationship)
                    {
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
                this._relationships.CollectionChanged += _relationships_CollectionChanged;
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
                var notes = (from n in this.dc.Notes
                             where n.OwnerID == this.SelectedOwner.OwnerID
                             orderby n.Entered descending
                             select n);

                NoteCount = notes.Count();

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
                            // If the FName string is empty, than we landed here as a result of an 'Add' to the 
                            // Relationship collection firing on a collection change event.  We can ignore this
                            // request for now.
                            if (!string.IsNullOrEmpty(r.FName)
                                && !string.IsNullOrEmpty(r.LName)
                                && !string.IsNullOrEmpty(r.RelationToOwner))
                            {
                                bool result = Helper.AddRelationship(this.dc, SelectedOwner, r);
                            }
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
            CanSaveExecute = IsDirty;
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
        ///TextEdit LostFocus Event Action
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
