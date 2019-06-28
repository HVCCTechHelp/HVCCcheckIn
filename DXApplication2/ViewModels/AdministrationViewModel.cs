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
    using System.IO;
    using HVCC.Shell.Models.Financial;
    using HVCC.Shell.Common.Converters;
    using DevExpress.XtraReports.UI;

    public partial class AdministrationViewModel : CommonViewModel, ICommandSink
    {
        public string UserName
        {
            get
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public AdministrationViewModel(IDataContext dc, IDataContext pDC = null)
        {
            IsBusy = false;
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

        private DateTime MayFirst
        {
            get
            {
                int year = DateTime.Now.Year;
                DateTime mayFirst = new DateTime(year, 5, 1);
                return mayFirst;
            }
        }

        public ObservableCollection<Owner> Owners { get; set; }

        private Invoice _theInvoice = null;
        public Invoice TheInvoice
        {
            get { return _theInvoice; }
            set
            {
                if (_theInvoice != value)
                {
                    _theInvoice = value;
                }
            }
        }

        public bool IsApply30DayLate
        {
            get
            {
                string[] strings = SelectedSeason.TimePeriod.Split('-');
                int yyyy;
                Int32.TryParse(strings[0], out yyyy);
                DateTime startDate = new DateTime(yyyy, 5, 1, 0, 0, 0);
                DateTime endDate = new DateTime(yyyy, 5, 15, 0, 0, 0);
                if (DateTime.Now >= startDate
                    && DateTime.Now <= endDate
                    && !SelectedSeason.IsLate30Applied)
                {
                    return true;
                }
                return false;
                //return true;
            }
        }
        public bool IsApply60DayLate
        {
            get
            {
                string[] strings = SelectedSeason.TimePeriod.Split('-');
                int yyyy;
                Int32.TryParse(strings[0], out yyyy);
                DateTime startDate = new DateTime(yyyy, 5, 31, 0, 0, 0);
                DateTime endDate = new DateTime(yyyy, 6, 15, 0, 0, 0);
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
                DateTime startDate = new DateTime(yyyy, 7, 1, 0, 0, 0);
                DateTime endDate = new DateTime(yyyy, 7, 15, 0, 0, 0);
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
        private int? AddNote(Owner selectedOwner, string p)
        {
            int? noteID = null;
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

            try
            {
                dc.usp_InsertNote(
                        note.OwnerID,
                        note.Comment,
                        ref noteID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return noteID;
        }

        private void GenerateInvoice(Owner selectedOwner)
        {
            TheInvoice = new Invoice();
            TheInvoice.LastModifiedBy = UserName;
            TheInvoice.LastModified = DateTime.Now;
            TheInvoice.InvoiceItems = new ObservableCollection<InvoiceItem>();
            TheInvoice.GUID = Guid.NewGuid();
            TheInvoice.OwnerID = selectedOwner.OwnerID;
            TheInvoice.IssuedDate = DateTime.Now;
            TheInvoice.DueDate = MayFirst;
            TheInvoice.TermsDays = -1;  /// -1 Indicates "Due by May 1st"
            TheInvoice.PaymentsApplied = 0;
            TheInvoice.IsPaid = false;
            TheInvoice.Amount = CurrentSeason.AnnualDues * -1;
            TheInvoice.BalanceDue = Math.Abs(TheInvoice.Amount);

            /// Create the InvoiceItem
            /// 
            InvoiceItem invoiceItem = new InvoiceItem();
            StringBuilder description = new StringBuilder();
            description.AppendLine("Membership dues for May 1 to Apr 30");
            description.Append("Lot(s)# ");
            foreach (Property p in selectedOwner.Properties)
            {
                description.Append(p.Customer);
                if (selectedOwner.Properties.Count() > 1)
                {
                    description.Append(", ");
                }
            }
            /// Remove the trailing comma from the string.
            /// 
            char[] remove = { ' ', ',' }; ;

            invoiceItem.Item = String.Format("Dues {0}", CurrentSeason.TimePeriod);
            invoiceItem.Description = description.ToString().TrimEnd(remove);
            invoiceItem.Quanity = selectedOwner.Properties.Count();
            invoiceItem.Rate = SelectedSeason.AnnualDues;
            invoiceItem.Amount = invoiceItem.Quanity * invoiceItem.Rate;
            TheInvoice.InvoiceItems.Add(invoiceItem);

            /// Check to see if the owner purchased a cart sticker for the previous season.  
            /// We use 'CurrentSeason' as the previous because we haven't made the new season (SelectedSeason) the active
            /// season yet.
            /// 
            int cartCount = (from x in selectedOwner.GolfCarts
                             where x.Year == CurrentSeason.TimePeriod
                             select x.Quanity).FirstOrDefault();

            if (0 < cartCount)
            {
                invoiceItem = new InvoiceItem();
                invoiceItem.Item = "Golf Cart Sticker";
                invoiceItem.Description = "Annual fee for Golf Cart Sticker";
                invoiceItem.Quanity = cartCount;
                invoiceItem.Rate = SelectedSeason.AnnualDues;
                invoiceItem.Amount = invoiceItem.Quanity * invoiceItem.Rate;
                TheInvoice.InvoiceItems.Add(invoiceItem);
            }
            /// Seralize the InvoiceItems collection to an XML string.
            /// 
            var xmlString = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
            TheInvoice.ItemDetails = xmlString;

            dc.Invoices.InsertOnSubmit(TheInvoice);
            dc.SubmitChanges();
        }

        //private int? GenerateFinancialTransaction(Owner selectedOwner, decimal amount, string appliesTo, string comment)
        //{
        //    int? transactionID = null;
        //    FinancialTransaction transaction = new FinancialTransaction();
        //    try
        //    {

        //        decimal? balance = (from x in dc.v_OwnerDetails
        //                            where x.OwnerID == selectedOwner.OwnerID
        //                            select x.Balance).FirstOrDefault();

        //        transaction.OwnerID = selectedOwner.OwnerID;
        //        transaction.FiscalYear = SelectedSeason.TimePeriod;
        //        transaction.DebitAmount = amount;
        //        transaction.Balance = (decimal)balance + amount;
        //        transaction.TransactionDate = DateTime.Now;
        //        transaction.TransactionMethod = "Machine Generated";
        //        transaction.TransactionAppliesTo = appliesTo;
        //        transaction.Comment = comment;

        //        dc.usp_InsertFinancialTransaction(
        //                    transaction.OwnerID,
        //                    transaction.FiscalYear,
        //                    transaction.Balance,
        //                    transaction.CreditAmount,
        //                    transaction.DebitAmount,
        //                    transaction.TransactionDate,
        //                    transaction.TransactionMethod,
        //                    transaction.TransactionAppliesTo,
        //                    transaction.Comment,
        //                    transaction.CheckNumber,
        //                    transaction.ReceiptNumber,
        //                    ref transactionID
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error saving", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //    return transactionID;

        //    // Check to see if there is a WaterShutoff record for the Owner.  If there
        //    // is not, we will generate one here.  If there is, it is updated to reflect the
        //    // current past due status.
        //    //if (comment.Contains("30")
        //    //    || comment.Contains("60")
        //    //    || comment.Contains("90"))
        //    //{
        //    //    WaterShutoff wsOff = (from x in dc.WaterShutoffs
        //    //                          where x.OwnerID == selectedOwner.OwnerID
        //    //                          && x.IsResolved == false
        //    //                          select x).FirstOrDefault();

        //    //    // No WaterShutoff record found, so we will create one.
        //    //    if (null == wsOff)
        //    //    {
        //    //        WaterShutoff waterShutoff = new WaterShutoff();
        //    //        waterShutoff.OwnerID = selectedOwner.OwnerID;
        //    //        if (comment.Contains("30"))
        //    //        {
        //    //            waterShutoff.IsLate30 = true;
        //    //            waterShutoff.FirstNotificationDate = DateTime.Now;
        //    //            this.dc.WaterShutoffs.InsertOnSubmit(waterShutoff);
        //    //        }
        //    //    }
        //    //    // There is an existing record, so it needs to be updated.....
        //    //    else
        //    //    {
        //    //        if (comment.Contains("60"))
        //    //        {
        //    //            wsOff.IsLate60 = true;
        //    //            wsOff.IsMemberSuspended = true;
        //    //            wsOff.SuspensionDate = DateTime.Now;
        //    //            wsOff.SecondNotificationDate = DateTime.Now;
        //    //        }
        //    //        else
        //    //        {
        //    //            wsOff.IsLate90 = true;
        //    //            if (!wsOff.IsMemberSuspended)
        //    //            {
        //    //                wsOff.IsMemberSuspended = true;
        //    //                wsOff.SuspensionDate = DateTime.Now;
        //    //            }
        //    //            wsOff.IsShutoffNoticeIssued = true;
        //    //            wsOff.ShutoffNoticeIssuedDate = DateTime.Now;
        //    //        }
        //    //    }
        //    //    ChangeSet cs = dc.GetChangeSet();
        //    //    dc.SubmitChanges();
        //    //}
        //}
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
            ///
            /// See Stored Procedure: 	[dbo].[usp_GetInvoiceForOwner]  
            /// 
            string fileName = string.Empty;
            var list = (from a in this.dc.v_OwnerDetails
                        select a);

            ObservableCollection<v_OwnerDetail> OwnersList = new ObservableCollection<v_OwnerDetail>(list);

            RaisePropertyChanged("IsBusy");
            foreach (v_OwnerDetail o in OwnersList)
            {
                if (true == o.IsCurrentOwner)
                {
                    fileName = string.Format(@"D:\Invoices\Invoice-{0}.PDF", o.OwnerID);
                    Reports.AnnuaInvoices report = new Reports.AnnuaInvoices();
                    report.Parameters["selectedOwner"].Value = o.OwnerID;
                    report.Parameters["previousYear"].Value = CurrentSeason.TimePeriod;
                    report.CreateDocument();
                    report.ExportToPdf(fileName);
                }
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

                var currentOwnerList = (from x in dc.Owners
                                        where x.IsCurrentOwner == true
                                        select x);
                Owners = new ObservableCollection<Owner>(currentOwnerList);

                int propertyCount = 0;
                int cartCount = 0;

                using (var StreamWriter = new StreamWriter(@"D:\Invoices\InvoiceList.csv"))
                {
                    StringBuilder sb2 = new StringBuilder();
                    sb2.Append("OwnerID|Lot1|Lot2|Lot3|Lot4|Lot5|Lot6|Assessment|GolfCart|Total");
                    StreamWriter.WriteLine(sb2.ToString());

                    foreach (Owner owner in Owners)
                    {
                        /// See Stored Procedure: 	[dbo].[usp_GetInvoiceForOwner]  
                        /// 
                        string fileName = string.Empty;
                        if (true == owner.IsCurrentOwner)
                        {
                            fileName = string.Format(@"D:\Invoices\Invoice-{0}.PDF", owner.OwnerID);
                            Reports.AnnuaInvoices report = new Reports.AnnuaInvoices();
                            report.Parameters["selectedOwner"].Value = owner.OwnerID;
                            report.Parameters["previousYear"].Value = CurrentSeason.TimePeriod;
                            report.CreateDocument();
                            report.ExportToPdf(fileName);
                        }

                        propertyCount = 0;
                        cartCount = 0;
                        StringBuilder sb = new StringBuilder();

                        /// StringBuilder2 is used to create a CSV file of invoice information....
                        /// 
                        sb2.Clear();
                        sb2.AppendFormat("{0}|", owner.OwnerID.ToString().PadLeft(6, '0'));

                        propertyCount = owner.Properties.Count();
                        decimal amount = SelectedSeason.AnnualDues * propertyCount;
                        sb.AppendFormat("Dues:{0}", amount.ToString("C", CultureInfo.CurrentCulture));

                        /// Check to see if Special Assessment needs to be applied.
                        /// 
                        decimal assessedAmt = 0m;
                        foreach (Property p in owner.Properties)
                        {
                            sb2.AppendFormat("{0}|", p.Customer);
                            if (p.IsAssessment)
                            {
                                assessedAmt += (decimal)SelectedSeason.AssessmentAmount;
                            }
                        }
                        if (assessedAmt > 0)
                        {
                            sb.AppendFormat(" {0} Asessment:{1}", SelectedSeason.Assessment, assessedAmt.ToString("C", CultureInfo.CurrentCulture));
                            sb2.AppendFormat("{0}|", assessedAmt.ToString("C", CultureInfo.CurrentCulture));
                        }
                        else
                        {
                            sb2.Append("|");
                        }

                        /// To ensure our CSV file has the same number of columns for every row, we have to null
                        /// pad the row upto (6) properties worth of columns.
                        /// 
                        for (int i = 0; i < (6 - propertyCount); i++)
                        {
                            sb2.Append("|");
                        }

                        ///
                        /// Check to see if the owner purchased a cart sticker for the previous season.  
                        /// We use 'CurrentSeason' as the previous because we haven't made the new season (SelectedSeason) the active
                        /// season yet.
                        /// 
                        cartCount = (from x in owner.GolfCarts
                                     where x.Year == CurrentSeason.TimePeriod
                                     select x.Quanity).FirstOrDefault();

                        if (0 < cartCount)
                        {
                            amount += SelectedSeason.CartFee * cartCount;
                            sb.AppendFormat(" GolfCart:{0}", (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));
                            sb2.AppendFormat("{0}|", (SelectedSeason.CartFee * cartCount));
                        }
                        else
                        {
                            /// If they don't own a cart, we have to pad the columns
                            /// 
                            sb2.Append("|");
                        }

                        /// Create the transaction note, then add the Note and Invoice transaction to the database
                        /// 
                        string note = String.Format("Annual dues, assessments and cart fees of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
                        AddNote(owner, note);
                        GenerateInvoice(owner);
                        //GenerateFinancialTransaction(o, amount, sb.ToString(), "Annual dues, assessments and cart fees applied");

                        ///
                        /// Add the total amount of the invoice and write the full line to the output file
                        /// 
                        decimal? newTotalDue = (from x in dc.v_OwnerDetails
                                                where x.OwnerID == owner.OwnerID
                                                select x.Balance).FirstOrDefault();

                        sb2.AppendFormat("{0}", newTotalDue.ToString());
                        StreamWriter.WriteLine(sb2.ToString());
                    }
                    //ChangeSet cs = dc.GetChangeSet();
                    ///
                    /// Close the spreadsheet file.....
                    /// 
                    StreamWriter.Close();
                }

                ///
                /// Update the Seasons table so we move the settings forward for the current and next season
                /// 
                SelectedSeason.IsCurrent = true;
                SelectedSeason.IsNext = false;
                SelectedSeason.IsDuesApplied = true;
                Seasons[SelectedSeasonIndex + 1].IsNext = true;
                Seasons[SelectedSeasonIndex + 1].IsVisible = true;
                IsBusy = false;

                dc.SubmitChanges();
                MessageBox.Show("Dues have been applied");
            }
        }

        /// <summary>
        /// Apply 30 Days Late fee Command
        /// </summary>
        private ICommand _applyLate30DaysCommand;
        public ICommand ApplyLate30DaysCommand
        {
            get
            {
                return _applyLate30DaysCommand ?? (_applyLate30DaysCommand = new CommandHandlerWparm((object parameter) => ApplyLateFeeAction(parameter), true));
            }
        }
        /// <summary>
        /// Apply 60 Days Late fee Command
        /// </summary>
        private ICommand _applyLate60DaysCommand;
        public ICommand ApplyLate60DaysCommand
        {
            get
            {
                return _applyLate60DaysCommand ?? (_applyLate60DaysCommand = new CommandHandlerWparm((object parameter) => ApplyLateFeeAction(parameter), true));
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
                return _applyLate90DaysCommand ?? (_applyLate90DaysCommand = new CommandHandlerWparm((object parameter) => ApplyLateFeeAction(parameter), true));
            }
        }

        /// <summary>
        /// Apply Late Fee command action
        /// </summary>
        /// <param name="type"></param>
        public void ApplyLateFeeAction(object parameter)
        {
            int daysLate = Convert.ToInt32(parameter);
            int? noteID = null;
            decimal amount = 20.00m;
            DateTime now = DateTime.Now;

            string[] strings = SelectedSeason.TimePeriod.Split('-');
            int yyyy;
            Int32.TryParse(strings[0], out yyyy);
            DateTime startOfAnnual = new DateTime(yyyy, 5, 1, 0, 0, 0);

            IsBusy = true;

            /// Get the list of Owners who are now late paying dues. 
            /// 
            var list = (from p in this.dc.Invoices
                        where p.ItemDetails.Contains("Dues")
                        && p.IsPaid == false
                        && p.DueDate == startOfAnnual
                        select p);

            int count = list.Count();

            using (var StreamWriter = new StreamWriter(@"D:\LateNotices\"+ daysLate.ToString()+"Days-Late.csv"))
            {
                StringBuilder sb2 = new StringBuilder();
                sb2.Append("OwnerID|MailTo|Address|Address2|City|State|Zip");
                StreamWriter.WriteLine(sb2.ToString());
                foreach (Invoice invoice in list)
                {
                    Owner owner = (from o in dc.Owners
                                   where o.OwnerID == invoice.OwnerID
                                   select o).FirstOrDefault();

                    /// This is a bit of a hack.....  But the condition exists where a ChangeOwner
                    /// results in unpaid invoces for an inactive owner.  As a cleanup, we check
                    /// for inactive owners and delete any lingering invoices.  In reality, the invoices
                    /// were paid, but staff did not enter the payment before inactivating the owner.
                    /// 
                    if (false == owner.IsCurrentOwner)
                    {
                        var nix = (from p in this.dc.Invoices
                                   where p.OwnerID == owner.OwnerID
                                   && p.IsPaid == false
                                   select p);

                        dc.Invoices.DeleteAllOnSubmit(nix);
                        owner.AccountBalance = 0;
                        ChangeSet cs = dc.GetChangeSet();
                        dc.SubmitChanges();
                    }
                    /// The else-if 'true' is just here to allow for debugging.  If you want to
                    /// avoid applying late fees, just change 'true' to 'null'
                    /// 
                    else if (true == owner.IsCurrentOwner)
                    {

                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat("LateFee:{0}", amount.ToString("C", CultureInfo.CurrentCulture));

                        string note = String.Format("{0} Day Late Fee of {1} applied", daysLate.ToString(), amount.ToString("C", CultureInfo.CurrentCulture));

                        /// Generate a new invoice for the late fee.
                        /// 
                        TheInvoice = new Invoice();
                        TheInvoice.LastModifiedBy = UserName;
                        TheInvoice.LastModified = DateTime.Now;
                        TheInvoice.GUID = Guid.NewGuid();
                        TheInvoice.OwnerID = owner.OwnerID;
                        TheInvoice.IssuedDate = now;
                        TheInvoice.DueDate = now;
                        TheInvoice.TermsDays = 0;  /// -1 Indicates "Due by May 1st", 0 = "Due Now"
                        TheInvoice.TermsDescriptive = "Due Now";
                        TheInvoice.Amount = amount;
                        TheInvoice.BalanceDue = amount;
                        TheInvoice.PaymentsApplied = 0;
                        TheInvoice.IsPaid = false;
                        TheInvoice.Memo = note;
                        TheInvoice.InvoiceItems = new ObservableCollection<InvoiceItem>();
                        string desc = String.Format("{0} Day Late Fee", daysLate.ToString());
                        TheInvoice.InvoiceItems.Add(new InvoiceItem { Item = "Late Payment Fee", Description = desc, Quanity = 1, Rate = amount }
                            );

                        /// Seralize the InvoiceItems collection to an XML string.
                        /// 
                        var xmlString = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
                        TheInvoice.ItemDetails = xmlString;


                        /// Update Owner Account Balance
                        /// 
                        owner.AccountBalance = Financial.GetAccountBalance(owner);
                        /// Update the Owner Record and add the new invoice
                        /// 
                        owner.Invoices.Add(TheInvoice);
                        owner.LastModifiedBy = UserName;
                        owner.LastModified = DateTime.Now;

                        /// Create a LatePayment record
                        /// 
                        LatePayment late = new LatePayment();
                        late.LastModified = DateTime.Now;
                        late.LastModifiedBy = UserName;
                        late.OwnerID = owner.OwnerID;
                        late.Season = CurrentSeason.TimePeriod;
                        if (30 == daysLate)
                        {
                            late.Is30Late = true;
                            SelectedSeason.IsLate30Applied = true;
                        }
                        if (60 == daysLate)
                        {
                            late.Is60Late = true;
                            SelectedSeason.IsLate60Applied = true;
                        }
                        if (90 == daysLate)
                        {
                            late.Is90Late = true;
                            SelectedSeason.IsLate90Applied = true;
                        }

                        owner.AccountBalance += TheInvoice.Amount;
                        dc.LatePayments.InsertOnSubmit(late);
                        noteID = AddNote(owner, note);
                        ChangeSet cs = dc.GetChangeSet();
                        this.dc.SubmitChanges();

                        /// Generate the PDF Report
                        /// 
                        XtraReport report = null;
                        string fileName = string.Format(@"D:\LateNotices\{0}Day\{0}Day-{1}.PDF", daysLate.ToString(), owner.OwnerID);
                        if (30 == daysLate)
                        {
                            report = new Reports.PastDue30Days();
                        }
                        if (60 == daysLate)
                        {
                            report = new Reports.PastDue60Days();
                        }
                        if (90 == daysLate)
                        {
                            report = new Reports.PastDue90Days();
                        }
                        report.Parameters["OwnerID"].Value = owner.OwnerID;
                        report.Parameters["Balance"].Value = owner.AccountBalance;
                        report.Parameters["InvoiceID"].Value = TheInvoice.TransactionID;
                        report.Parameters["InvoiceDate"].Value = DateTime.Now;
                        report.CreateDocument();
                        report.ExportToPdf(fileName);

                        /// StringBuilder2 is used to create a CSV file of invoice information....
                        /// 
                        sb2.Clear();
                        sb2.AppendFormat("{0}|{1}|{2}|{3}|{4}|{5}|{6}"
                            , owner.OwnerID.ToString().PadLeft(6, '0'), owner.MailTo, owner.Address, owner.Address2, owner.City, owner.State, owner.Zip);
                        StreamWriter.WriteLine(sb2.ToString());
                    }
                }
                StreamWriter.Close();
            }

            MessageBox.Show("Late Fees have been applied");
            //}

            IsBusy = false;
        }

        /// <summary>
        /// Apply 60 Days Late fee Command
        /// </summary>
        //private ICommand _applyLate60DaysCommand;
        //public ICommand ApplyLate60DaysCommand
        //{
        //    get
        //    {
        //        return _applyLate60DaysCommand ?? (_applyLate60DaysCommand = new CommandHandler(() => ApplyLate60DaysAction(), true));
        //    }
        //}

        /// <summary>
        /// 60 Day Late command action
        /// </summary>
        /// <param name="type"></param>
        //public void ApplyLate60DaysAction()
        //{
        //    int? transactionID = null;
        //    int? noteID = null;

        //    if (!IsApply60DayLate)
        //    {
        //        MessageBox.Show("You cannot apply fees before July 1st, or after July 7th");
        //    }
        //    else
        //    {
        //        IsBusy = true;

        //        var list = (from x in this.dc.v_OwnerDetails
        //                    where x.Balance > 100.00m
        //                    select x);
        //        foreach (v_OwnerDetail ownerDetail in list)
        //        {
        //            Owner owner = (from x in dc.Owners
        //                           where x.OwnerID == ownerDetail.OwnerID
        //                           select x).FirstOrDefault();

        //            StringBuilder sb = new StringBuilder();
        //            decimal amount = 20.00m;
        //            sb.AppendFormat("LateFee:{0}", amount.ToString("C", CultureInfo.CurrentCulture));
        //            //if (assessment == true)
        //            //{
        //            //    amount += SelectedSeason.AssessmentAmount * propertyCount;
        //            //}
        //            //sb.AppendFormat(" {0} Assessment:{1}", SelectedSeason.Assessment, (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));

        //            string note = String.Format("60 Day Late Fee of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
        //            noteID = AddNote(owner, note);
        //            //transactionID = GenerateFinancialTransaction(owner, amount, sb.ToString(), "Fee applied: 90 days late");

        //            /// Add the XRef record Transaction <-> Note
        //            /// 
        //            // --OLD--
        //            //try
        //            //{
        //            //    TransactionXNote tXn = new TransactionXNote();
        //            //    int? tXnID = null;
        //            //    dc.usp_InsertTransactionXNote(
        //            //        transactionID,
        //            //        noteID,
        //            //        ref tXnID);
        //            //}
        //            //catch (Exception ex)
        //            //{
        //            //    MessageBox.Show("Error saving", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            //}

        //            // Generate the PDF Report
        //            Late60Day late60 = new Late60Day();
        //            late60.OwnerID = (int)ownerDetail.OwnerID;
        //            late60.Season = CurrentSeason.TimePeriod;
        //            dc.Late60Days.InsertOnSubmit(late60);

        //            ChangeSet cs = dc.GetChangeSet();
        //            this.dc.SubmitChanges();

        //            string fileName = string.Format(@"D:\LateNotices\60Day\60Day-{0}.PDF", ownerDetail.OwnerID);
        //            Reports.PastDue60Days report = new Reports.PastDue60Days();
        //            report.Parameters["OwnerID"].Value = ownerDetail.OwnerID;
        //            report.Parameters["InvoiceDate"].Value = DateTime.Now;
        //            report.CreateDocument();
        //            report.ExportToPdf(fileName);
        //        }

        //        IsBusy = false;

        //        SelectedSeason.IsLate60Applied = true;
        //        this.dc.SubmitChanges();
        //        MessageBox.Show("Fees have been applied");
        //    }
        //}

        /// <summary>
        /// Apply 90 Days Late fee Command
        /// </summary>
        //private ICommand _applyLate90DaysCommand;
        //public ICommand ApplyLate90DaysCommand
        //{
        //    get
        //    {
        //        return _applyLate90DaysCommand ?? (_applyLate90DaysCommand = new CommandHandler(() => ApplyLate90DaysAction(), true));
        //    }
        //}

        /// <summary>
        /// 90 Day Late command action
        /// </summary>
        /// <param name="type"></param>
        //public void ApplyLate90DaysAction()
        //{
        //    int? transactionID = null;
        //    int? noteID = null;

        //    if (!IsApply90DayLate)
        //    {
        //        MessageBox.Show("You cannot apply fees before Aug 1st, or after Aug 7th");
        //    }
        //    else
        //    {
        //        IsBusy = true;

        //        var list = (from x in this.dc.v_OwnerDetails
        //                    where x.Balance > 100.00m
        //                    select x);
        //        foreach (v_OwnerDetail ownerDetail in list)
        //        {
        //            Owner owner = (from x in dc.Owners
        //                           where x.OwnerID == ownerDetail.OwnerID
        //                           select x).FirstOrDefault();

        //            StringBuilder sb = new StringBuilder();
        //            decimal amount = 20.00m;
        //            sb.AppendFormat("LateFee:{0}", amount.ToString("C", CultureInfo.CurrentCulture));
        //            //if (assessment == true)
        //            //{
        //            //    amount += SelectedSeason.AssessmentAmount * propertyCount;
        //            //    sb.AppendFormat(" {0} Assessment:{1}", SelectedSeason.Assessment, (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));
        //            //}

        //            string note = String.Format("90 Day Late Fee of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
        //            noteID = AddNote(owner, note);
        //            //transactionID = GenerateFinancialTransaction(owner, amount, sb.ToString(), "Fee applied: 90 days late");

        //            /// Add the XRef record Transaction <-> Note
        //            /// 
        //            //--OLD--
        //            //try
        //            //{
        //            //    TransactionXNote tXn = new TransactionXNote();
        //            //    int? tXnID = null;
        //            //    dc.usp_InsertTransactionXNote(
        //            //        transactionID,
        //            //        noteID,
        //            //        ref tXnID);
        //            //}
        //            //catch (Exception ex)
        //            //{
        //            //    MessageBox.Show("Error saving", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //            //}

        //            // Generate the PDF report
        //            Late90Day late90 = new Late90Day();
        //            late90.OwnerID = (int)ownerDetail.OwnerID;
        //            late90.Season = CurrentSeason.TimePeriod;
        //            dc.Late90Days.InsertOnSubmit(late90);

        //            ChangeSet cs = dc.GetChangeSet();
        //            this.dc.SubmitChanges();

        //            string fileName = string.Format(@"D:\LateNotices\90Day\90Day-{0}.PDF", ownerDetail.OwnerID);
        //            Reports.PastDue90Days report = new Reports.PastDue90Days();
        //            report.Parameters["OwnerID"].Value = ownerDetail.OwnerID;
        //            report.Parameters["InvoiceDate"].Value = DateTime.Now;
        //            report.CreateDocument();
        //            report.ExportToPdf(fileName);
        //        }
        //        IsBusy = false;

        //        SelectedSeason.IsLate90Applied = true;
        //        this.dc.SubmitChanges();
        //        MessageBox.Show("Fees have been applied");
        //    }
        //}
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
