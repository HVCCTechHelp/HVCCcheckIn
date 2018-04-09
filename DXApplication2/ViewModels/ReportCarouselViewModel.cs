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
    using HVCC.Shell.Resources;
    using System.Collections.Generic;
    using DevExpress.Xpf.Printing;
    using HVCC.Shell.Reports;
    using HVCC.Shell.Helpers;

    public partial class ReportCarouselViewModel : CommonViewModel, ICommandSink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public ReportCarouselViewModel(IDataContext dc, IDataContext pDC = null)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            this.RegisterCommands();

            var permissions = (from x in this.dc.ApplicationPermissions
                               select x);
            Permissions = new ObservableCollection<ApplicationPermission>(permissions);

            // Build the collection of reports for the carousel....
            string b64text = String.Empty;
            byte[] imgBytes = null;

            imgBytes = System.IO.File.ReadAllBytes(@"\\HVCCServer\HVCCApplication\Images\Reports\Transactions.JPG");
            b64text = Convert.ToBase64String(imgBytes);
            CarouselCollection.Add(new Carousel { B64Text = b64text, Header = "Transaction Report", Report = "FinancialTransaction" });

            imgBytes = System.IO.File.ReadAllBytes(@"\\HVCCServer\HVCCApplication\Images\Reports\BalanceDue.JPG");
            b64text = Convert.ToBase64String(imgBytes);
            CarouselCollection.Add(new Carousel { B64Text = b64text, Header = "Balances Due Report", Report = "BalancesDue" });

            imgBytes = System.IO.File.ReadAllBytes(@"\\HVCCServer\HVCCApplication\Images\Reports\OwnershipHistory.JPG");
            b64text = Convert.ToBase64String(imgBytes);
            CarouselCollection.Add(new Carousel { B64Text = b64text, Header = "Ownership History Report", Report = "OwnershipHistory" });

            imgBytes = System.IO.File.ReadAllBytes(@"\\HVCCServer\HVCCApplication\Images\Reports\PropertyNotes.JPG");
            b64text = Convert.ToBase64String(imgBytes);
            CarouselCollection.Add(new Carousel { B64Text = b64text, Header = "Owner Notes Report", Report = "OwnerNotes" });

            imgBytes = System.IO.File.ReadAllBytes(@"\\HVCCServer\HVCCApplication\Images\Reports\DailyUsage.JPG");
            b64text = Convert.ToBase64String(imgBytes);
            CarouselCollection.Add(new Carousel { B64Text = b64text, Header = "Daily Usage Report", Report = "DailyUsage" });

            imgBytes = System.IO.File.ReadAllBytes(@"\\HVCCServer\HVCCApplication\Images\Reports\FacilityUsage.JPG");
            b64text = Convert.ToBase64String(imgBytes);
            CarouselCollection.Add(new Carousel { B64Text = b64text, Header = "Facility Usage Report", Report = "FacilityUsage" });

            SelectedItem = CarouselCollection[0];
        }


        public MvvmBinder Binder { get; set; }
        public string Base64ImageData { get; set; }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
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

        private int _activeItem = 0;
        public int ActiveItem
        {
            get
            {
                return _activeItem;
            }
            set
            {
                if (value != _activeItem)
                {
                    _activeItem = value;
                    SelectedItem = CarouselCollection[_activeItem];
                }
            }
        }

        private ObservableCollection<Carousel> _carouselCollection = new ObservableCollection<Carousel>();
        public ObservableCollection<Carousel> CarouselCollection
        {
            get { return _carouselCollection; }
            set
            {
                if (value != _carouselCollection)
                {
                    _carouselCollection = value;
                }
            }
        }

        public Carousel SelectedItem { get; set; }

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

    }


    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class ReportCarouselViewModel : CommonViewModel, ICommandSink
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
    /// ViewModel Commands
    /// </summary>
    public partial class ReportCarouselViewModel
    {

        /// <summary>
        /// PreviewMouseDown Command
        /// </summary>
        private ICommand _previewMouseDownCommand;
        public ICommand PreviewMouseDownCommand
        {
            get
            {
                return _previewMouseDownCommand ?? (_previewMouseDownCommand = new CommandHandlerWparm((object parameter) => PreviewMouseDownAction(parameter), true));
            }
        }

        /// <summary>
        /// PreviewMouseDown Action
        /// </summary>
        /// <param name="parameter"></param>
        public void PreviewMouseDownAction(object parameter)
        {
            int index = ActiveItem;
        }

        /// <summary>
        /// Test Command
        /// </summary>
        private ICommand _carouselCommand;
        public ICommand CarouselCommand
        {
            get
            {
                return _carouselCommand ?? (_carouselCommand = new CommandHandlerWparm((object parameter) => CarouselAction(parameter), true));
            }
        }

        /// <summary>
        /// Test Action
        /// </summary>
        /// <param name="parameter"></param>
        public void CarouselAction(object parameter)
        {

            // Get the MVVM binder for this viewmodel.  We need reference to the ViewModel's View
            // in order to display the transaction receipt.
            foreach (MvvmBinder m in Host.OpenMvvmBinders)
            {
                if (m.ViewModel == this)
                {
                    Binder = m;
                    break;
                }
            }

            object report;
            Views.ReportCarouselView view = (HVCC.Shell.Views.ReportCarouselView)Binder.View;
            Dictionary<int, int> daysInMonth = Helper.PopulateDates();
            int mon, year;
            DateTime start, end;


            switch (SelectedItem.Report)
            {

                case "FinancialTransaction":
                    report = new Reports.WeeklyFinancialTransactionReport();
                    PrintHelper.ShowPrintPreview(view, (WeeklyFinancialTransactionReport)report);
                    break;
                case "BalancesDue":
                    report = new BalancesDueReport();
                    PrintHelper.ShowPrintPreview(view, (BalancesDueReport)report);
                    break;
                case "OwnershipHistory":
                    Host.Execute(HostVerb.Open, "OwnershipHistory");
                    break;
                case "OwnerNotes":
                    year = DateTime.Now.Year;
                    mon = DateTime.Now.Month;

                    start = new DateTime(year, mon, 1, 12, 0, 0);
                    end = new DateTime(year, mon, daysInMonth[mon], 12, 0, 0);

                    PropertyNotesReport reportX = new PropertyNotesReport();
                    reportX.Parameters["StartDate"].Value = start;
                    reportX.Parameters["EndDate"].Value = end;
                    PrintHelper.ShowPrintPreview(view, reportX);
                    break;
                case "DailyUsage":
                    DateTime forDate = DateTime.Now;
                    Reports.DailyDetailReport reportY = new Reports.DailyDetailReport();
                    reportY.Parameters["ForDate"].Value = forDate;
                    PrintHelper.ShowPrintPreview(view, reportY);
                    break;
                case "FacilityUsage":
                    year = DateTime.Now.Year;
                    mon = DateTime.Now.Month;

                    start = new DateTime(year, mon, 1, 12, 0, 0);
                    end = new DateTime(year, mon, daysInMonth[mon], 12, 0, 0);

                    FacilitiesUsageReport reportZ = new FacilitiesUsageReport();
                    reportZ.Parameters["StartDate"].Value = start;
                    reportZ.Parameters["EndDate"].Value = end;
                    PrintHelper.ShowPrintPreview(view, reportZ);
                    break;
                default:
                    break;
            }

            //string fileName = string.Format(@"D:\Transaction-{0}.PDF", ft.RowId);
            //report.CreateDocument();
            //report.ExportToPdf(fileName);
        }
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class ReportCarouselViewModel : IDisposable
    public partial class ReportCarouselViewModel : IDisposable
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
        ~ReportCarouselViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}
