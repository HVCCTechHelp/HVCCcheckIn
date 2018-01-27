namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using HVCC.Shell.Common;
    using HVCC.Shell.Models;
    using DevExpress.Spreadsheet;
    using System.Windows.Input;
    using HVCC.Shell.Resources;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Common.Interfaces;

    public partial class WaterShutoffViewModel : CommonViewModel, ICommandSink
    {
        // The WaterShutoffViewModel should only ever be created by an OwnerEdit Ribbon button event.
        // 'parameter' will be a reference to the SelectedOwner of the OwnerEditViewModel.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public WaterShutoffViewModel(IDataContext dc, IDataContext pDC = null)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            this.RegisterCommands();

            WaterShutoffs = FetchShutoffs();
        }

        public override bool IsValid { get { return true; } }

        private bool _isDirty = false;
        public override bool IsDirty
        {
            get
            {
                string[] caption = Caption.ToString().Split('*');
                if (_isDirty)
                {
                    Caption = caption[0].TrimEnd(' ') + "* ";
                    return true;
                }
                Caption = caption[0].TrimEnd(' ');
                return false;
            }
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                }
            }
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
        /// Controls enable/disbale state of the Refresh ribbion action button
        /// </summary>
        private bool _isRefreshEnabled = true; // Default: false
        public bool IsRefreshEnabled
        {
            get { return _isRefreshEnabled; }
            set
            {
                if (value != _isRefreshEnabled)
                {
                    _isRefreshEnabled = value;
                    RaisePropertyChanged("IsRefreshEnabled");
                }
            }
        }

        private ObservableCollection<v_WaterShutoff> _waterShutoffs = null;
        public ObservableCollection<v_WaterShutoff> WaterShutoffs
        {
            get
            {
                return _waterShutoffs;
            }
            set
            {
                if (_waterShutoffs != value)
                {
                    _waterShutoffs = value;
                    RaisePropertyChanged("WaterShutoffs");
                }
            }
        }

        private v_WaterShutoff _selecctedRow = null;
        public v_WaterShutoff SelectedRow
        {
            get
            {
                return _selecctedRow;
            }
            set
            {
                if (_selecctedRow != value)
                {
                    _selecctedRow = value;
                }
            }
        }

        /// <summary>
        /// Gets a list of WaterShutoff records
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<v_WaterShutoff> FetchShutoffs()
        {
            var waterShutoffs = (from x in dc.v_WaterShutoffs
                     select x);
            return new ObservableCollection<v_WaterShutoff>(waterShutoffs);

        }
}

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class WaterShutoffViewModel : CommonViewModel, ICommandSink
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
    public partial class WaterShutoffViewModel : CommonViewModel
    {

        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _waterShutoffCommand;
        public ICommand WaterShutoffCommand
        {
            get
            {
                return _waterShutoffCommand ?? (_waterShutoffCommand = new CommandHandlerWparm((object parameter) => WaterShutoffAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void WaterShutoffAction(object parameter)
        {
            //v_WaterShutoff p = parameter as v_WaterShutoff; // TO-DO: i have not idea why the parameter isn't being passed...
            v_WaterShutoff p = SelectedRow;
            IsBusy = true;
            Host.Execute(HostVerb.Open, "WaterShutoffEdit", p);
        }
        /// <summary>
        /// Refresh Command
        /// </summary>
        private ICommand _refreshCommand;
        public ICommand RefreshCommand
        {
            get
            {
                return _refreshCommand ?? (_refreshCommand = new CommandHandlerWparm((object parameter) => RefreshAction(parameter), IsRefreshEnabled));
            }
        }

        /// <summary>
        /// Refresh data sources
        /// </summary>
        /// <param name="type"></param>
        public void RefreshAction(object parameter)
        {
            RaisePropertyChanged("IsBusy");
            WaterShutoffs = FetchShutoffs();
            RaisePropertyChanged("IsNotBusy");
        }


    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class WaterShutoffViewModel : IDisposable
    public partial class WaterShutoffViewModel : IDisposable
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
        ~WaterShutoffViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
