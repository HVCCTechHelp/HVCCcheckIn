namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Xml;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Views;
    using HVCC.Shell.Common.ViewModels;
    using System.Windows.Input;
    using HVCC.Shell.Resources;
    using HVCC.Shell.Models;

    /// <summary>
    /// Shell.Instance is the "ViewModel" to the MainWindow "View"
    /// </summary>
    public partial class MainViewModel : CommonViewModel, IViewModel
    {
        //public void Initialize(MainWindow view)
        //{
        //    this.View = view;
        //}

        internal MainWindow View { get; private set; }

        public override bool IsValid
        {
            get { return true; }
        }

        public override bool IsDirty
        {
            get
            {
                return false;
            }
        }

        private bool _isBusy = false;
        public override bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if (value != _isBusy)
                {
                    _isBusy = value;
                    RaisePropertyChanged("IsBusy");
                }
            }
        }
    }

    public partial class MainViewModel : CommonViewModel, IViewModel
    {
        public MainViewModel(IDataContext dc)
        {
            this.Host = HVCC.Shell.Host.Instance;
            this.dc = dc as HVCCDataContext;
            this.OpenMvvmBinders.CollectionChanged += this.OpenMvvmBinders_CollectionChanged;
        }

        private void OpenMvvmBinders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.OpenMvvmBinders != null)
            {
                // TO-DO: Need to figure out if this is necessary.....
                App.OpenDocuments = this.OpenMvvmBinders.Select(x => x.GetType().Name).ToList();
            }
        }
    }

    public partial class MainViewModel: CommonViewModel // Keep Track of Open IMvvmBinders
    {
        /// <summary>
        /// The MvvmBinders interface collection keeps track of the View / ViewModel relationships
        /// </summary>
        private ObservableCollection<IMvvmBinder> openMvvmBinders = new ObservableCollection<IMvvmBinder>();
        public ObservableCollection<IMvvmBinder> OpenMvvmBinders
        {
            get { return this.openMvvmBinders; }
        }

        // TO-DO: Need to figure out if this is necessary.....
        internal void Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow mw = this.View as MainWindow;

            IEnumerable<IViewModel> allOpenDocuments = from x in this.OpenMvvmBinders
                                                  where x.ViewModel is IViewModel
                                                       select x.ViewModel as IViewModel;
            if (null != allOpenDocuments && allOpenDocuments.Count() > 0)
            {
                bool cancelClose = false;
                foreach (IViewModel form in allOpenDocuments)
                {
                    bool cancel = false;
                    form.Closing(out cancel);
                    cancelClose |= cancel;
                }

                if (cancelClose)
                {
                    e.Cancel = cancelClose;
                    return;
                }
            }

            IEnumerable<IViewModel> dirtyDocuments = from x in this.OpenMvvmBinders
                                                where x.ViewModel is IViewModel && ((IViewModel)x.ViewModel).IsDirty
                                                select x.ViewModel as IViewModel;
            if (null != dirtyDocuments && dirtyDocuments.Count() > 0)
            {
                string seperator = string.Empty;
                StringBuilder sb = new StringBuilder();
                foreach (IViewModel form in dirtyDocuments)
                {
                    sb.Append(seperator);
                    sb.Append(form.Caption);
                    seperator = ", ";
                }

                MessageBoxResult result = MessageBox.Show(string.Format("Do you want to save your changes to the following files?: {0}", sb.ToString()), "Save Changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;

                    case MessageBoxResult.Yes:
                        {
                            try
                            {
                                if (CustomCommands.SaveAll.CanExecute(null, this.View))
                                {
                                    CustomCommands.SaveAll.Execute(null, this.View);
                                }
                            }
                            finally
                            {
                                IEnumerable<IViewModel> stillDirtyDocuments = from x in this.OpenMvvmBinders
                                                                         where x.ViewModel is IViewModel && ((IViewModel)x.ViewModel).IsDirty
                                                                         select x.ViewModel as IViewModel;
                                e.Cancel = stillDirtyDocuments.Count() > 0;
                            }
                        }

                        break;
                }
            }
        }
    }

    public partial class MainViewModel
    {
        /* -------------------------------- DataBase Roles -------------------------------------------- */
        #region DataBase Roles/Permissions

        /// <summary>
        /// Enumerated database roles
        /// </summary>
        public enum UserRole
        {
            NA,
            DBO,
            Permanent,
            Seasonal,
            BoardMember
        }

        /// <summary>
        /// Returns the database role for the user
        /// </summary>
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
        /// Application specific permissions based on database role
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

        /// <summary>
        /// Checks to see if there is a valid connection to the database.
        /// </summary>
        public bool IsConnected
        {
            get
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
        }

        /// <summary>
        /// Returns true/false if the current user is in the database role being tested.
        /// </summary>
        /// <param name="databaseRole"></param>
        /// <returns></returns>
        public bool IsMember(DatabaseRoleInfo databaseRole)
        {
                return true == dc.fn_IsMember(databaseRole.Role);
        }

        #endregion
    }

    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class MainViewModel : IDisposable
    {
        // Resources that must be disposed:
        public HVCCDataContext dc = null; // new HVCC.Shell.Models.HVCCDataContext();

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
        ~MainViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion


}