namespace HVCC.Shell
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
    using System.Windows;
    using System.ComponentModel;

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
        #region Singelton Implementation
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
        #endregion

        /// <summary>
        /// Application permissions list
        /// </summary>
        /// <example><code>
        /// VM implementation:  ApplicationPermission permissions = Host.AppPermissions as ApplicationPermission;
        ///</code></example>
        public object AppPermissions { get { return ApplPermissions; } }
        public object AppDefault { get { return ApplDefault; } }

        #region Interface Implementation
        public ObservableCollection<IMvvmBinder> OpenMvvmBinders { get; private set; }
        /* ============================================================================================= */
        //// 
        //// Create/Add your View/ViewModel binders here....
        ////
        public static IMvvmBinder GetNewMainWindow()
        {
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new MainViewModel(dc) as IViewModel;
            IView v = new MainWindow() { DataContext = vm } as IView;
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewOwnersView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new OwnersViewModel(dc) { Caption = "Owners" };
            IView v = new HVCC.Shell.Views.OwnersView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewPropertyDetailView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new PropertiesDetailsViewModel(dc) { Caption = "Properties" };
            IView v = new HVCC.Shell.Views.PropertyDetailsView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewPropertyEditView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new PropertyEditViewModel(dc, arg) { Caption = "Edit Property" };
            IView v = new HVCC.Shell.Views.PropertyEditView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewChangeOwnerView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new ChangeOwnerViewModel(dc, arg) { Caption = "Change Owner" };
            IView v = new HVCC.Shell.Views.ChangeOwnerView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewOwnerEditView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new OwnerEditViewModel(dc, arg) { Caption = "Edit Owner" };
            IView v = new HVCC.Shell.Views.OwnerEditView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewOwnershipHistoryView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new OwnershipHistoryViewModel(dc) { Caption = "Owner History" };
            IView v = new HVCC.Shell.Views.OwnershipHistoryView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewPropertiesUpdatedView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new PropertiesUpdatedViewModel(dc, arg) { Caption = "Updated Balances" };
            IView v = new HVCC.Shell.Views.PropertiesUpdatedView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewGolfCartView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new GolfCartViewModel(dc) { Caption = "Golf Carts" };
            IView v = new HVCC.Shell.Views.GolfCartView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        //
        public static IMvvmBinder GetNewWaterMeterView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new WaterMeterViewModel(dc) { Caption = "Water Meter Readings" };
            IView v = new HVCC.Shell.Views.WaterMeterView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewWaterMeterEditView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new WaterMeterEditViewModel(dc, arg) { Caption = "Water Meter Edit" };
            IView v = new HVCC.Shell.Views.WaterSystemEditView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        public static IMvvmBinder GetNewWellMeterView(object arg)
        {
            ////IDataContext dc = new UnitTextConnectionDataContext();
            IDataContext dc = new HVCC.Shell.Models.HVCCDataContext() as IDataContext;
            IViewModel vm = new WaterWellViewModel(dc) { Caption = "Well Meter Readings" };
            IView v = new HVCC.Shell.Views.WellMeterReadingsView(vm);
            return new MvvmBinder(dc, v, vm);
        }
        //

        /// <summary>
        /// Executes Open/Close on MvvmBinders
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="param"></param>
        public void Execute(HostVerb verb, object param, object arg = null)
        {
            if (verb == HostVerb.Open)
            {
                // REQUIRED:  The Caption strings must match the x:Name string of the View
                if (param.ToString() == "Properties")
                {
                    var binder = GetNewPropertyDetailView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "PropertyEdit")
                {
                    var binder = GetNewPropertyEditView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "Owners")
                {
                    var binder = GetNewOwnersView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "ChangeOwner")
                {
                    var binder = GetNewChangeOwnerView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "OwnershipHistory")
                {
                    var binder = GetNewOwnershipHistoryView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "EditOwner")
                {
                    var binder = GetNewOwnerEditView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "ImportBalances")
                {
                    var binder = GetNewPropertiesUpdatedView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "GolfCart")
                {
                    var binder = GetNewGolfCartView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "WaterMeter")
                {
                    var binder = GetNewWaterMeterView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "WaterMeterEdit")
                {
                    var binder = GetNewWaterMeterEditView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
                else if (param.ToString() == "WellMeter")
                {
                    var binder = GetNewWellMeterView(arg);
                    this.OpenMvvmBinders.Add(binder);
                }
            }
            else if (verb == HostVerb.Close)
            {
                string parameter = param as String;
                try
                {
                    var r = this.OpenMvvmBinders.Where(x => x.ViewModel.Caption.Replace(" ", "") == parameter.Replace(" ", "")).FirstOrDefault();
                    bool result = this.OpenMvvmBinders.Remove(r);

                    // If we are closing one of the Edit forms, raise a refresh PropertyChanged event to let the 
                    // detail view(s) know that data may have changed.
                    if (parameter.Contains("Edit"))
                    {
                        RaisePropertyChanged("Refresh");
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Host Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void Close(IMvvmBinder mvvmBinder)
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Iterates through the collection of MvvmBinders to see if any ViewModels are dirty.
        /// </summary>
        /// <returns></returns>
        public bool AnyDirty()
        {
            foreach (IMvvmBinder b in OpenMvvmBinders)
            {
                try
                {
                    DataContext dc = b.DataContext as DataContext;
                    // MainViewModel will always be null, so we ignore it.....
                    if (null != dc)
                    {
                        ChangeSet cs = dc.GetChangeSet();
                        if (0 != cs.Updates.Count ||
                            0 != cs.Inserts.Count ||
                            0 != cs.Deletes.Count)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in Host: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return false;
        }

        /// <summary>
        /// Removes a binder from the collection based on the ViewModel caption
        /// </summary>
        /// <param name="caption"></param>
        public void RemoveBinderByCaption(string caption)
        {
            string[] parts = caption.Split('*');
            caption = parts[0].TrimEnd(' ');

            foreach (MvvmBinder b in OpenMvvmBinders)
            {
                if (b.ViewModel.Caption.Contains(caption))
                {
                    OpenMvvmBinders.Remove(b);
                    break;
                }
            }
        }

        /* ================================================================================================ */

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
                    MessageBox.Show("Error retrieving application defaults\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        MessageBox.Show("Error retrieving application defaults\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
