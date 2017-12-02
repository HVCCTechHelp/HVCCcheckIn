namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using HVCC.Shell.Models;
    using DevExpress.Mvvm;
    using HVCC.Shell.Common.Interfaces;
    using DevExpress.Xpf.Grid;

    /// <summary>
    /// Shell.Instance is the "ViewModel" to the MainWindow "View"
    /// </summary>
    public partial class MainViewModel : ViewModelBase, IViewModel
    {
        HVCCDataContext dc = null;
        public MainViewModel(IDataContext dc)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            //this.RegisterCommands();
        }

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

        /*----------------------- IViewModel Members --------------------------------------- */
        public string Caption { get ; set; }
        public TableView Table { get; set; }

        public bool IsValid { get; set; }

        public bool IsDirty { get; set; }

        public IHost Host { get; set; }

        /// <summary>
        /// Returns true/false if the current user is in the database role being tested.
        /// </summary>
        /// <param name="databaseRole"></param>
        /// <returns></returns>
        public bool IsMember(DatabaseRoleInfo databaseRole)
        {
            return true == dc.fn_IsMember(databaseRole.Role);
        }

        public void Closing(out bool cancelCloseOperation)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}