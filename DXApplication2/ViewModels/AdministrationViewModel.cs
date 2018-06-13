namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using DevExpress.Xpf.Docking;
    using System.Data.Linq;
    using HVCC.Shell.Common;
    using DevExpress.Mvvm;
    using HVCC.Shell.Models;
    using DevExpress.Spreadsheet;
    using System.Windows.Input;
    using HVCC.Shell.Resources;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Common.Interfaces;
    using System.Diagnostics;
    using System.Windows;
    using System.Text;
    using System.Globalization;

    public partial class AdministrationViewModel : CommonViewModel, ICommandSink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public AdministrationViewModel(IDataContext dc, IDataContext pDC = null)
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

        public ObservableCollection<Owner> Owners { get; set; }

        public bool IsApply30DayLate
        {
            get
            {
                string[] strings = SelectedSeason.TimePeriod.Split('-');
                int yyyy;
                Int32.TryParse(strings[0], out yyyy);
                DateTime startDate = new DateTime(yyyy, 5, 31, 0, 0, 0);
                DateTime endDate = new DateTime(yyyy, 6, 7, 0, 0, 0);
                if (DateTime.Now >= startDate
                    && DateTime.Now <= endDate
                    && !SelectedSeason.IsLate30Applied)
                {
                    return true;
                }
                return false;
            }
        }
        public bool IsApply60DayLate
        {
            get
            {
                string[] strings = SelectedSeason.TimePeriod.Split('-');
                int yyyy;
                Int32.TryParse(strings[0], out yyyy);
                DateTime startDate = new DateTime(yyyy, 7, 1, 0, 0, 0);
                DateTime endDate = new DateTime(yyyy, 7, 7, 0, 0, 0);
                if (DateTime.Now >= startDate
                    && DateTime.Now <= endDate
                    && !SelectedSeason.IsLate60Applied)
                {
                    return true;
                }
                return false;
            }
        }
        public bool IsApply90DayLate
        {
            get
            {
                string[] strings = SelectedSeason.TimePeriod.Split('-');
                int yyyy;
                Int32.TryParse(strings[0], out yyyy);
                DateTime startDate = new DateTime(yyyy, 8, 1, 0, 0, 0);
                DateTime endDate = new DateTime(yyyy, 8, 7, 0, 0, 0);
                if (DateTime.Now >= startDate
                    && DateTime.Now <= endDate
                    && !SelectedSeason.IsLate90Applied)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Creates a one-line Note.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private void AddNote(Owner selectedOwner, string p)
        {
            Note note = new Note();
            StringBuilder sb = new StringBuilder();

            // Check to see if there is a WaterShutoff record for the Owner.  If there
            // is not, we will generate one here.  If there is, it is updated to reflect the
            // current past due status.
            if (p.Contains("30")
                || p.Contains("60")
                || p.Contains("90"))
            {

                sb.AppendFormat("Water Shutoff Status changed: {0} on {1:MM/dd/yyyy}", p, DateTime.Now);
            }
            else
            {
                sb.AppendFormat("{0}", p, DateTime.Now);
            }
            note.Comment = sb.ToString().Trim();
            note.OwnerID = selectedOwner.OwnerID;
            dc.Notes.InsertOnSubmit(note);
        }

        private void GenerateFinancialTransaction(Owner selectedOwner, decimal amount, string appliesTo, string comment)
        {
            FinancialTransaction transaction = new FinancialTransaction();

            decimal balance = (from x in dc.v_OwnerDetails
                               where x.OwnerID == selectedOwner.OwnerID
                               select x.Balance).FirstOrDefault();

            transaction.OwnerID = selectedOwner.OwnerID;
            transaction.FiscalYear = SelectedSeason.TimePeriod;
            transaction.DebitAmount = amount;
            transaction.Balance = balance + amount;
            transaction.TransactionDate = DateTime.Now;
            transaction.TransactionMethod = "Machine Generated";
            transaction.TransactionAppliesTo = appliesTo;
            transaction.Comment = comment;
            dc.FinancialTransactions.InsertOnSubmit(transaction);

            // Check to see if there is a WaterShutoff record for the Owner.  If there
            // is not, we will generate one here.  If there is, it is updated to reflect the
            // current past due status.
            //if (comment.Contains("30")
            //    || comment.Contains("60")
            //    || comment.Contains("90"))
            //{
            //    WaterShutoff wsOff = (from x in dc.WaterShutoffs
            //                          where x.OwnerID == selectedOwner.OwnerID
            //                          && x.IsResolved == false
            //                          select x).FirstOrDefault();

            //    // No WaterShutoff record found, so we will create one.
            //    if (null == wsOff)
            //    {
            //        WaterShutoff waterShutoff = new WaterShutoff();
            //        waterShutoff.OwnerID = selectedOwner.OwnerID;
            //        if (comment.Contains("30"))
            //        {
            //            waterShutoff.IsLate30 = true;
            //            waterShutoff.FirstNotificationDate = DateTime.Now;
            //            this.dc.WaterShutoffs.InsertOnSubmit(waterShutoff);
            //        }
            //    }
            //    // There is an existing record, so it needs to be updated.....
            //    else
            //    {
            //        if (comment.Contains("60"))
            //        {
            //            wsOff.IsLate60 = true;
            //            wsOff.IsMemberSuspended = true;
            //            wsOff.SuspensionDate = DateTime.Now;
            //            wsOff.SecondNotificationDate = DateTime.Now;
            //        }
            //        else
            //        {
            //            wsOff.IsLate90 = true;
            //            if (!wsOff.IsMemberSuspended)
            //            {
            //                wsOff.IsMemberSuspended = true;
            //                wsOff.SuspensionDate = DateTime.Now;
            //            }
            //            wsOff.IsShutoffNoticeIssued = true;
            //            wsOff.ShutoffNoticeIssuedDate = DateTime.Now;
            //        }
            //    }
            //    ChangeSet cs = dc.GetChangeSet();
            //    dc.SubmitChanges();
            //}
        }
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class AdministrationViewModel : CommonViewModel, ICommandSink
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
    public partial class AdministrationViewModel : CommonViewModel
    {
        /// <summary>
        /// View Notes about Properties
        /// </summary>
        private ICommand _graphGolfCommand;
        public ICommand GraphGolfCommand
        {
            get
            {
                return _graphGolfCommand ?? (_graphGolfCommand = new CommandHandler(() => GraphGolfAction(), true));
            }
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        /// <param name="type"></param>
        public void GraphGolfAction()
        {
            Host.Execute(HostVerb.Open, "Graph Facilities");
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        private ICommand _generateAnnualInvoicesCommand;
        public ICommand GenerateAnnualInvoicesCommand
        {
            get
            {
                return _generateAnnualInvoicesCommand ?? (_generateAnnualInvoicesCommand = new CommandHandler(() => GenerateAnnualInvoicesAction(), true));
            }
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        /// <param name="type"></param>
        public void GenerateAnnualInvoicesAction()
        {
            string fileName = string.Empty;
            var list = (from a in this.dc.v_OwnerDetails
                        select a);

            ObservableCollection<v_OwnerDetail> OwnersList = new ObservableCollection<v_OwnerDetail>(list);

            RaisePropertyChanged("IsBusy");
            foreach (v_OwnerDetail o in OwnersList)
            {
                fileName = string.Format(@"D:\Invoices\Invoice-{0}.PDF", o.OwnerID);
                Reports.AnnuaInvoices report = new Reports.AnnuaInvoices();
                report.Parameters["selectedOwner"].Value = o.OwnerID;
                report.CreateDocument();
                report.ExportToPdf(fileName);
            }
            RaisePropertyChanged("IsNotBusy");
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        private ICommand _displayInvoicesCommand;
        public ICommand DisplayInvoicesCommand
        {
            get
            {
                return _displayInvoicesCommand ?? (_displayInvoicesCommand = new CommandHandler(() => DisplayInvoicesAction(), true));
            }
        }

        /// <summary>
        /// View Notes about Properties
        /// </summary>
        /// <param name="type"></param>
        public void DisplayInvoicesAction()
        {
            Host.Execute(HostVerb.Open, "Invoices");
        }


        /// <summary>
        /// Apply annual dues, cart fees and assessments Command
        /// </summary>
        private ICommand _applyDuesCommand;
        public ICommand ApplyDuesCommand
        {
            get
            {
                return _applyDuesCommand ?? (_applyDuesCommand = new CommandHandler(() => ApplyDuesAction(), true));
            }
        }

        /// <summary>
        /// Apply annual dues command action
        /// </summary>
        /// <param name="type"></param>
        public void ApplyDuesAction()
        {
            DateTime now = DateTime.Now;

            string[] strings = SelectedSeason.TimePeriod.Split('-');
            int yyyy;
            Int32.TryParse(strings[0], out yyyy);
            DateTime startOfAnnual = new DateTime(yyyy, 5, 1, 0, 0, 0);
            //if (now.Year != startOfAnnual.Year
            //    || now.Month != startOfAnnual.Month
            //    || Math.Abs(now.Day - startOfAnnual.Day) > 3
            //    )
            //{
            //    MessageBox.Show("You cannot apply dues before May 1st, or after May 3rd");
            //}
            //else
            //{
            string msg = String.Format("Confirm you would like to make {0} the active year, and apply annual dues to all accounts.",
                SelectedSeason.TimePeriod);
            MessageBoxResult result = MessageBox.Show(msg, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (MessageBoxResult.OK == result)
            {
                IsBusy = true;
                CurrentSeason.IsCurrent = false;
                CurrentSeason.IsNext = false;
                CurrentSeason.IsVisible = false;

                var list = (from x in dc.Owners
                            where x.IsCurrentOwner == true
                            select x);
                Owners = new ObservableCollection<Owner>(list);
                foreach (Owner o in Owners)
                {
                    StringBuilder sb = new StringBuilder();
                    int propertyCount = o.Properties.Count();
                    int cartCount = o.GolfCarts.Count();
                    decimal amount = SelectedSeason.AnnualDues * propertyCount;
                    sb.AppendFormat("Dues:{0}", amount.ToString("C", CultureInfo.CurrentCulture));

                    // Check to see if Special Assessment needs to be applied.
                    decimal assessedAmt = 0m;
                    foreach (Property p in o.Properties)
                    {
                        if (p.IsAssessment)
                        {
                            assessedAmt += (decimal)SelectedSeason.AssessmentAmount;
                        }
                    }
                    if (assessedAmt > 0)
                    {
                        sb.AppendFormat(" {0} Asessment:{1}", SelectedSeason.Assessment, assessedAmt.ToString("C", CultureInfo.CurrentCulture));
                    }

                    if (cartCount > 0)
                    {
                        amount += SelectedSeason.CartFee * cartCount;
                        sb.AppendFormat(" GolfCart:{0}", (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));
                    }

                    string note = String.Format("Annual dues, assessments and cart fees of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
                    AddNote(o, note);
                    GenerateFinancialTransaction(o, amount, sb.ToString(), "Annual dues, assessments and cart fees applied");
                    ChangeSet cs = dc.GetChangeSet();
                }

                // Update the Seasons table so we move the settings forward for the current and next season
                SelectedSeason.IsCurrent = true;
                SelectedSeason.IsNext = false;
                SelectedSeason.IsDuesApplied = true;
                Seasons[SelectedSeasonIndex + 1].IsNext = true;
                Seasons[SelectedSeasonIndex + 1].IsVisible = true;
                IsBusy = false;

                dc.SubmitChanges();
                MessageBox.Show("Dues have been applied");
            }
            //}
        }

        /// <summary>
        /// Apply 30 Days Late fee Command
        /// </summary>
        private ICommand _applyLate30DaysCommand;
        public ICommand ApplyLate30DaysCommand
        {
            get
            {
                return _applyLate30DaysCommand ?? (_applyLate30DaysCommand = new CommandHandler(() => ApplyLate30DaysAction(), true));
            }
        }

        /// <summary>
        /// 30 Day Late command action
        /// </summary>
        /// <param name="type"></param>
        public void ApplyLate30DaysAction()
        {
            DateTime now = DateTime.Now;

            string[] strings = SelectedSeason.TimePeriod.Split('-');
            int yyyy;
            Int32.TryParse(strings[0], out yyyy);
            DateTime startOfAnnual = new DateTime(yyyy, 5, 1, 0, 0, 0);
            if (!IsApply30DayLate)
            {
                MessageBox.Show("You cannot apply fees before June 1st, or after June 7th");
            }
            else
            {
                IsBusy = true;

                // Get the list of Owners who are now 30 days late paying dues.  If they owe
                // $100 or less, we let them go for now.
                var list = (from x in this.dc.v_OwnerDetails
                            where x.Balance > (CurrentSeason.AnnualDues - 100.00m)
                            select x);

                foreach (v_OwnerDetail ownerDetail in list)
                {
                    Owner owner = (from x in dc.Owners
                                   where x.OwnerID == ownerDetail.OwnerID
                                   select x).FirstOrDefault();

                    StringBuilder sb = new StringBuilder();
                    decimal amount = 20.00m;
                    sb.AppendFormat("LateFee:{0}", amount.ToString("C", CultureInfo.CurrentCulture));

                    // If they haven't paid their assessment.....
                    //if (assessment == true)
                    //{
                    //    amount += SelectedSeason.AssessmentAmount * propertyCount;
                    //    sb.AppendFormat(" {0} Assessment:{1}", SelectedSeason.Assessment, (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));
                    //}

                    string note = String.Format("30 Day Late Fee of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
                    AddNote(owner, note);
                    GenerateFinancialTransaction(owner, amount, sb.ToString(), "Fee applied: 30 days late");
                    Late30Day late30 = new Late30Day();
                    late30.OwnerID = ownerDetail.OwnerID;
                    late30.MailTo = ownerDetail.MailTo;
                    late30.Season = CurrentSeason.TimePeriod;
                    dc.Late30Days.InsertOnSubmit(late30);

                    ChangeSet cs = dc.GetChangeSet();
                    this.dc.SubmitChanges();

                    string fileName = string.Format(@"D:\LateNotices\30Day\30Day-{0}.PDF", ownerDetail.OwnerID);
                    Reports.PastDue30Days report = new Reports.PastDue30Days();
                    report.Parameters["OwnerID"].Value = ownerDetail.OwnerID;
                    report.Parameters["InvoiceDate"].Value = DateTime.Now;
                    report.CreateDocument();
                    report.ExportToPdf(fileName);
                }

                SelectedSeason.IsLate30Applied = true;
                this.dc.SubmitChanges();
                MessageBox.Show("Fees have been applied");
            }

            IsBusy = false;
        }

        /// <summary>
        /// Apply 60 Days Late fee Command
        /// </summary>
        private ICommand _applyLate60DaysCommand;
        public ICommand ApplyLate60DaysCommand
        {
            get
            {
                return _applyLate60DaysCommand ?? (_applyLate60DaysCommand = new CommandHandler(() => ApplyLate60DaysAction(), true));
            }
        }

        /// <summary>
        /// 60 Day Late command action
        /// </summary>
        /// <param name="type"></param>
        public void ApplyLate60DaysAction()
        {
            if (!IsApply60DayLate)
            {
                MessageBox.Show("You cannot apply fees before July 1st, or after July 7th");
            }
            else
            {
                IsBusy = true;

                var list = (from x in this.dc.v_OwnerDetails
                            where x.Balance > 0
                            select x);
                foreach (v_OwnerDetail ownerDetail in list)
                {
                    Owner owner = (from x in dc.Owners
                                   where x.OwnerID == ownerDetail.OwnerID
                                   select x).FirstOrDefault();

                    StringBuilder sb = new StringBuilder();
                    decimal amount = 20.00m;
                    sb.AppendFormat("LateFee:{0}", amount.ToString("C", CultureInfo.CurrentCulture));
                    //if (assessment == true)
                    //{
                    //    amount += SelectedSeason.AssessmentAmount * propertyCount;
                    //    sb.AppendFormat(" {0} Assessment:{1}", SelectedSeason.Assessment, (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));
                    //}

                    string note = String.Format("60 Day Late Fee of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
                    AddNote(owner, note);
                    GenerateFinancialTransaction(owner, amount, sb.ToString(), "Fee applied: 60 days late");
                    Late60Day late60 = new Late60Day();
                    late60.OwnerID = ownerDetail.OwnerID;
                    late60.Season = CurrentSeason.TimePeriod;
                    dc.Late60Days.InsertOnSubmit(late60);

                    ChangeSet cs = dc.GetChangeSet();
                    this.dc.SubmitChanges();

                    string fileName = string.Format(@"D:\LateNotices\60Day\60Day-{0}.PDF", ownerDetail.OwnerID);
                    Reports.PastDue60Days report = new Reports.PastDue60Days();
                    report.Parameters["OwnerID"].Value = ownerDetail.OwnerID;
                    report.Parameters["InvoiceDate"].Value = DateTime.Now;
                    report.CreateDocument();
                    report.ExportToPdf(fileName);
                }

                IsBusy = false;

                SelectedSeason.IsLate60Applied = true;
                this.dc.SubmitChanges();
                MessageBox.Show("Fees have been applied");
            }
        }

        /// <summary>
        /// Apply 90 Days Late fee Command
        /// </summary>
        private ICommand _applyLate90DaysCommand;
        public ICommand ApplyLate90DaysCommand
        {
            get
            {
                return _applyLate90DaysCommand ?? (_applyLate90DaysCommand = new CommandHandler(() => ApplyLate90DaysAction(), true));
            }
        }

        /// <summary>
        /// 90 Day Late command action
        /// </summary>
        /// <param name="type"></param>
        public void ApplyLate90DaysAction()
        {
            if (!IsApply90DayLate)
            {
                MessageBox.Show("You cannot apply fees before Aug 1st, or after Aug 7th");
            }
            else
            {
                IsBusy = true;

                var list = (from x in this.dc.v_OwnerDetails
                            where x.Balance > 0
                            select x);
                foreach (v_OwnerDetail ownerDetail in list)
                {
                    Owner owner = (from x in dc.Owners
                                   where x.OwnerID == ownerDetail.OwnerID
                                   select x).FirstOrDefault();

                    StringBuilder sb = new StringBuilder();
                    decimal amount = 20.00m;
                    sb.AppendFormat("LateFee:{0}", amount.ToString("C", CultureInfo.CurrentCulture));
                    //if (assessment == true)
                    //{
                    //    amount += SelectedSeason.AssessmentAmount * propertyCount;
                    //    sb.AppendFormat(" {0} Assessment:{1}", SelectedSeason.Assessment, (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));
                    //}

                    string note = String.Format("90 Day Late Fee of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
                    AddNote(owner, note);
                    GenerateFinancialTransaction(owner, amount, sb.ToString(), "Fee applied: 90 days late");
                    Late90Day late90 = new Late90Day();
                    late90.OwnerID = ownerDetail.OwnerID;
                    late90.Season = CurrentSeason.TimePeriod;
                    dc.Late90Days.InsertOnSubmit(late90);

                    ChangeSet cs = dc.GetChangeSet();
                    this.dc.SubmitChanges();

                    string fileName = string.Format(@"D:\LateNotices\90Day\90Day-{0}.PDF", ownerDetail.OwnerID);
                    Reports.PastDue60Days report = new Reports.PastDue60Days();
                    report.Parameters["OwnerID"].Value = ownerDetail.OwnerID;
                    report.Parameters["InvoiceDate"].Value = DateTime.Now;
                    report.CreateDocument();
                    report.ExportToPdf(fileName);
                }
                IsBusy = false;

                SelectedSeason.IsLate90Applied = true;
                this.dc.SubmitChanges();
                MessageBox.Show("Fees have been applied");
            }
        }
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class AdministrationViewModel : IDisposable
    public partial class AdministrationViewModel : IDisposable
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
        ~AdministrationViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
