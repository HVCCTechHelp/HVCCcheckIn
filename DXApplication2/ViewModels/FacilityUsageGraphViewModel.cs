namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using DevExpress.Xpf.Docking;
    using System.Data.Linq;
    using HVCC.Shell.Common;
    using HVCC.Shell.Models;
    using DevExpress.Spreadsheet;
    using System.Windows.Input;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Common.Interfaces;

    public partial class FacilityUsageGraphViewModel : CommonViewModel, ICommandSink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public FacilityUsageGraphViewModel(IDataContext dc, IDataContext pDC = null)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            this.RegisterCommands();

            var permissions = (from x in this.dc.ApplicationPermissions
                               select x);
            Permissions = new ObservableCollection<ApplicationPermission>(permissions);

            CurrentSeason = (from x in Seasons
                             where x.IsCurrent == true
                             select x).FirstOrDefault();

            SelectedSeason = CurrentSeason;
            int ndx = 0;
            foreach (Season s in Seasons)
            {
                if (s.TimePeriod == SelectedSeason.TimePeriod)
                {
                    break;
                }
                ndx++;
            }
            SelectedSeasonIndex = ndx;
        }

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

        private ObservableCollection<ApplicationPermission> _permissions = null;
        public ObservableCollection<ApplicationPermission> Permissions
        {
            get
            {
                return _permissions;
            }
            set
            {
                if (_permissions != value)
                {
                    _permissions = value;
                    RaisePropertyChanged("Permissions");
                }
            }
        }

        public ObservableCollection<Season> Seasons
        {
            get
            {
                var list = (from x in this.dc.Seasons
                            select x);
                return new ObservableCollection<Season>(list);
            }
        }

        private Season _currentSeason = null;
        public Season CurrentSeason
        {
            get
            {
                return _currentSeason;
            }
            set
            {
                if (_currentSeason != value)
                {
                    _currentSeason = value;
                }
            }
        }

        private Season _selectedSeason = null;
        public Season SelectedSeason
        {
            get
            {
                return _selectedSeason;
            }
            set
            {
                if (_selectedSeason != value)
                {
                    _selectedSeason = value;
                    RaisePropertyChanged("SelectedSeason");
                }
            }
        }

        private int _selectedSeasonIndex = 0;
        public int SelectedSeasonIndex
        {
            get
            {
                return _selectedSeasonIndex;
            }
            set
            {
                if (_selectedSeasonIndex != value)
                {
                    _selectedSeasonIndex = value;
                    SelectedSeason = Seasons[_selectedSeasonIndex];
                    RaisePropertyChanged("SelectedSeasonIndex");
                }
            }
        }

        private bool _isDisplayGolfMembers = true;
        public bool IsDisplayGolfMembers
        {
            get
            {
                return _isDisplayGolfMembers;
            }
            set
            {
                if (value != _isDisplayGolfMembers)
                {
                    _isDisplayGolfMembers = value;
                    RaisePropertiesChanged("IsDisplayGolfMembers");
                }
            }
        }

        private bool _isDisplayGolfGuests = true;
        public bool IsDisplayGolfGuests
        {
            get
            {
                return _isDisplayGolfGuests;
            }
            set
            {
                if (value != _isDisplayGolfGuests)
                {
                    _isDisplayGolfGuests = value;
                    RaisePropertiesChanged("IsDisplayGolfGuests");
                }
            }
        }

        private bool _isDisplayPoolMembers = true;
        public bool IsDisplayPoolMembers
        {
            get
            {
                return _isDisplayPoolMembers;
            }
            set
            {
                if (value != _isDisplayPoolMembers)
                {
                    _isDisplayPoolMembers = value;
                    RaisePropertiesChanged("IsDisplayPoolMembers");
                }
            }
        }

        private bool _isDisplayPoolGuests = true;
        public bool IsDisplayPoolGuests
        {
            get
            {
                return _isDisplayPoolGuests;
            }
            set
            {
                if (value != _isDisplayPoolGuests)
                {
                    _isDisplayPoolGuests = value;
                    RaisePropertiesChanged("IsDisplayPoolGuests");
                }
            }
        }

        public ObservableCollection<GroupedRow> GolfMembers
        {
            get
            {
                var result = (from row in this.dc.FacilityUsages
                              where row.GolfRoundsMember > 0
                              && row.Date >= BeginDate && row.Date <= EndDate
                              group row by new { row.Date.Date } into g
                              select new GroupedRow()
                              {
                                  Description = "Golf Members",
                                  Date = g.Key.Date,
                                  Sum = g.Sum(x => x.GolfRoundsMember)
                              });

                return new ObservableCollection<GroupedRow>(result);
            }
        }

        public ObservableCollection<GroupedRow> GolfGuests
        {
            get
            {
                var result = (from row in this.dc.FacilityUsages
                              where row.GolfRoundsGuest > 0
                              && row.Date >= BeginDate && row.Date <= EndDate
                              group row by new { row.Date.Date } into g
                              select new GroupedRow()
                              {
                                  Description = "Golf Guest",
                                  Date = g.Key.Date,
                                  Sum = g.Sum(x => x.GolfRoundsGuest)
                              });

                return new ObservableCollection<GroupedRow>(result);
            }
        }

        public ObservableCollection<GroupedRow> PoolMembers
        {
            get
            {
                var result = (from row in this.dc.FacilityUsages
                              where row.PoolMember > 0
                              && row.Date >= BeginDate && row.Date <= EndDate
                              group row by new { row.Date.Date } into g
                              select new GroupedRow()
                              {
                                  Description = "Pool Members",
                                  Date = g.Key.Date,
                                  Sum = g.Sum(x => x.PoolMember)
                              });

                return new ObservableCollection<GroupedRow>(result);
            }
        }

        public ObservableCollection<GroupedRow> PoolGuests
        {
            get
            {
                var result = (from row in this.dc.FacilityUsages
                              where row.PoolGuest > 0
                              && row.Date >= BeginDate && row.Date <= EndDate
                              group row by new { row.Date.Date } into g
                              select new GroupedRow()
                              {
                                  Description = "Pool Guests",
                                  Date = g.Key.Date,
                                  Sum = g.Sum(x => x.PoolGuest)
                              });

                return new ObservableCollection<GroupedRow>(result);
            }
        }

        private DateTime _beginDate = new DateTime(2017,5,1,0,0,0);
        public DateTime BeginDate
        {
            get
            {
                return _beginDate;
            }
            set
            {
                if (value != _beginDate)
                {
                    _beginDate = value;
                    RaisePropertiesChanged("GolfMembers");
                    RaisePropertiesChanged("GolfGuests");
                    RaisePropertiesChanged("PoolMembers");
                    RaisePropertiesChanged("PoolGuests");
                    RaisePropertiesChanged("BeginDate");
                }
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get
            {
                return _endDate;
            }
            set
            {
                if (value != _endDate)
                {
                    _endDate = value;
                    RaisePropertiesChanged("GolfMembers");
                    RaisePropertiesChanged("GolfGuests");
                    RaisePropertiesChanged("PoolMembers");
                    RaisePropertiesChanged("PoolGuests");
                    RaisePropertiesChanged("EndDate");
                }
            }

        }

    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class FacilityUsageGraphViewModel : CommonViewModel, ICommandSink
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
        private bool CanSaveExecute
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            this.dc.SubmitChanges();
            RaisePropertyChanged("DataChanged");
            this.IsBusy = false;
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
    /// Disposition.......
    /// </summary>
    #region public partial class AdministrationViewModel : IDisposable
    public partial class FacilityUsageGraphViewModel : IDisposable
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
        ~FacilityUsageGraphViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
