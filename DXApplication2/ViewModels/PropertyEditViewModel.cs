// TO-DO:  Plumb the Save button to the IsEnabledSave
////////////////////////////////////////////////////////////////////////////////////////////
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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;

    public partial class PropertyEditViewModel : CommonViewModel, ICommandSink
    {

        public PropertyEditViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            // The invocation of this VM can come from PropertyDetails or OwnerEdit.  The Detail view(s)
            // are bound to database views, whereas Edit view(s) are bound to the database table. Therefore,
            // we have to check the type of the 'parameter' being passed in.
            if (parameter is Property)
            {
                Property p = parameter as Property;
                SelectedProperty = GetProperty(p.PropertyID);
            }
            else
            {
                v_PropertyDetail p = parameter as v_PropertyDetail;
                SelectedProperty = GetProperty(p.PropertyID);
            }
            Owner = SelectedProperty.Owner;  // The View is bound to this element
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            CanSaveExecute = false;
            this.RegisterCommands();

            NotesHeader = string.Format("HVCC Notes [{0}]", NoteCount);
        }

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

        #region Properties

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        private Property _selectedProperty = null;
        public Property SelectedProperty
        {
            get
            {
                HeaderText = String.Format("Lot# {0}", _selectedProperty.Customer);
                IsBusy = false;
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
                        this._selectedProperty = value;
                        AllNotes = GetOwnerNotes();
                    }
                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

        /// <summary>
        /// The currently selected Owner record to be acted upon.
        /// </summary>
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
                    _owner = value;
                    RaisePropertyChanged("Owner");
                }
            }
        }

        private string _notesHeader = string.Empty;
        public string NotesHeader
        {
            get
            {
                return _notesHeader;
            }
            set
            {
                if (_notesHeader != value)
                {
                    _notesHeader = value;
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
                MessageBox.Show("Can't retrieve property from database\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }


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
                             where n.OwnerID == this.SelectedProperty.Owner.OwnerID
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

            dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Notes);
            AllNotes = GetOwnerNotes();
            NewNote = string.Empty;

            this.IsBusy = false;
            RaisePropertyChanged("IsNotBusy");
            RaisePropertyChanged("DataChanged");

            //Host.Execute(HostVerb.Close, this.Caption);
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
    /// ViewModel Commands
    /// </summary>
    public partial class PropertyEditViewModel
    {
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
                MessageBox.Show(ex.Message);
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
                MessageBox.Show(ex.Message);
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
