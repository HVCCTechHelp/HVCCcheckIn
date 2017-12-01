﻿namespace HVCC.Shell
{
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.ViewModels;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.ObjectModel;
    using HVCC.Shell.Common;
    using HVCC.Shell.Views;
    using System.Data.Linq;
    using HVCC.Shell.Models;

    public enum UserRole
    {
        NA,
        DBO,
        Permanent,
        Seasonal,
        BoardMember
    }

    /// <summary>
    /// Summary:
    ///     Singleton instance of Host class
    /// </summary>
    public class Host : IHost
    {
        private Host()
        {
            this.OpenMvvmBinders = new ObservableCollection<IMvvmBinder>();
        }

        public static Host Instance
        {
            get { return Nested.Instance; }
        }

        private class Nested
        {
            internal static readonly Host Instance = new Host();
        }

        public ObservableCollection<IMvvmBinder> OpenMvvmBinders { get; private set; }

        public void Execute(HostVerb verb, object param)
        {
            if (verb == HostVerb.Open)
            {
                if (param.ToString() == "GolfCart")
                {
                    var binder = GetNewGolfCartView();
                    this.OpenMvvmBinders.Add(binder);
                }
            }
            else if (verb == HostVerb.Close)
            {
                var r = this.OpenMvvmBinders.Where(x => x.View == param).FirstOrDefault();
                this.OpenMvvmBinders.Remove(r);
            }
        }

        public bool AnyDirty()
        {
            foreach (IMvvmBinder b in OpenMvvmBinders)
            {
                DataContext dc = b.DataContext as DataContext;
                // MainViewModel will always be null, so we ignore it.....
                if (null != dc)
                {
                    ChangeSet cs = dc.GetChangeSet();
                    if (0 != cs.Updates.Count &&
                        0 != cs.Inserts.Count &&
                        0 != cs.Deletes.Count)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Close(IMvvmBinder mvvmBinder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Application permissions list
        /// <code>
        /// VM implementation:  ApplicationPermission permissions = Host.AppPermissions as ApplicationPermission;
        ///</code>
        /// </summary>
        public object AppPermissions
        {
            get { return ApplPermissions; }
        }

        //public bool PromptYesNo(string messagePrompt, string caption)
        //{
        //    throw new NotImplementedException();
        //}

        //public bool? PromptYesNoCancel(string messagePrompt, string caption)
        //{
        //    throw new NotImplementedException();
        //}

        //public void RefocusOrOpenViewModel(IMvvmBinder mvvmBinder)
        //{
        //    throw new NotImplementedException();
        //}

        //public void ShowMessage(string message, string caption, HostMessageType messageType = HostMessageType.None)
        //{
        //    throw new NotImplementedException();
        //}

        //// 
        //// Create/Add your View/ViewModel relationships here....
        ////

        public static IMvvmBinder GetNewMainWindow()
        {
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new MainViewModel(dc) as IViewModel;
            IView v = new MainWindow() { DataContext = vm } as IView;
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewGolfCartView()
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new GolfCartViewModel(dc) { Caption = "Golf Carts " };
            IView v = new HVCC.Shell.Views.GolfCartView(vm);
            return new MvvmBinder(dc, v, vm);
        }

        #region DataBase Roles/Permissions
        private static HVCCDataContext dc = new HVCCDataContext();

        /// <summary>
        /// Indicates if the client application is connected to the database
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

        public static UserRole DBRole
        {
            get
            {
                //return UserRole.Seasonal;
                if (IsMember(new DatabaseRoleInfo("Staff-Seasonal", "HVCC")))
                {
                    return UserRole.Seasonal;
                }
                else if (IsMember(new DatabaseRoleInfo("Staff-Permanent", "HVCC")))
                {
                    return UserRole.Permanent;
                }
                else if (IsMember(new DatabaseRoleInfo("BoardMember", "HVCC")))
                {
                    return UserRole.BoardMember;
                }
                else if (IsMember(new DatabaseRoleInfo("db_owner", "HVCC")))
                {
                    return UserRole.DBO;
                }
                else
                {
                    return UserRole.NA;
                }
            }
        }

        private static ApplicationPermission _appPermissions = null;
        /// <summary>
        /// 
        /// </summary>
        public static ApplicationPermission ApplPermissions
        {
            get
            {
                try
                {
                    //// Get the list of "ApplicationPermissions" from the database
                    object perms = (from a in dc.ApplicationPermissions
                                    where a.RoleIndex == (int)DBRole //(int)UserRole.Permanent //
                                    select a).FirstOrDefault();

                    _appPermissions = perms as ApplicationPermission;

                    return _appPermissions;
                }
                catch (Exception ex)
                {
                    return null;
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
                        object defaults = (from a in dc.ApplicationDefaults
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
        /// Returns true/false if the current user is in the database role being tested.
        /// </summary>
        /// <param name="databaseRole"></param>
        /// <returns></returns>
        public static bool IsMember(DatabaseRoleInfo databaseRole)
        {
            using (HVCCDataContext db = new HVCCDataContext())
            {
                return true == db.fn_IsMember(databaseRole.Role);
            }
        }

        #endregion

    }
}