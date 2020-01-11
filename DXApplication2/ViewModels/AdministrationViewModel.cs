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
    using System.Collections.Generic;

    public partial class AdministrationViewModel : CommonViewModel, ICommandSink
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public AdministrationViewModel(IDataContext dc, IDataContext pDC = null)
        {
            this.RegisterCommands();

            IsBusy = false;
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
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

            var items = (from x in this.dc.ListOfItems
                         where x.IsActive == true
                         select x);

            if (null != items)
            {
                ListOfItems = new ObservableCollection<ListOfItem>(items);
            }

            Host.PropertyChanged +=
                new System.ComponentModel.PropertyChangedEventHandler(this.HostNotification_PropertyChanged);
        }

        public string UserName
        {
            get
            {
                return System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
        }

        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

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
                    CanSaveExecute = false;
                    return false;
                }
                Caption = caption[0].TrimEnd(' ') + "*";
                CanSaveExecute = true;
                return true;
            }
            set { }
        }

        public override bool IsValid => throw new NotImplementedException();

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
                string[] yrs = SelectedSeason.TimePeriod.Split('-');
                int year = Int32.Parse(yrs[0]);
                //int year = DateTime.Now.Year;
                DateTime mayFirst = new DateTime(year, 5, 1);
                return mayFirst;
            }
        }

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
                    //RaisePropertyChanged("SelectedOwner");
                }
            }
        }

        public ObservableCollection<Owner> Owners { get; set; }

        private ObservableCollection<ListOfItem> _listOfItems = null;
        public ObservableCollection<ListOfItem> ListOfItems
        {
            get
            {
                return _listOfItems;
            }
            set
            {
                if (value != _listOfItems)
                {
                    // When the collection changes; an item is added/removed, we unregister the previous CollectionChanged
                    // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                    if (_listOfItems != null)
                    {
                        _listOfItems.CollectionChanged -= _listOfItems_CollectionChanged;
                    }
                    _listOfItems = value;
                    // Once the new value is assigned, we register a new PropertyChanged event listner.
                    this._listOfItems.CollectionChanged += _listOfItems_CollectionChanged;

                    /// Register a change notiification for the Invoice Items collection so changes
                    /// are processed.
                    /// 
                    this.RegisterForChangedNotification<ListOfItem>(ListOfItems);
                    RaisePropertyChanged("ListOfItems");
                }
            }
        }

        private ListOfItem _selectedItem = null;
        public ListOfItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                if (value != _selectedItem)
                {
                    _selectedItem = value;
                    RaisePropertyChanged("SelectedItem");
                }
            }
        }

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

        private AnnualInvoice _annualInvoice = null;
        public AnnualInvoice AnnualInvoice
        {
            get { return _annualInvoice; }
            set
            {
                if (_annualInvoice != value)
                {
                    _annualInvoice = value;
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

        /// <summary>
        /// Deserializes the Invoice Items XML string into an ObservableCollection
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<InvoiceItem> DeserializeInvoiceItems()
        {
            var items = XmlHelper.XmlDeserializeFromString<InvoiceItemList>(TheInvoice.ItemDetails);
            ObservableCollection<InvoiceItem> invoiceItems = new ObservableCollection<InvoiceItem>();
            foreach (InvoiceItem x in items.Items)
            {
                invoiceItems.Add(x);
            }
            return invoiceItems;
        }


        /* ------------------------------------ Private Methods -------------------------------------------- */
        #region Private Methods

        /// <summary>
        /// Generates an Annual Invoice for an Owner
        /// </summary>
        /// <param name="selectedOwner"></param>
        private void GenerateAnnualInvoice()
        {
            InvoiceItem invoiceItem = new InvoiceItem();
            try
            {
                using (Invoice TheInvoice = new Invoice())
                {
                    decimal currentAccountBalance = (decimal)SelectedOwner.AccountBalance;

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
                            TheInvoice.BalanceDue = (decimal)SelectedOwner.AccountBalance + TheInvoice.Amount;
                            SelectedOwner.AccountBalance += TheInvoice.BalanceDue;
                        }
                        /// If the account balance is more than enough to cover the total invoice amount...
                        /// 
                        else
                        {
                            TheInvoice.BalanceDue = 0;
                            SelectedOwner.AccountBalance += TheInvoice.Amount;
                        }
                    }
                    /// If the Owner has a zero balance.....
                    /// 
                    else if (SelectedOwner.AccountBalance == TheInvoice.Amount)
                    {
                        TheInvoice.BalanceDue = TheInvoice.Amount;
                        SelectedOwner.AccountBalance += TheInvoice.BalanceDue;
                    }
                    /// If the Owner has a balanced owed....
                    /// 
                    else
                    {
                        TheInvoice.BalanceDue = TheInvoice.Amount;
                        SelectedOwner.AccountBalance += TheInvoice.BalanceDue;
                    }

                    /// Seralize the InvoiceItems collection to an XML string.
                    /// 
                    var xmlString = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
                    TheInvoice.ItemDetails = xmlString;

                    /// Save the invoice to the owners account
                    /// 
                    dc.Invoices.InsertOnSubmit(TheInvoice);
                    ChangeSet cs = dc.GetChangeSet();
                    dc.SubmitChanges();

                    /// We use the AnnualInvoice class to create an object that can be passed
                    /// to the Annual Invoice report.  This report is a different format than
                    /// the standard invoice.
                    /// 
                    string fileName = string.Empty;
                    fileName = string.Format(@"D:\Invoices\Invoice-{0}.PDF", SelectedOwner.OwnerID);

                    AnnualInvoice AnnualInvoice = new AnnualInvoice();
                    AnnualInvoice.Owner = SelectedOwner;
                    AnnualInvoice.BalanceBeforeInvoice = (decimal)SelectedOwner.AccountBalance - TheInvoice.Amount;
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invoice Error:" + ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Generates a Golf Cart sticker invoice for an Owner.  Although the invoice is created, it is NOT
        /// posted to the Owner's account.  This messes with the account balance. So, it is just printed.
        /// When the owner pays for the sticker, the invoice is first added, then payment applied.
        /// </summary>
        /// <param name="selectedOwner"></param>
        private void GenerateGolfCartInvoice()
        {
            InvoiceItem invoiceItem = new InvoiceItem();
            List<Invoice> invoices = new List<Invoice>();

            try
            {
                using (Invoice TheInvoice = new Invoice())
                {
                    TheInvoice.Season = SelectedSeason;
                    TheInvoice.Owner = SelectedOwner;
                    TheInvoice.LastModifiedBy = UserName;
                    TheInvoice.LastModified = DateTime.Now;
                    TheInvoice.InvoiceItems = new ObservableCollection<InvoiceItem>();
                    TheInvoice.GUID = Guid.NewGuid();
                    TheInvoice.OwnerID = SelectedOwner.OwnerID;
                    TheInvoice.IssuedDate = DateTime.Now;
                    TheInvoice.DueDate = MayFirst;
                    TheInvoice.TermsDays = -1;  /// -1 Indicates "Due by May 1st"
                    TheInvoice.TermsDescriptive = "Due by May 1st";
                    TheInvoice.PaymentsApplied = 0;
                    TheInvoice.IsPaid = false;
                    TheInvoice.Priority = 10; /// Golf Cart Sticker is always a priority 10

                    /// Create the InvoiceItem
                    /// 
                    StringBuilder description = new StringBuilder();

                    /// Get the count of stickers the owner purchased the previous season.  
                    /// We use 'CurrentSeason' as the previous because we haven't made the new season (SelectedSeason) the active
                    /// season yet.
                    /// 
                    int cartCount = (from x in SelectedOwner.GolfCarts
                                     where x.Year == CurrentSeason.TimePeriod
                                     select x.Quanity).FirstOrDefault();

                    if (0 < cartCount)
                    {
                        invoiceItem = new InvoiceItem();
                        invoiceItem.Priority = 10; /// specific to Golf Cart sticker
                        invoiceItem.Item = "Golf Cart Sticker";
                        invoiceItem.Description = "Annual fee for Golf Cart Sticker";
                        invoiceItem.Quanity = cartCount;
                        invoiceItem.Rate = SelectedSeason.CartFee;
                        invoiceItem.Amount = invoiceItem.Quanity * invoiceItem.Rate;

                        TheInvoice.InvoiceItems.Add(invoiceItem);
                        TheInvoice.Memo = invoiceItem.Description;
                        TheInvoice.Amount = invoiceItem.Amount;
                        TheInvoice.BalanceDue = TheInvoice.Amount;
                    }

                    /// Seralize the InvoiceItems collection to an XML string.
                    /// 
                    var xmlString = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
                    TheInvoice.ItemDetails = xmlString;

                    /// The Xtra report requires a collection type with an interface (IEnumerable, IList, etc)
                    /// 
                    invoices.Add(TheInvoice);

                    /// Assign the 'invoices' entity set to the report's datasource property
                    /// Create and display the FinancialTransaction Recepit
                    /// Generate the PDF Invoice for the sticker.
                    /// 
                    string fileName = string.Format(@"D:\Invoices\Invoice-{0}_CartSticker.PDF", SelectedOwner.OwnerID);
                    Reports.StandardInvoiceReport report = new Reports.StandardInvoiceReport();
                    report.DataSource = invoices;

                    report.CreateDocument();
                    report.ExportToPdf(fileName);

                    invoices.Clear();
                    report.Dispose();
                    report = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error" + ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        protected void RegisterForChangedNotification<T>(ObservableCollection<T> list) where T : INotifyPropertyChanged
        {
            list.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(this.List_CollectionChanged<T>);
            foreach (T row in list)
            {
                row.PropertyChanged += new PropertyChangedEventHandler(this.ListItem_PropertyChanged);
            }
        }

        private void List_CollectionChanged<T>(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) where T : INotifyPropertyChanged
        {
            if (e != null && e.OldItems != null)
            {
                foreach (T row in e.OldItems)
                {
                    if (row != null)
                    {
                        // If one is deleted you can DeleteOnSubmit it here or something, also unregister for its property changed
                        row.PropertyChanged -= this.ListItem_PropertyChanged;
                    }
                }
            }

            if (e != null && e.NewItems != null)
            {
                foreach (T row in e.NewItems)
                {
                    if (row != null)
                    {
                        // If a new one is entered you can InsertOnSubmit it here or something, also register for its property changed
                        row.PropertyChanged += this.ListItem_PropertyChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Executes when the RelationsToProcess collection changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _listOfItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            string action = e.Action.ToString();

            switch (action)
            {
                case "Add":
                    var newItems = e.NewItems;
                    foreach (ListOfItem i in newItems)
                    {
                        dc.ListOfItems.InsertOnSubmit(i);
                    }
                    break;
                case "Remove":
                    var oldItems = e.OldItems;
                    foreach (ListOfItem i in oldItems)
                    {
                        dc.ListOfItems.DeleteOnSubmit(i);
                    }
                    break;
            }
            bool foo = IsDirty;
            RaisePropertyChanged("DataChanged");
        }


        /// <summary>
        /// Listen for changes to a registered collection's property(ies)
        /// </summary>
        /// <param name = "sender" ></ param >
        /// < param name="e"></param>
        private void ListItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //string prop = e.PropertyName;

            //switch (prop)
            //{
            //    case "InvoiceItem":
            //        InvoiceItem invoiceItem = sender as InvoiceItem;

            //        bool foo1 = IsDirty;
            //        break;
            //}
            bool foo = IsDirty;
            RaisePropertyChanged("DataChanged");
        }
        #endregion

        /* ------------------------------------ Public Methods -------------------------------------------- */
        #region Public Methods

        /// <summary>
        /// Property Changed event handler for the Host instance
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void HostNotification_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Refresh":
                    dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.ListOfItems);
                    break;
                default:
                    break;
            }
        }

        #endregion
    }

    /*====================================================Command Sink Bindings======================================================================*/
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
            get;
            set;
        }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            ChangeSet cs = dc.GetChangeSet();
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

    /*=====================================================Commands and Actions===============================================================*/
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
            try
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

                string msg;
                MessageBoxResult result;
                if (SelectedSeason.AnnualDues < 1.0m)
                {
                    msg = String.Format("Please enter a valid amount for {0} dues.", SelectedSeason.TimePeriod);
                    result = MessageBox.Show(msg, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    return;
                }
                {
                    msg = String.Format("Confirm you would like to make {0} the active year, and apply annual dues to all accounts.",
                        SelectedSeason.TimePeriod);
                    result = MessageBox.Show(msg, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                }

                if (MessageBoxResult.OK == result)
                {
                    IsBusy = true;

                    /// Disable the previous season, so we can prepare to create the new season
                    /// 
                    CurrentSeason.IsCurrent = false;
                    CurrentSeason.IsNext = false;
                    CurrentSeason.IsVisible = false;

                    /// We need to update the invoice ListOfItems records for annual dues to reflect 
                    /// the values of the new season
                    /// 
                    ListOfItem activeLOI = (from x in dc.ListOfItems
                                            where x.Item.Contains(CurrentSeason.TimePeriod) && x.IsActive == true
                                            select x).FirstOrDefault();
                    if (null != activeLOI)
                    {
                        activeLOI.Item = "FY" + SelectedSeason.TimePeriod + " Dues";
                        activeLOI.Rate = SelectedSeason.AnnualDues;
                    }
                    else
                    {
                        throw new System.ArgumentException("Requested record not found", " ListOfItems");
                    }

                    /// Get the list of all the current (active) owners to invoice
                    /// 
                    var currentOwnerList = (from x in dc.Owners
                                            where x.IsCurrentOwner == true
                                            select x);
                    Owners = new ObservableCollection<Owner>(currentOwnerList);

                    int propertyCount = 0;
                    int cartCount = 0;

                    /// We use a StreamWriter to create a CVS file of the invoices we create. The CVS
                    /// file is used to generate the invoices in HVCC and QuickBooks.
                    /// 
                    StringBuilder sb2 = new StringBuilder();
                    using (var StreamWriter = new StreamWriter(@"D:\Invoices\InvoiceList.csv"))
                    {
                        sb2.Append("OwnerID|Lot1|Lot2|Lot3|Lot4|Lot5|Lot6|Assessment|GolfCart|Total");
                        StreamWriter.WriteLine(sb2.ToString());

                        /// Iterate the Owners collection to generate the invoices and update the
                        /// database tables.
                        /// 
                        foreach (Owner owner in Owners)
                        {
                            SelectedOwner = owner;
                            propertyCount = 0;
                            cartCount = 0;
                            //StringBuilder sb = new StringBuilder();

                            /// StringBuilder2 is used to create a CSV file of invoice information....
                            /// 
                            sb2.Clear();
                            sb2.AppendFormat("{0}|", owner.OwnerID.ToString().PadLeft(6, '0'));

                            propertyCount = owner.Properties.Count();
                            //decimal amount = SelectedSeason.AnnualDues * propertyCount;
                            //sb.AppendFormat("Dues:{0}", amount.ToString("C", CultureInfo.CurrentCulture));

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
                                //sb.AppendFormat(" {0} Asessment:{1}", SelectedSeason.Assessment, assessedAmt.ToString("C", CultureInfo.CurrentCulture));
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

                            /// Create the transaction note, then add the Note and Invoice transaction to the database
                            /// 
                            //string note = String.Format("Annual dues and assessments of {0} applied", amount.ToString("C", CultureInfo.CurrentCulture));
                            //AddNote(owner, note);
                            GenerateAnnualInvoice();  // HERE

                            /// Check to see if the owner purchased a cart sticker for the previous season.  
                            /// We use 'CurrentSeason' as the previous because we haven't made the new season (SelectedSeason) the active
                            /// season yet.
                            /// 
                            cartCount = (from x in owner.GolfCarts
                                         where x.Year == CurrentSeason.TimePeriod
                                         select x.Quanity).FirstOrDefault();

                            if (0 < cartCount)
                            {
                                //amount += SelectedSeason.CartFee * cartCount;
                                //sb.AppendFormat(" GolfCart:{0}", (SelectedSeason.CartFee * cartCount).ToString("C", CultureInfo.CurrentCulture));
                                sb2.AppendFormat("{0}|", "YES");
                                GenerateGolfCartInvoice();
                            }
                            else
                            {
                                /// If they don't own a cart, we have to pad the columns
                                /// 
                                sb2.Append("|");
                            }

                            /// Add the total amount of the invoice and write the full line to the output file
                            /// 
                            decimal total = SelectedSeason.AnnualDues * propertyCount;
                            sb2.AppendFormat("{0}", total.ToString("C", CultureInfo.CurrentCulture));
                            StreamWriter.WriteLine(sb2.ToString());
                        }

                        /// Close the spreadsheet file.....
                        /// 
                        StreamWriter.Close();

                        Owners = null;
                    }

                    /// Update the Seasons table so we move the settings forward for the current and next season
                    /// 
                    SelectedSeason.IsCurrent = true;
                    SelectedSeason.IsNext = false;
                    SelectedSeason.IsDuesApplied = true;
                    Seasons[SelectedSeasonIndex + 1].IsNext = true;
                    Seasons[SelectedSeasonIndex + 1].IsVisible = true;

                    ChangeSet cs = dc.GetChangeSet();
                    dc.SubmitChanges();
                    IsBusy = false;
                    MessageBox.Show("Dues have been applied");
                }
            }
            catch (Exception ex)
            {
                IsBusy = false;
                MessageBox.Show("ERROR: " + ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Apply First Late fee Command
        /// </summary>
        private ICommand _applyApplyFirstLateCommand;
        public ICommand ApplyFirstLateCommand
        {
            get
            {
                return _applyApplyFirstLateCommand ?? (_applyApplyFirstLateCommand = new CommandHandlerWparm((object parameter) => ApplyLateFeeAction(parameter), true));
            }
        }

        /// <summary>
        /// Apply Second Late fee Command
        /// </summary>
        private ICommand _applySecondLateCommand;
        public ICommand ApplySecondLateCommand
        {
            get
            {
                return _applySecondLateCommand ?? (_applySecondLateCommand = new CommandHandlerWparm((object parameter) => ApplyLateFeeAction(parameter), true));
            }
        }

        /// <summary>
        /// Apply Third Late fee Command
        /// </summary>
        private ICommand _applyThirdLateCommand;
        public ICommand ApplyThirdLateCommand
        {
            get
            {
                return _applyThirdLateCommand ?? (_applyThirdLateCommand = new CommandHandlerWparm((object parameter) => ApplyLateFeeAction(parameter), true));
            }
        }

        /// <summary>
        /// Apply Late Fee command action
        /// </summary>
        /// <param name="type"></param>
        public void ApplyLateFeeAction(object parameter)
        {
            string whichLate = (string)parameter;
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

            using (var StreamWriter = new StreamWriter(@"D:\LateNotices\" + whichLate + ".csv"))
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
                    else if (true == owner.IsCurrentOwner && (decimal)owner.AccountBalance > 50.0m)
                    {

                        StringBuilder sb = new StringBuilder();
                        sb.AppendFormat("LateFee:{0}", amount.ToString("C", CultureInfo.CurrentCulture));

                        string note = String.Format("{0} Day Late Fee of {1} applied", whichLate.ToString(), amount.ToString("C", CultureInfo.CurrentCulture));

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
                        string desc = String.Format("{0} Day Late Fee", whichLate.ToString());
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
                        if ("first" == whichLate)
                        {
                            late.Is30Late = true;
                            SelectedSeason.IsLate30Applied = true;
                        }
                        if ("second" == whichLate)
                        {
                            late.Is60Late = true;
                            SelectedSeason.IsLate60Applied = true;
                        }
                        if ("third" == whichLate)
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
                        string fileName = string.Format(@"D:\LateNotices\{0}\{0}-{1}.PDF", whichLate, owner.OwnerID);
                        if ("first" == whichLate)
                        {
                            report = new Reports.PastDue30Days();
                        }
                        if ("second" == whichLate)
                        {
                            report = new Reports.PastDue60Days();
                        }
                        if ("third" == whichLate)
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
        /// Remove Invoice Item from grid Command
        /// </summary>
        private ICommand _removeItemCommand;
        public ICommand RemoveItemCommand
        {
            get
            {
                return _removeItemCommand ?? (_removeItemCommand = new CommandHandlerWparm((object parameter) => RemoveItemAction(parameter), true));
            }
        }

        /// <summary>
        /// Remove invoice item from grid command action
        /// </summary>
        /// <param name="type"></param>
        public void RemoveItemAction(object parameter)
        {
            ListOfItem item = parameter as ListOfItem;

            /// Remove the selected item from the in-memory collection.  This action will
            /// in turn cause a collection_changed event to fire which will result in the
            /// item being removed from the DC's entity set.
            /// 
            this.ListOfItems.Remove(item);
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
