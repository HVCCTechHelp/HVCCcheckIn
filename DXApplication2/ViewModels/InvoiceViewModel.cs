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
    using HVCC.Shell.Common.Commands;
    using System.Collections.Generic;
    using HVCC.Shell.Common.Converters;
    using HVCC.Shell.Models.Financial;
    using static HVCC.Shell.Common.Resources.Enumerations;

    public partial class InvoiceViewModel : CommonViewModel, ICommandSink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public InvoiceViewModel(IDataContext dc, IDataContext pDC = null)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            this.RegisterCommands();

            var permissions = (from x in this.dc.ApplicationPermissions
                               select x);
            Permissions = new ObservableCollection<ApplicationPermission>(permissions);

            this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);
        }

        public string UserName
        {
            get
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
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

        private string _headerText = String.Empty;
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
                    RaisePropertyChanged("HeaderText");
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
                    //RaisePropertyChanged("SelectedSeasonIndex");
                }
            }
        }

        public ObservableCollection<string> FiscalYears
        {
            get
            {
                var list = (from x in Seasons
                            select x.TimePeriod);
                return new ObservableCollection<string>(list);
            }
        }

        private string _fiscalYear = String.Empty;
        public string FiscalYear
        {
            get
            {
                return _fiscalYear;
            }
            set
            {
                if (_fiscalYear != value)
                {
                    _fiscalYear = value;
                    SelectedSeason = (from x in Seasons
                                      where x.TimePeriod == _fiscalYear
                                      select x).FirstOrDefault();

                    RaisePropertyChanged("FiscalYear");
                }
            }
        }

        public ObservableCollection<Owner> Owners { get; set; }

        private Owner _selectedOwner = null;
        public Owner SelectedOwner
        {
            get
            {
                return _selectedOwner;
            }
            set
            {
                if (value != _selectedOwner)
                {
                    _selectedOwner = value;
                    RaisePropertyChanged("SelectedOwner");
                }
            }
        }

        private x_Invoice _selectedInvoice = null;
        public x_Invoice SelectedInvoice
        {
            get
            {
                return _selectedInvoice;
            }
            set
            {
                if (value != _selectedInvoice)
                {
                    _selectedInvoice = value;
                    RaisePropertyChanged("SelectedInvoice");
                }
            }
        }

        private Invoice _theInvoice = new Invoice();
        public Invoice TheInvoice
        {
            get
            {
                return _theInvoice;
            }
            set
            {
                if (value != _theInvoice)
                {
                    _theInvoice = value;
                    RaisePropertyChanged("TheInvoice");
                }
            }
        }

        private ObservableCollection<x_Invoice> _invoices = null;
        public ObservableCollection<x_Invoice> Invoices
        {
            get
            {
                return _invoices;
            }
            set
            {
                if (_invoices != value)
                {
                    _invoices = value;
                    RaisePropertyChanged("Invoices");
                }
            }
        }

        private DateTime MayFirst
        {
            get
            {
                string[] yrs = SelectedSeason.TimePeriod.Split('-');
                int year = Int32.Parse(yrs[0]);
                //int year = DateTime.Now.Year;
                DateTime mayFirst = new DateTime(year, 5, 1);
                return mayFirst;
            }
        }

        /* ----------------------------------- Private Methods ---------------------------------------- */
        #region Private Methods

        /// <summary>
        /// Summary
        ///     Raises a property changed event when the NewOwner data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Property_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedSeason")
            {
                IsBusy = true;

                HeaderText = string.Format("Invoice List for {0}", SelectedSeason.TimePeriod);

                ObservableCollection<x_Invoice> invoiceList = new ObservableCollection<x_Invoice>();

                var list = (from x in dc.Owners
                            where x.IsCurrentOwner == true
                            select x);
                Owners = new ObservableCollection<Owner>(list);

                foreach (Owner o in Owners)
                {

                    x_Invoice invoice = new x_Invoice();
                    invoice.OwnerID = o.OwnerID;
                    invoice.MailTo = o.MailTo;
                    invoice.Balance = (decimal)(from y in dc.v_OwnerDetails
                                                where y.OwnerID == o.OwnerID
                                                select y.Balance).FirstOrDefault();

                    int cartCount = (from z in o.GolfCarts
                                     where z.Year == Seasons[SelectedSeasonIndex - 1].TimePeriod
                                     select z.Quanity).FirstOrDefault();
                    int propertyCount = o.Properties.Count();

                    invoice.Dues = SelectedSeason.AnnualDues * propertyCount;

                    // Check to see if Special Assessment needs to be applied.
                    StringBuilder lots = new StringBuilder();
                    decimal assessedAmt = 0m;
                    foreach (Property p in o.Properties)
                    {
                        lots.Append(string.Format("[ {0} ]     ", p.Customer));
                        if (p.IsAssessment)
                        {
                            assessedAmt += (decimal)SelectedSeason.AssessmentAmount;
                        }
                    }
                    invoice.Properties = lots.ToString();
                    invoice.Assessment = assessedAmt;

                    decimal cartAmt = 0m;
                    if (cartCount > 0)
                    {
                        cartAmt += SelectedSeason.CartFee * cartCount;
                    }
                    invoice.Cart = cartAmt;

                    invoice.Total = invoice.Dues + invoice.Assessment + invoice.Cart;
                    invoice.NewBalance = invoice.Balance + invoice.Total;
                    invoiceList.Add(invoice);
                }
                Invoices = invoiceList;

                IsBusy = false;
            }
        }
        #endregion
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class InvoiceViewModel : CommonViewModel, ICommandSink
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
    public partial class InvoiceViewModel : CommonViewModel
    {

        /// <summary>
        /// THIS IS A PURE HACK~!
        /// ------------------------------------------------------------------------------------------------
        /// This was created to solve a time sensitive issue with Annual Invoices.  It should be used
        /// with caution.....  AT 01/26/2020
        /// </summary>
        public void AnnualInvoice()
        {

            CurrentSeason = (from x in Seasons
                             where x.IsCurrent == true
                             select x).FirstOrDefault();

            SelectedSeason = CurrentSeason;

            /// Get the list of all the current (active) owners to invoice
            /// 
            var currentOwnerList = (from x in dc.Owners
                                    where x.IsCurrentOwner == true
                                    select x);
            Owners = new ObservableCollection<Owner>(currentOwnerList);

            SelectedOwner = (from x in Owners
                             where x.OwnerID == SelectedInvoice.OwnerID
                             select x).FirstOrDefault();


            int propertyCount = SelectedOwner.Properties.Count();

            /// Check to see if Special Assessment needs to be applied.
            /// 
            decimal assessedAmt = 0m;
            foreach (Property p in SelectedOwner.Properties)
            {
                if (p.IsAssessment)
                {
                    assessedAmt += (decimal)SelectedSeason.AssessmentAmount;
                }
            }

            InvoiceItem invoiceItem = new InvoiceItem();
            try
            {
                TheInvoice = null;
                TheInvoice = new Invoice();
                decimal currentAccountBalance = (decimal)SelectedOwner.AccountBalance;
                bool recalculateBalance = false;
                /// If the account has a credit balance, we want to recalculate the balance
                /// so payments with equity are applied to the invoice.
                /// 
                if (currentAccountBalance < 0) { recalculateBalance = true; }

                TheInvoice.Season = SelectedSeason;
                TheInvoice.LastModifiedBy = UserName;
                TheInvoice.LastModified = DateTime.Now;
                TheInvoice.Season = SelectedSeason;
                TheInvoice.InvoiceItems = new ObservableCollection<InvoiceItem>();
                TheInvoice.GUID = Guid.NewGuid();
                TheInvoice.OwnerID = SelectedOwner.OwnerID;
                TheInvoice.IssuedDate = DateTime.Now;
                TheInvoice.DueDate = MayFirst;
                TheInvoice.TermsDays = -1;  /// -1 Indicates "Due by May 1st"
                TheInvoice.TermsDescriptive = "Due by May 1st";
                TheInvoice.PaymentsApplied = 0m;
                TheInvoice.IsPaid = false;
                TheInvoice.Priority = 1;

                /// Create the InvoiceItem
                /// 
                StringBuilder description = new StringBuilder();
                description.AppendLine("Membership dues for May 1 to Apr 30");
                description.Append("Lot(s)# ");
                foreach (Property p in SelectedOwner.Properties)
                {
                    description.Append(p.Customer);
                    if (SelectedOwner.Properties.Count() > 1)
                    {
                        description.Append(", ");
                    }
                }
                /// Remove the trailing comma from the string.
                /// 
                char[] remove = { ' ', ',' }; ;

                invoiceItem.Item = String.Format("FY{0} Dues", CurrentSeason.TimePeriod);
                invoiceItem.Priority = 1;
                invoiceItem.Description = description.ToString().TrimEnd(remove);
                invoiceItem.Quanity = SelectedOwner.Properties.Count();
                invoiceItem.Rate = SelectedSeason.AnnualDues;
                invoiceItem.Amount = invoiceItem.Quanity * invoiceItem.Rate;

                TheInvoice.InvoiceItems.Add(invoiceItem);
                TheInvoice.Memo = invoiceItem.Description;
                TheInvoice.Amount = invoiceItem.Amount;

                /// If the Owner has a credit balance...
                /// 
                if (SelectedOwner.AccountBalance < TheInvoice.Amount)
                {
                    /// If the account balance isn't enough to cover the total invoice amount....
                    /// 
                    if (Math.Abs((decimal)SelectedOwner.AccountBalance) < TheInvoice.Amount)
                    {
                        TheInvoice.BalanceDue = TheInvoice.Amount + (decimal)SelectedOwner.AccountBalance;
                    }
                    /// If the account balance is more than enough to cover the total invoice amount...
                    /// 
                    else
                    {
                        TheInvoice.BalanceDue = 0;
                    }
                    SelectedOwner.AccountBalance += TheInvoice.Amount;
                }
                /// If the Owner has a zero balance.....
                /// 
                else if (SelectedOwner.AccountBalance == TheInvoice.Amount)
                {
                    TheInvoice.BalanceDue = TheInvoice.Amount;
                    SelectedOwner.AccountBalance += TheInvoice.Amount;
                }
                /// If the Owner has a balanced owed....
                /// 
                else
                {
                    TheInvoice.BalanceDue = TheInvoice.Amount;
                    SelectedOwner.AccountBalance += TheInvoice.Amount;
                }

                /// Seralize the InvoiceItems collection to an XML string.
                /// 
                var xmlString = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
                TheInvoice.ItemDetails = xmlString;

                AnnualInvoice AnnualInvoice = new AnnualInvoice();
                AnnualInvoice.Owner = SelectedOwner;
                AnnualInvoice.BalanceBeforeInvoice = currentAccountBalance;
                AnnualInvoice.Season = SelectedSeason;
                AnnualInvoice.Quanity = SelectedOwner.Properties.Count();
                AnnualInvoice.Description = description.ToString();
                AnnualInvoice.DuesRate = SelectedSeason.AnnualDues;
                AnnualInvoice.Assessment = SelectedSeason.Assessment;
                AnnualInvoice.AssessmentRate = (decimal)SelectedSeason.AssessmentAmount;
                AnnualInvoice.TotalAmount = TheInvoice.Amount;
                AnnualInvoice.InvoiceNum = TheInvoice.TransactionID;

                /// The Xtra report requires a collection type with an interface (IEnumerable, IList, etc)
                /// A List entity set satisfies the requirement, so we add the invoice to the list so it
                /// can be passed as the data source object to the report.
                /// 
                string fileName = string.Empty;
                fileName = string.Format(@"D:\Invoices\Invoice-{0}.PDF", SelectedOwner.OwnerID);
                List<AnnualInvoice> ai = new List<AnnualInvoice>();
                ai.Add(AnnualInvoice);
                Reports.AnnuaInvoices report = new Reports.AnnuaInvoices();
                report.DataSource = ai;
                report.CreateDocument();
                report.ExportToPdf(fileName);

                /// Dispose the objects
                report.Dispose();
                report = null;
                AnnualInvoice.Dispose();

                dc.Invoices.InsertOnSubmit(TheInvoice);
                ChangeSet cs = dc.GetChangeSet();
                SaveExecute();

                /// Apply payment equity balance to the new invoice
                /// 
                if (recalculateBalance)
                {
                    dc.Refresh(RefreshMode.OverwriteCurrentValues, SelectedOwner);
                    Financial.RecalculateAccount(dc, SelectedOwner);
                    cs = dc.GetChangeSet();
                    SaveExecute();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invoice Error:" + ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        public virtual ISaveFileDialogService SaveFileDialogService { get { return this.GetService<ISaveFileDialogService>(); } }
        public virtual IExportService ExportService { get { return GetService<IExportService>(); } }
        public bool CanExport = true;
        /// <summary>
        /// Add Cart Command
        /// </summary>
        private ICommand _exportCommand;
        public ICommand ExportCommand
        {
            get
            {
                CommandAction action = new CommandAction();
                return _exportCommand ?? (_exportCommand = new CommandHandlerWparm((object parameter) => action.ExportAction(parameter, Table, SaveFileDialogService, ExportService), CanExport));
            }
        }

        public bool CanPrint = true;
        /// <summary>
        /// Print Command
        /// </summary>
        private ICommand _printCommand;
        public ICommand PrintCommand
        {
            get
            {
                CommandAction action = new CommandAction();
                return _printCommand ?? (_printCommand = new CommandHandlerWparm((object parameter) => action.PrintAction(parameter, Table, ExportService), CanPrint));
            }
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
            AnnualInvoice();
        }


    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class InvoiceViewModel : IDisposable
    public partial class InvoiceViewModel : IDisposable
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
        ~InvoiceViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}

public class x_Invoice
{
    public int OwnerID { get; set; }
    public string MailTo { get; set; }
    public string Properties { get; set; }
    public decimal Balance { get; set; }
    public decimal Dues { get; set; }
    public decimal Assessment { get; set; }
    public decimal Cart { get; set; }
    public decimal Total { get; set; }
    public decimal NewBalance { get; set; }
}

