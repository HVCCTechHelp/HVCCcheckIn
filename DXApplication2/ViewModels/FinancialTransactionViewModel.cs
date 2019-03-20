﻿namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Printing;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Converters;
    using HVCC.Shell.Common.Interfaces;
    using static HVCC.Shell.Common.Resources.Enumerations;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Models;
    using HVCC.Shell.Models.Financial;
    using HVCC.Shell.Resources;
    using HVCC.Shell.Validation;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Data.Linq;
    using System.Linq;
    using System.Text;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Xml.Serialization;

    public partial class FinancialTransactionViewModel : CommonViewModel, ICommandSink
    {
        public FinancialTransactionViewModel(IDataContext dc, object parameter)
        {
            IsBusy = true;
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            /// The parameter passed in is an Owner object, but it is not in this VM's 
            /// datacontext. Therefore, we have to get the Owner record from the database so
            /// it is local to this VM.
            /// 
            Owner param = parameter as Owner;
            this.SelectedOwner = (from x in this.dc.Owners
                                  where x.OwnerID == param.OwnerID
                                  select x).FirstOrDefault();

            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            FinancialTransactions = GetTransactionHistory();
            AllInvoices = GetAllInvoicesForOwner();
            AllPayments = GetAllPaymentsForOwner();

            CanDeleteTransaction = false;
            IsBusy = false;
        }

        /// <summary>
        ///  Required properties of the interface(s)
        /// </summary>
        #region Interface Properties
        public ApplicationPermission ApplPermissions { get; set; }
        public ApplicationDefault ApplDefault { get; set; }

        public override bool IsValid => throw new NotImplementedException();

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

        public MvvmBinder Binder { get; set; }
        #endregion

        /// <summary>
        /// Common properties used throughout the ViewModel
        /// </summary>
        #region VMcommons
        public Season CurrentSeason
        {
            get
            {
                Season s = (from x in dc.Seasons
                            where x.IsCurrent == true
                            select x).FirstOrDefault();
                return s;
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
                // wrap the setting in a null check.  When the master row is expanded, and a detail row
                // is selected, it causes a propertychanged event and sets the value to null, which we want to ignore.
                if (null != value && _selectedOwner != value)
                {
                    _selectedOwner = value;
                    RaisePropertyChanged("SelectedOwner");
                }
            }
        }

        public decimal PaymentsAppliedAmount
        {
            get
            {
                decimal sum = 0m;
                foreach (Payment p in AllPayments)
                {
                    sum += p.Amount;
                }
                return sum;
            }
        }

        public decimal InvoiceAppliedAmount
        {
            get
            {
                decimal sum = 0m;
                foreach (Invoice i in AllInvoices)
                {
                    sum += i.Amount;
                }
                return sum;
            }
        }

        public TransactionState IsTransactionState { get; set; }

        public TransactionType WhatIsBeingProcessed { get; set; }

        private DateTime MayFirst
        {
            get
            {
                int year = DateTime.Now.Year;
                DateTime mayFirst = new DateTime(year, 5, 1);
                return mayFirst;
            }
        }

        #endregion

        /// <summary>
        /// The properties that deal with the payment or invoice being processed
        /// </summary>
        #region Financials
        private ObservableCollection<v_OwnerTransaction> _financialTransactions = null;
        /// <summary>
        /// A collection of Payments 
        /// </summary>
        public ObservableCollection<v_OwnerTransaction> FinancialTransactions
        {
            get
            {
                return _financialTransactions;
            }
            set
            {
                if (this._financialTransactions != value)
                {
                    this._financialTransactions = value;
                    RaisePropertyChanged("FinancialTransactions");
                }
            }
        }

        public ObservableCollection<Invoice> AllInvoices { get; set; }

        public ObservableCollection<Payment> AllPayments { get; set; }

        private v_OwnerTransaction _selectedTransaction = new v_OwnerTransaction();
        public v_OwnerTransaction SelectedTransaction
        {
            get
            {
                return _selectedTransaction;
            }
            set
            {
                if (_selectedTransaction != value)
                {
                    _selectedTransaction = value;
                    if (_selectedTransaction.Type == "Invoice") { WhatIsBeingProcessed = TransactionType.Invoice; }
                    if (_selectedTransaction.Type == "Payment") { WhatIsBeingProcessed = TransactionType.Payment; }
                    RaisePropertyChanged("SelectedTransaction");
                    CanDeleteTransaction = true;
                }
            }
        }

        private Payment _thePayment = null;
        public Payment ThePayment
        {
            get
            {
                return _thePayment;
            }
            set
            {
                // When NewPayment is changed, we unregister the previous PropertyChanged
                // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                // We use this PropertyChanged trigger to handle NewPayment property changes.
                if (this._thePayment != null)
                {
                    this._thePayment.PropertyChanged -= ThePayment_PropertyChanged;
                }
                if (value != _thePayment)
                {
                    _thePayment = value;
                    if (this._thePayment != null)
                    {
                        // Once the new value is assigned, we register a new PropertyChanged event listner.
                        _thePayment.PropertyChanged += ThePayment_PropertyChanged;
                    }
                    RaisePropertyChanged("ThePayment");
                }
            }
        }

        private Payment _paymentPriorToEdit = null;
        public Payment PaymentPriorToEdit
        {
            get
            {
                return _paymentPriorToEdit;
            }
            set
            {
                if (value != _paymentPriorToEdit)
                {
                    _paymentPriorToEdit = value;
                }
            }
        }

        public decimal PendingPmtAmount { get; set; }

        private Invoice _theInvoice = null;
        public Invoice TheInvoice
        {
            get
            {
                return _theInvoice;
            }
            set
            {
                // When NewPayment is changed, we unregister the previous PropertyChanged
                // event listner to avoid a propogation of objects being created in memory and possibly leading to an out of memory error.
                // We use this PropertyChanged trigger to handle NewPayment property changes.
                if (this._theInvoice != null)
                {
                    this._theInvoice.PropertyChanged -= TheInvoice_PropertyChanged;
                }
                if (value != _theInvoice)
                {
                    _theInvoice = value;
                    if (this._theInvoice != null)
                    {
                        /// Check to see if this is a new invoice....  (this should be moved to where the GUID is set.....
                        if (0 == _theInvoice.TransactionID)
                        {
                            _theInvoice.IssuedDate = DateTime.Now;
                        }
                        // Once the new value is assigned, we register a new PropertyChanged event listner.
                        _theInvoice.PropertyChanged += TheInvoice_PropertyChanged;
                    }
                    RaisePropertyChanged("TheInvoice");
                }
            }
        }

        private Invoice _invoicePriorToEdit = null;
        public Invoice InvoicePriorToEdit
        {
            get
            {
                return _invoicePriorToEdit;
            }
            set
            {
                if (value != _invoicePriorToEdit)
                {
                    _invoicePriorToEdit = value;
                }
            }
        }

        private InvoiceItem _selectedInvoiceItem = null;
        public InvoiceItem SelectedInvoiceItem
        {
            get
            {
                return _selectedInvoiceItem;
            }
            set
            {
                if (_selectedInvoiceItem != value)
                {
                    _selectedInvoiceItem = value;
                    RaisePropertyChanged("SelectedInvoiceItem");
                }
            }
        }

        public Payment_X_Invoice PXI { get; set; }

        public ObservableCollection<Payment_X_Invoice> PXIs { get; set; }

        /// <summary>
        /// List of billible items for an invoice
        /// </summary>
        public ObservableCollection<ListOfItem> AvailableItems
        {
            get
            {
                var list = (from x in dc.ListOfItems
                            where x.IsActive == true
                            select x);

                return new ObservableCollection<ListOfItem>(list);
            }
        }

        /// <summary>
        /// TEMPORARY:  Need to convert this to a DB table....
        /// </summary>
        private ObservableCollection<TermsList> _termsList = new ObservableCollection<TermsList>();
        public ObservableCollection<TermsList> TermsList
        {
            get
            {
                if (0 == _termsList.Count())
                {
                    _termsList.Add(new TermsList { DescriptiveTerm = "May 1st", Days = -1 });
                    _termsList.Add(new TermsList { DescriptiveTerm = "Net 30", Days = 30 });
                    _termsList.Add(new TermsList { DescriptiveTerm = "Due Now", Days = 0 });
                }
                return _termsList;
            }
        }
        #endregion

        /// <summary>
        /// Properties that affect the appearance of the UI
        /// </summary>
        #region UIproperties

        private decimal _processingOwnerID = 0m;
        public decimal ProcessingOwnerID
        {
            get { return _processingOwnerID; }
            set
            {
                if (value != _processingOwnerID)
                {
                    _processingOwnerID = value;
                    RaisePropertyChanged("ProcessingOwnerID");
                }
            }
        }

        private SolidColorBrush _textColor = new SolidColorBrush(Colors.Black);
        public SolidColorBrush TextColor
        {
            get
            {
                return _textColor;
            }
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    RaisePropertyChanged("TextColor");
                }
            }
        }

        public string HeaderText
        {
            get
            {
                return string.Format("Post Transaction for Owner #{0}", SelectedOwner.OwnerID);
            }
        }

        public string BillTo
        {
            get
            {
                return string.Format("BILL TO: #{0:000000}", SelectedOwner.OwnerID);
            }
        }

        private bool _canDeleteTransaction = false;
        public bool CanDeleteTransaction
        {
            get
            {
                // Only allow deletions of an existing record that is not in a dirty state.
                //if (this.SelectedTransaction != null && this.SelectedTransactionIndex == 0 && !IsEditTransaction && ApplPermissions.CanViewAdministration)
                //{
                return _canDeleteTransaction;
                //}

                //return _canDeleteTransaction = false;
            }
            set
            {
                if (_canDeleteTransaction != value)
                {
                    _canDeleteTransaction = value;
                    RaisePropertyChanged("CanDeleteTransaction");
                }
            }
        }

        private bool _isInvoiceEnabled = true;
        public bool IsInvoiceEnabled
        {
            get
            {
                return _isInvoiceEnabled;
            }
            set
            {
                if (value != _isInvoiceEnabled)
                {
                    _isInvoiceEnabled = value;
                    RaisePropertyChanged("IsInvoiceEnabled");
                }
            }
        }

        private bool _isPaymentEnabled = true;
        public bool IsPaymentEnabled
        {
            get
            {
                return _isPaymentEnabled;
            }
            set
            {
                if (value != _isPaymentEnabled)
                {
                    _isPaymentEnabled = value;
                    RaisePropertyChanged("IsPaymentEnabled");
                }
            }
        }

        private bool _isCollapsedInvoice = true;
        public bool IsCollapsedInvoice
        {
            get
            {
                return _isCollapsedInvoice;
            }
            set
            {
                if (value != _isCollapsedInvoice)
                {
                    _isCollapsedInvoice = value;
                    RaisePropertyChanged("IsCollapsedInvoice");
                }
            }
        }

        private bool _isCollapsedPayments = true;
        public bool IsCollapsedPayments
        {
            get
            {
                return _isCollapsedPayments;
            }
            set
            {
                if (value != _isCollapsedPayments)
                {
                    _isCollapsedPayments = value;
                    RaisePropertyChanged("IsCollapsedPayments");
                }
            }
        }

        private bool _isCollapsedHistoryGrid = false;
        public bool IsCollapsedHistoryGrid
        {
            get { return _isCollapsedHistoryGrid; }
            set
            {
                if (_isCollapsedHistoryGrid != value)
                {
                    _isCollapsedHistoryGrid = value;
                    RaisePropertyChanged("IsCollapsedHistoryGrid");
                }
            }
        }

        private int _invoiceHeight = 0;
        public int InvoiceHeight
        {
            get
            {
                return _invoiceHeight;
            }
            set
            {
                if (value != _invoiceHeight)
                {
                    _invoiceHeight = value;
                    RaisePropertyChanged("InvoiceHeight");
                }
            }
        }

        private int _paymentHeight = 0;
        public int PaymentHeight
        {
            get
            {
                return _paymentHeight;
            }
            set
            {
                if (value != _paymentHeight)
                {
                    _paymentHeight = value;
                    RaisePropertyChanged("PaymentHeight");
                }
            }
        }
        #endregion

        /// ViewModel Methods
        /// 
        #region Methods
        /// 
        /// <summary>
        /// Gets all the financial records (invoices/payments) for the selected owner
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<v_OwnerTransaction> GetTransactionHistory()
        {
            try
            {
                /// Get all the transactions for the selected Owner and calculate the account balance
                var list = (from x in dc.v_OwnerTransactions
                            where x.OwnerID == SelectedOwner.OwnerID
                            select x).OrderByDescending(x => x.Date);

                if (null != list && 0 < list.Count())
                {
                    /// Iterate over the records to calculate the account balance.  Although the balance was updated
                    /// the last time (each time) the financial records were updated, we recalculate it for accuracy.
                    decimal balance = 0m;
                    foreach (v_OwnerTransaction t in list)
                    {
                        balance += t.Amount;
                    }
                    //AccountBalance = balance;
                }

                return new ObservableCollection<v_OwnerTransaction>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error retrieving Financial data : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

        }

        private ObservableCollection<Invoice> GetAllInvoicesForOwner()
        {
            /// Build the list of outstanding invoices on the initial spin up of the payment processing.
            /// The list will be used later on in the process to determine what invoice(s) the payment will
            /// be applied to. Therefore, we do not want to change the initial collection.
            dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Invoices);
            var list = (from x in dc.Invoices
                        where x.OwnerID == SelectedOwner.OwnerID
                        select x);
            if (null == list)
            {
                return new ObservableCollection<Invoice>();
            }
            else
            {
                /// The paoperty ISApplyToPayment is an extention property of Invoice (not included in the Invoice
                /// database model). Therefore, we have to set the initial value here.
                /// 
                foreach (Invoice i in list)
                {
                    i.IsPaymentApplied = false;
                }
                return new ObservableCollection<Invoice>(list);
            }
        }

        private ObservableCollection<Invoice> GetOpenInvoicesForOwner()
        {
            /// Build the list of outstanding invoices on the initial spin up of the payment processing.
            /// The list will be used later on in the process to determine what invoice(s) the payment will
            /// be applied to. Therefore, we do not want to change the initial collection.
            //dc.Refresh(RefreshMode.OverwriteCurrentValues, dc.Invoices);
            //var list = (from x in dc.Invoices
            //            where x.OwnerID == SelectedOwner.OwnerID &&
            //            x.BalanceDue > 0
            //            select x);

            var list = (from x in AllInvoices
                        where x.BalanceDue > 0
                        select x);

            if (null == list)
            {
                return new ObservableCollection<Invoice>();
            }
            else
            {
                /// The paoperty ISApplyToPayment is an extention property of Invoice (not included in the Invoice
                /// database model). Therefore, we have to set the initial value here.
                /// 
                foreach (Invoice i in list)
                {
                    i.IsPaymentApplied = false;
                }
                return new ObservableCollection<Invoice>(list);
            }
        }

        private ObservableCollection<Payment> GetAllPaymentsForOwner()
        {
            var list = (from x in dc.Payments
                        where x.OwnerID == SelectedOwner.OwnerID
                        orderby x.PaymentDate
                        select x);
            if (null == list || 0 == list.Count())
            {
                return new ObservableCollection<Payment>();
            }
            else
            {
                return new ObservableCollection<Payment>(list);
            }
        }

        private ObservableCollection<Payment> GetAvailablePaymentsForOwner()
        {
            //var list = (from x in dc.Payments
            //            where x.OwnerID == SelectedOwner.OwnerID &&
            //            x.EquityBalance > 0m
            //            orderby x.PaymentDate
            //            select x);

            var list = (from x in AllPayments
                        where x.EquityBalance > 0m
                        orderby x.PaymentDate
                        select x);
            if (null == list)
            {
                return new ObservableCollection<Payment>();
            }
            else
            {
                return new ObservableCollection<Payment>(list);
            }
        }

        private ObservableCollection<Payment> GetPaymentsForInvoice(Invoice invoice)
        {
            /// New up a payments collection for TheInvoice
            /// 
            ObservableCollection<Payment> pmts = new ObservableCollection<Payment>();

            /// Query the DC's PxI table for records matching the Invoice
            /// 
            var list = (from x in dc.Payment_X_Invoices
                        where x.InvoiceID == invoice.TransactionID
                        select x);

            /// If PxI's were returned, we iterate the list and add the Payment
            /// records to the Payments collection.
            /// 
            if (null != list)
            {
                foreach (Payment_X_Invoice pxi in list)
                {
                    Payment pmt = (from p in dc.Payments
                                   where p.TransactionID == pxi.PaymentID
                                   select p).FirstOrDefault();
                    pmts.Add(pmt);
                }
            }
            return pmts;
        }

        private ObservableCollection<Invoice> GetInvoicesForPayment(Payment thePayment, SortOrder sortOrder)
        {
            /// Sort the Payment's PXI records....
            ///
            IEnumerable<Payment_X_Invoice> sortedList;
            if (sortOrder == SortOrder.Assending)
            {
                sortedList = ThePayment.Payment_X_Invoices.AsQueryable().OrderBy(Payment_X_Invoice => Payment_X_Invoice.InvoiceID);
            }
            else
            {
                sortedList = ThePayment.Payment_X_Invoices.AsQueryable().OrderByDescending(Payment_X_Invoice => Payment_X_Invoice.InvoiceID);
            }

            /// Reterived the Invoice(s) for the Payment using the PXI record(s) of the Payment
            /// 
            ObservableCollection<Invoice> invoices = new ObservableCollection<Invoice>();
            foreach (Payment_X_Invoice pxi in sortedList)
            {
                Invoice inv = (from i in dc.Invoices
                               where i.TransactionID == pxi.InvoiceID
                               select i).FirstOrDefault();

                inv.PaymentAmount = ThePayment.Amount;
                invoices.Add(inv);
            }
            return invoices;
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

        /// <summary>
        /// 
        /// </summary>
        private void CheckForGolfCartSticker(TransactionType transactionType)
        {
            /// Check to see if the invoice has a golf cart sticker line item
            /// 
            var items = (from x in TheInvoice.InvoiceItems
                         where x.Item == "Golf Cart Sticker"
                         select x);

            /// If the invoice has a cart sticker, we iterate the items collection. Most likely
            /// there will only be one item.
            /// 
            foreach (InvoiceItem item in items)
            {
                /// Check to see if there is an existing golf cart record for this owner and
                /// for the current season.
                /// 
                GolfCart gc = (from x in SelectedOwner.GolfCarts
                               where x.Year == CurrentSeason.TimePeriod
                               select x).FirstOrDefault();


                /// Is a payment is being applied to a cart sticker invoice...
                /// 
                if (transactionType == TransactionType.Payment)
                {
                    gc.Quanity = item.Quanity;
                    if (0 == TheInvoice.BalanceDue)
                    {
                        gc.PaymentDate = ThePayment.PaymentDate;
                        gc.IsPaid = true;
                    }
                }
                /// If the method was called from an update to an existing Invoice, then the 
                /// most likey senerio is the quanity is being updated. We are only concerned
                /// with this condition if there is an existing CG record that needs to be updated.
                /// 
                else
                {
                    if (null == gc)
                    {
                        gc = new GolfCart();
                        SelectedOwner.GolfCarts.Add(gc);

                        gc.OwnerID = SelectedOwner.OwnerID;
                        gc.Customer = SelectedOwner.Customer;
                        gc.Year = CurrentSeason.TimePeriod;
                        gc.PaymentDate = null;
                        gc.IsPaid = false;
                        gc.IsReceived = false;
                        gc.ReceivedDate = null;
                        gc.Quanity = item.Quanity;
                        dc.GolfCarts.InsertOnSubmit(gc);

                        ChangeSet cs = dc.GetChangeSet();
                    }
                    /// If the quanity is increasing, we assume, since this is a new invoice, it isn't 
                    /// paid for...?
                    /// 
                    if (TheInvoice.BalanceDue > 0)
                    {
                        gc.IsPaid = false;
                    }
                    else
                    {
                        gc.IsPaid = true;
                    }
                    gc.Quanity = item.Quanity;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// ViewModel property change event handlers
    /// </summary>
    #region PropertyChangeEvents
    public partial class FinancialTransactionViewModel : CommonViewModel, ICommandSink
    {
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
        /// Listen for changes to a registered collection's property(ies)
        /// </summary>
        /// <param name = "sender" ></ param >
        /// < param name="e"></param>
        private void ListItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            /// Figure out which collection raised the property change
            string longType = sender.GetType().ToString();
            string[] strings = longType.Split('.');
            int count = strings.Count();
            string type = strings[count - 1];

            switch (type)
            {
                case "Invoice":
                    TheInvoice_PropertyChanged(sender, e);
                    break;
                case "Payment":
                    ThePayment_PropertyChanged(sender, e);
                    break;
                /// The InvoiceItem property changed is raised when TheInvoice items list changes.
                /// This is also where/when the Invoice is applied to open payments with positive equity balances.
                /// 
                case "InvoiceItem":
                    InvoiceItem invoiceItem = sender as InvoiceItem;

                    switch (e.PropertyName)
                    {
                        /// Item is a property of the InvoiceItems collection
                        /// 
                        case "Item":
                            var r = (from x in AvailableItems
                                     where x.Item == SelectedInvoiceItem.Item
                                     select x).FirstOrDefault();

                            SelectedInvoiceItem.Rate = r.Rate;

                            if (SelectedInvoiceItem.Item.Contains("Dues"))
                            {
                                StringBuilder description = new StringBuilder();
                                description.AppendLine(r.Description);
                                description.Append("Lot(s)# ");
                                foreach (Property p in SelectedOwner.Properties)
                                {
                                    description.Append(p.Customer);
                                    if (SelectedOwner.Properties.Count() > 1)
                                    {
                                        description.Append(", ");
                                    }
                                }

                                char[] remove = { ' ', ',' }; ;
                                SelectedInvoiceItem.Description = description.ToString().TrimEnd(remove);
                                SelectedInvoiceItem.Quanity = SelectedOwner.Properties.Count();
                            }
                            else
                            {
                                SelectedInvoiceItem.Quanity = 1;
                                SelectedInvoiceItem.Description = r.Description;
                            }
                            break;
                        /// Quanity is a property of the InvoiceItems collection.  When either the Quanity or
                        /// Rate is changed the Invoice Amount must be recalculated.  In addition, the balance
                        /// due on the invoice must be calculated.  If the customer has payments with equity
                        /// balance than it is deducted from the invoice.  The process of relating payments
                        /// to invoice (and calculations) happens if/when the user saves the invoice. Otherwise
                        /// these calculations for visual display on the UI.
                        /// 
                        case "Quanity":
                        case "Rate":
                            /// Each time the quanity or rate change, we have to resest the invoice amount 
                            /// and selected invoice item's total
                            /// 
                            SelectedInvoiceItem.Amount = SelectedInvoiceItem.Rate * SelectedInvoiceItem.Quanity;
                            decimal _invoiceAmount = 0m;
                            foreach (InvoiceItem i in TheInvoice.InvoiceItems)
                            {
                                _invoiceAmount += i.Amount;
                            }
                            TheInvoice.Amount = _invoiceAmount;

                            if (null != InvoicePriorToEdit)
                            {
                                /// Check to see if they have a Golf Cart sticker.....
                                /// 
                                CheckForGolfCartSticker(TransactionType.Invoice);
                            }
                            break;
                        /// When an Item is removed from the invoice, the invoice amount has to 
                        /// be adjusted by subtracting the item's amount.
                        /// 
                        case "Remove":
                            /// TO-DO: If Golf Cart Sticker is deleted, remove the Golf Cart Record
                            /// 
                            TheInvoice.Amount -= invoiceItem.Amount;
                            break;
                    }
                    /// Seralize the InvoiceItems collection to an XML string.
                    /// 
                    var xmlString = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
                    TheInvoice.ItemDetails = xmlString;
                    ChangeSet cs = dc.GetChangeSet();
                    bool foo1 = IsDirty;
                    break;
            }
            bool foo2 = IsDirty;
        }

        /// <summary>
        /// ThePayment PropertyChanged event handler.  When a property of ThePayment object is
        /// changed, the PropertyChanged event is fired, and processed by this handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThePayment_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            /// When the user enters a payment amount we update the first OutstandingInvoice object.
            /// 
            switch (e.PropertyName)
            {
                case "Amount":
                    /// In the case where the user entered a payment amount, then changed the amount before
                    /// it was saved we treat it as an Edit transaction (even if it is a new payment)
                    /// 
                    if (ThePayment.Amount != PendingPmtAmount && IsTransactionState == TransactionState.Pending)
                    {
                        IsTransactionState = TransactionState.EditSkip;
                    }
                    if (ThePayment.Amount > 0)
                    {
                        /// Payments are stored as a negative amount by accounting principals....
                        /// Therefore, we inverse the amount so all calculations are calculated
                        /// using addition. If the transaction is new, we also add the new payment
                        /// record to the datacontext's tranaction stack.
                        /// 
                        if (IsTransactionState == TransactionState.New)
                        {
                            dc.Payments.InsertOnSubmit(ThePayment);
                            AllPayments.Add(ThePayment);
                        }
                        ThePayment.Amount = ThePayment.Amount * -1;
                    }
                    /// There is two different sets of logic for processing a payment depending of
                    /// if the payment is new, or is being edited.  
                    /// 
                    if (IsTransactionState == TransactionState.New)
                    {
                        ThePayment.EquityBalance = Math.Abs(ThePayment.Amount);

                        /// Apply payment if there are unpaid invoices available
                        /// 
                        ThePayment.Invoices = GetOpenInvoicesForOwner();
                        if (ThePayment.HasInvoices)
                        {
                            int ndx = 0;
                            while (ThePayment.EquityBalance > 0 && ThePayment.Invoices.Count() > ndx)
                            {
                                TheInvoice = ThePayment.Invoices[ndx];
                                Financial.ApplyPayment(ThePayment, TheInvoice, TransactionType.Payment);

                                /// Deserialize the XML string to get the list of invoice items so it can also be copied
                                /// to the InvoiceItems collection which is bound to a UI grid.
                                /// 
                                TheInvoice.InvoiceItems = DeserializeInvoiceItems();

                                /// Apply the payment to the invoice
                                /// 
                                ThePayment.PaymentMsg.Visibility = Visibility.Hidden;

                                TheInvoice.IsPaymentApplied = true;

                                /// Check to see if they paid for a Golf Cart sticker.....
                                /// 
                                CheckForGolfCartSticker(TransactionType.Payment);

                                /// Increment the invoice array index, and do it again
                                /// 
                                ++ndx;
                            }
                        }

                        /// Set the transaction state to 'Pending' to avoid a RaisePropertyChanged event
                        /// from executing any of the Amount property changed code a second time.
                        /// 
                        PendingPmtAmount = ThePayment.Amount;
                        IsTransactionState = TransactionState.Pending;
                        bool foo = IsDirty;
                    }
                    /// Else, we are editing the payment....
                    /// 
                    else if (IsTransactionState == TransactionState.Edit || IsTransactionState == TransactionState.EditSkip)
                    {
                        /// Set the message box result to an initial state of OK so the warning message will
                        /// be skipped. If this payment is being edited, then we will go ahead and present
                        /// the warning.
                        /// 
                        MessageBoxResult result = MessageBoxResult.OK;
                        if (IsTransactionState != TransactionState.EditSkip)
                        {
                            result = MessageBox.Show("This payment has been used to pay invoices.\n" +
                               "Changing the payment amount affects how it applies to those invoices.\n" +
                               "Do you want to continue?", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        }

                        /// If the user clicked on cancel, all we need to do is refresh the payments table
                        /// to reset everything back to the unedited value(s).
                        /// 
                        if (MessageBoxResult.Cancel == result)
                        {
                            dc.Refresh(RefreshMode.OverwriteCurrentValues, ThePayment);
                        }
                        /// We process invoices if any exist.
                        /// 
                        else
                        {
                            /// If there are invoices, they are first unapplied from the payment and the account balance
                            /// is adjusted so the new (edited) payment amount is reapplied to the invoices.
                            /// 
                            ThePayment.Invoices = GetInvoicesForPayment(ThePayment, SortOrder.Assending);
                            if (ThePayment.HasInvoices)
                            {
                                /// We have to reverse the payment for each associated Invoice (in memory)
                                /// before we apply the revised payment amount. We also have to unapply the payment
                                /// from associated invoices in reverse order (newest to oldest).
                                /// 
                                int ndx = 0;
                                while (ThePayment.Invoices.Count() > ndx)
                                {
                                    TheInvoice = ThePayment.Invoices[ndx];
                                    TheInvoice.IsPaymentApplied = false;
                                    PXI = Financial.UnapplyPayment(ThePayment, TheInvoice, TransactionType.Payment);
                                    dc.Payment_X_Invoices.DeleteOnSubmit(PXI);
                                    ++ndx;
                                }
                                ThePayment.Invoices.Clear();
                            }

                            /// Now that the old payment amount was reversed from the invoices, we
                            /// have to set the payment's equity balance to the new (edited) payment amount
                            /// and apply the payment to open invoices.
                            /// 
                            ThePayment.EquityBalance = Math.Abs(ThePayment.Amount);

                            /// Refresh the list of open invoices the payment can be applied to.
                            /// 
                            ThePayment.Payment_X_Invoices.Clear();
                            ThePayment.Invoices = GetOpenInvoicesForOwner();
                            if (ThePayment.HasInvoices)
                            {
                                /// Apply the payment:
                                /// Depending whether the new amount is greater or less
                                /// than the previous amount, we may need to look for additional open invoices
                                /// if the payment has a remaining equity balance after applying payments to
                                /// the current list of associated invoices.
                                /// 
                                int ndx = 0;
                                while (ThePayment.EquityBalance > 0 && ThePayment.Invoices.Count() > ndx)
                                {
                                    TheInvoice = ThePayment.Invoices[ndx];
                                    ThePayment.PaymentMsg.Visibility = Visibility.Hidden;
                                    Financial.ApplyPayment(ThePayment, TheInvoice, TransactionType.Payment);
                                    TheInvoice.IsPaymentApplied = true;
                                    ++ndx;
                                }
                                dc.Payment_X_Invoices.InsertAllOnSubmit(ThePayment.Payment_X_Invoices);
                            }
                        }
                        /// Set the transaction state to 'Pending' to avoid a RaisePropertyChanged event
                        /// from executing any of the Amount property changed code.
                        /// 
                        PendingPmtAmount = ThePayment.Amount;
                        IsTransactionState = TransactionState.Pending;
                        bool foo = IsDirty;
                    }
                    /// TransactionState == Pending: We can ignore the last property changed event.
                    /// It is a result of how the property changed stack is processed.
                    /// 
                    else
                    {
                        ; /// Do nothing....
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// TheInvoice PropertyChanged event handler.  When a property of TheInvoice object is
        /// changed, the PropertyChanged event is fired, and processed by this handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TheInvoice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "TermsDays":
                    /// Calulate the due date
                    switch (TheInvoice.TermsDays)
                    {
                        case -1:
                            TheInvoice.DueDate = MayFirst;
                            break;
                        case 0:
                            TheInvoice.DueDate = TheInvoice.IssuedDate;
                            break;
                        case 30:
                            TheInvoice.DueDate = TheInvoice.IssuedDate.AddDays((double)TheInvoice.TermsDays);
                            break;
                    }
                    /// TEMPORARY: This may not be the best place to make this assignment.....
                    /// Assign the descriptive terms string to the new invoice.
                    TheInvoice.TermsDescriptive = (from x in TermsList
                                                   where x.Days == TheInvoice.TermsDays
                                                   select x.DescriptiveTerm).FirstOrDefault();
                    break;
                case "Amount":
                    /// When the amount changes we put the Invoice object on the datacontext's Insert queue
                    /// if it is new (does not have a valid TransactionID)
                    /// 
                    if (0 == TheInvoice.TransactionID && IsTransactionState == TransactionState.New)
                    {
                        dc.Invoices.InsertOnSubmit(TheInvoice);
                        AllInvoices.Add(TheInvoice);
                    }

                    /// If this is a new invoice, we have to get the current AccountBalance for the Owner
                    /// before the invoice is applied.
                    /// 
                    if (IsTransactionState == TransactionState.New)
                    {
                        /// Set, or reset, the parameters affected by an invoice amount change.
                        /// 
                        TheInvoice.BalanceDue = TheInvoice.Amount;
                        TheInvoice.PaymentsApplied = 0m;
                        IsTransactionState = TransactionState.Pending;
                    }
                    /// If we are editing a new invoice, and the Amount has changed from the initial
                    /// value, we reset the Invoice values and AccountBalance and reprocess payments.
                    /// 
                    else if (IsTransactionState == TransactionState.Pending)
                    {
                        TheInvoice.BalanceDue = TheInvoice.Amount;
                        TheInvoice.PaymentsApplied = 0m;
                        CheckForGolfCartSticker(TransactionType.Invoice);
                    }
                    /// This is an invoice that was previously saved and is now being edited.
                    /// (IsTransactionState == TransactionState.Edit)
                    /// 
                    else
                    {
                        /// Why did the invoice amount change?
                        ///     1). The item quaanity/rate changed.
                        ///     2). A new item was added
                        ///     3). An existing item was deleted
                        /// To keep everything stright, we subtract the invoice amount prior to the edit.
                        /// Then set the BalanceDue to the new calculated amount (based on invoice items),
                        /// and zero out the payment applied. 
                        /// 
                        TheInvoice.BalanceDue = TheInvoice.Amount;
                        TheInvoice.PaymentsApplied = 0m;

                        if (0 == TheInvoice.InvoiceItems.Count())
                        {
                            MessageBox.Show("You must have at lease 1 invoice item for an invoice to be valid",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            /// Since the edit was invalid, we have to reset everything back to its 
                            /// original state.
                            /// 
                            dc.Refresh(RefreshMode.OverwriteCurrentValues, TheInvoice);
                            return;
                        }

                        /// Since TheInvoice values have changed, we use the invoice values proir
                        /// to being edited to unapply the payment. This keeps the AccountBalance
                        /// accurate, and it roles the payment values back to their original values.
                        /// 
                        foreach (Payment p in InvoicePriorToEdit.Payments)
                        {
                            ThePayment = p;
                            PXI = Financial.UnapplyPayment(ThePayment, InvoicePriorToEdit, TransactionType.Invoice);
                        }
                    }

                    /// Now apply payments to the invoice if there are available payments with 
                    /// an equity balance.
                    /// 
                    if (TheInvoice.HasPayments)
                    {
                        /// If this is a New/Pending invoice, we refresh the payments collection and 
                        /// remove the PxI.  Both will be updated with the new values when the payment(s)
                        /// are applied to the invoice.
                        /// 
                        if (IsTransactionState == TransactionState.Pending)
                        {
                            /// Refresh the invoice Payments collection in case the invoice items rate or quanity changed
                            /// which in turn will change the invoice amount and raise another property_changed event.
                            /// We also remove any PxI's from the DC's pending changeset because they will need to
                            /// also be recreated with the current invoice values.
                            /// 
                            dc.Refresh(RefreshMode.OverwriteCurrentValues, TheInvoice.Payments);

                            /// When the invoice has PxI's, and the amount is being updated, they
                            /// need to be removed from the DC's pending transaction. They will be recreated
                            /// when the payment(s) are applied to the invoice.
                            /// 
                            if (TheInvoice.HasPXIs)
                            {
                                foreach (Payment_X_Invoice p in TheInvoice.Payment_X_Invoices)
                                {
                                    dc.GetTable(p.GetType()).DeleteOnSubmit(p);
                                }
                            }
                        }
                        /// Iterate the Payments collection to apply equity balance(s)
                        /// to this invoice. Because the invoice Amount can change, by modifying
                        /// an invoice items's rate or quanity, we play it safe and refresh
                        /// the payment, and clear out the PxI collection each time before applying
                        /// a payment.  This keeps from corrupting the payment values if rate/quanity
                        /// is modified.
                        /// 
                        int ndx = 0;
                        while (ndx < TheInvoice.Payments.Count() && TheInvoice.BalanceDue > 0)
                        {
                            ThePayment = TheInvoice.Payments[ndx];
                            ThePayment.PaymentMsg.Visibility = Visibility.Hidden;
                            Financial.ApplyPayment(ThePayment, TheInvoice, TransactionType.Invoice);
                            TheInvoice.IsPaymentApplied = true;
                            ++ndx;
                        }

                        /// For new invoices only: Now that payments have been applied, we need to add the invoice's PxI
                        /// collection to the DC's pending insert transaction. Edited invoices will be processed
                        /// through the DC's pending update transaction.
                        /// 
                        if (IsTransactionState == TransactionState.Pending)
                        {
                            dc.Payment_X_Invoices.InsertAllOnSubmit(TheInvoice.Payment_X_Invoices);
                        }

                        /// Lastly, if the invoice is paid off, check to see if the invoice included
                        /// a golf cart sticker.
                        /// 
                        if (0 == TheInvoice.BalanceDue)
                        {
                            /// Check to see if the invoice has a Golf Cart Sticker...
                            /// 
                            CheckForGolfCartSticker(TransactionType.Invoice);
                        }
                    }
                    /// There are no payments to apply to this invoice....
                    /// 
                    else
                    {
                        /// No payments exist with an equity balance. Therefore, the invoice
                        /// is due in full.
                        /// 
                        TheInvoice.PaymentAmount = 0;
                    }
                    break;
            }

            bool foo = IsDirty;
        }
    }
    #endregion

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class FinancialTransactionViewModel : CommonViewModel, ICommandSink
    {
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

        private void SaveExecute()
        {
            try
            {
                /// Since the Owner account balance can be affected several times by appliying/unapplying
                /// payments, or when an invoice's items rate/quanity change, it is better performance-wise
                /// to update the account balance when the use commits the save changes.
                /// ?? - need to varify this assumption....
                /// 
                SelectedOwner.AccountBalance = PaymentsAppliedAmount;
                SelectedOwner.AccountBalance += InvoiceAppliedAmount;

                ChangeSet cs = dc.GetChangeSet();
                dc.SubmitChanges();
                if (WhatIsBeingProcessed == TransactionType.Invoice)
                {
                    RePrintAction(TheInvoice);
                }
                if (WhatIsBeingProcessed == TransactionType.Payment)
                {
                    RePrintAction(ThePayment);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving changes: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            /// Update the properties that control the UI.....
            /// 

            /// Null or zero (reset) the new payment/invoice objects....
            /// 
            ThePayment = null;
            TheInvoice = null;
            PaymentPriorToEdit = null;
            InvoicePriorToEdit = null;

            /// Set the VM properties that are bound to UI elements so the UI updates.
            /// 
            IsCollapsedInvoice = true;
            IsCollapsedPayments = true;
            IsInvoiceEnabled = true;
            IsPaymentEnabled = true;
            CanSaveExecute = false;

            /// CLear the FinancialTransactions (History) collection and reload it so the UI reflects
            /// the changes.
            /// 
            FinancialTransactions.Clear();
            FinancialTransactions = GetTransactionHistory();
            IsCollapsedHistoryGrid = false;

            /// Force IsDirty to update......
            /// 
            RaisePropertyChanged("DataChanged");
            FinancialTransactions = GetTransactionHistory();
        }

        #region ICommandSink Implementation
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
    public partial class FinancialTransactionViewModel
    {
        /// <summary>
        /// Grid Row Double Click Command
        /// </summary>
        private ICommand _rowDoubleClickCommand;
        public ICommand RowDoubleClickCommand
        {
            get
            {
                return _rowDoubleClickCommand ?? (_rowDoubleClickCommand = new CommandHandlerWparm((object parameter) => RowDoubleClickAction(parameter), true));
            }
        }

        /// <summary>
        /// Grid row double click event to command action
        /// </summary>
        /// <param name="type"></param>
        public void RowDoubleClickAction(object parameter)
        {
            IsTransactionState = TransactionState.Edit;

            /// The user has clicked on a payment or invoice record in the history grid indicating they want to take an action:
            /// Possible actions: Apply payment, edit payment/invoice, delete payment/invoice
            /// The 'parameter' is the SelectedTransaction of type v_OwnerTransaction
            /// 
            if ("Payment" == SelectedTransaction.Type)
            {
                try
                {
                    /// Since the SelectedTransaction is set by the selection from the Transaction History
                    /// grid, it will be of type v_OwnerTransaction.  Therefore, we have to retrieve the
                    /// actual Payment record from the database.
                    /// 
                    ThePayment = (from x in dc.Payments
                                  where x.TransactionID == SelectedTransaction.TransactionID
                                  select x).FirstOrDefault();

                    /// If there are associated invoices...
                    /// 
                    if (ThePayment.Payment_X_Invoices.Count() > 0)
                    {
                        ThePayment.Invoices = GetInvoicesForPayment(ThePayment, SortOrder.Assending);
                        /// For the invoice grid to property indicate the payment is applied we
                        /// have to set the IsApplyToPayment property which is only used for the UI.
                        /// 
                        foreach (Invoice i in ThePayment.Invoices)
                        {
                            i.IsPaymentApplied = true;
                        }
                    }

                    /// If there are no associated invoices, then we need to set the transaction state to EditSkip
                    /// so the warning about affecting invoices is not displayed.
                    /// 
                    if (0 == ThePayment.Invoices.Count())
                    {
                        IsTransactionState = TransactionState.EditSkip;
                    }
                    else
                    {
                        IsTransactionState = TransactionState.Edit;
                    }

                    /// Clone ThePayment so we have the payment values prior to being edited.
                    /// 
                    PaymentPriorToEdit = ThePayment.Clone() as Payment;

                    /// Register the invoices collection for change notification
                    /// 
                    this.RegisterForChangedNotification<Invoice>(ThePayment.Invoices);

                    PaymentAction(null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error retreiving payment: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            /// Action is an Invoice edit
            /// 
            else
            {
                try
                {
                    /// We need to fetch the Invoice record from the database using the 
                    /// SelectedTransaction.TransactionID as the key value.
                    /// 
                    TheInvoice = (from x in dc.Invoices
                                  where x.TransactionID == SelectedTransaction.TransactionID
                                  select x).FirstOrDefault();

                    /// Deserialize the XML string to get the list of invoice items so it can also be copied
                    /// to the InvoiceItems collection which is bound to a UI grid.
                    /// 
                    TheInvoice.InvoiceItems = DeserializeInvoiceItems();

                    /// If the Invoice does not have associated payments we need to new up the payments and PxI collections
                    /// to avoid thowing a null reference error when we check for unapplied payments.
                    /// 
                    if (null == TheInvoice.Payments) { TheInvoice.Payments = new ObservableCollection<Payment>(); }
                    if (null == TheInvoice.Payment_X_Invoices) { TheInvoice.Payment_X_Invoices = new EntitySet<Payment_X_Invoice>(); }

                    /// If there are payments associated to this Invoice, we buffer them to the Invoice's
                    /// payments collection. Otherwise, we look for, and buffer, unapplied payments.
                    /// 
                    if (TheInvoice.Payment_X_Invoices.Count() > 0)
                    {
                        TheInvoice.Payments = GetPaymentsForInvoice(TheInvoice);
                    }
                    else
                    {
                        TheInvoice.Payments = GetAvailablePaymentsForOwner();
                    }

                    /// Clone TheInvoice so we have the payment values prior to being edited.
                    /// 
                    InvoicePriorToEdit = TheInvoice.Clone() as Invoice;
                    InvoicePriorToEdit.InvoiceItems = new ObservableCollection<InvoiceItem>();
                    foreach (InvoiceItem item in TheInvoice.InvoiceItems)
                    {
                        InvoicePriorToEdit.InvoiceItems.Add(item.Clone() as InvoiceItem);
                    }

                    /// Register a change notiification for the Invoice Items collection so changes
                    /// are processed.
                    /// 
                    this.RegisterForChangedNotification<InvoiceItem>(TheInvoice.InvoiceItems);

                    /// Call InvoiceAction with a null paramater value to set the properties
                    /// that affect the UI.
                    /// 
                    InvoiceAction(null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error retreiving Invoice: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        ///// <summary>
        /////  Expands the invoice layout group and collapses the payment layout group
        ///// </summary>
        private ICommand _invoiceCommand;
        public ICommand InvoiceCommand
        {
            get
            {
                return _invoiceCommand ?? (_invoiceCommand = new CommandHandlerWparm((object parameter) => InvoiceAction(parameter), ApplPermissions.CanImport));
            }
        }

        ///// <summary>
        ///// Expands the invoice layout group and collapses the payment layout group
        ///// </summary>
        ///// <param name="type"></param>
        public void InvoiceAction(object parameter)
        {
            WhatIsBeingProcessed = TransactionType.Invoice;
            /// Disable the Invoice and Payment ribbon buttons while we have an active Invoice/Payment in process...
            /// 
            IsInvoiceEnabled = true;
            IsPaymentEnabled = false;
            /// Expand Invoice and collapse Payment......
            IsCollapsedInvoice = false;
            IsCollapsedPayments = true;
            IsCollapsedHistoryGrid = true;

            //InvoiceAppliedAmount = 0m;
            //PaymentsAppliedAmount = 0m;

            /// New up an Invoice object and set the initial values.  
            /// 
            if ("New" == (string)parameter)
            {
                IsTransactionState = TransactionState.New;
                TheInvoice = new Invoice();
                TheInvoice.InvoiceItems = new ObservableCollection<InvoiceItem>();
                TheInvoice.GUID = Guid.NewGuid();
                TheInvoice.OwnerID = SelectedOwner.OwnerID;
                TheInvoice.IssuedDate = DateTime.Now;
                TheInvoice.DueDate = MayFirst;
                TheInvoice.TermsDays = -1;  /// -1 Indicates "Due by May 1st"
                TheInvoice.Amount = 0;
                TheInvoice.BalanceDue = 0;
                TheInvoice.PaymentsApplied = 0;
                TheInvoice.IsPaid = false;
                PXIs = new ObservableCollection<Payment_X_Invoice>();
                TheInvoice.Payments = GetAvailablePaymentsForOwner();
                TheInvoice.InvoiceItems = new ObservableCollection<InvoiceItem>();
            }
            /// Register a change notiification for the Invoice Items collection so changes
            /// are processed.
            /// 
            this.RegisterForChangedNotification<InvoiceItem>(TheInvoice.InvoiceItems);
        }

        ///// <summary>
        /////  Expands the payment layout group and collapses the invoice layout group
        ///// </summary>
        private ICommand _paymentCommand;
        public ICommand PaymentCommand
        {
            get
            {
                return _paymentCommand ?? (_paymentCommand = new CommandHandlerWparm((object parameter) => PaymentAction(parameter), ApplPermissions.CanImport));
            }
        }

        ///// <summary>
        ///// Expands the payment layout group and collapses the invoice layout group
        ///// </summary>
        ///// <param name="type"></param>
        public void PaymentAction(object parameter)
        {
            WhatIsBeingProcessed = TransactionType.Payment;
            /// Disable the Invoice and Payment ribbon buttons while we have an active Invoice/Payment in process...
            IsInvoiceEnabled = false;
            IsPaymentEnabled = false;
            /// Expand Payment and collapse Invoice......
            /// 
            IsCollapsedInvoice = true;
            IsCollapsedPayments = false;
            IsCollapsedHistoryGrid = true;

            if ("New" == (string)parameter)
            {
                IsTransactionState = TransactionState.New;
                ThePayment = new Payment();
                ThePayment.GUID = Guid.NewGuid();
                ThePayment.OwnerID = SelectedOwner.OwnerID;
                ThePayment.PaymentDate = DateTime.Now;
                ThePayment.PaymentMethod = "Check";
                PXIs = new ObservableCollection<Payment_X_Invoice>();
                ThePayment.Invoices = GetOpenInvoicesForOwner();
                this.RegisterForChangedNotification<Invoice>(ThePayment.Invoices);
            }
            /// NOTE: Transaction edits are processed in the RowDoubleClickAction method
        }

        /// <summary>
        ///  Clears the elements of the financial transaction edit form
        /// </summary>
        private ICommand _cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand ?? (_cancelCommand = new CommandHandlerWparm((object parameter) => CancelAction(parameter), true));
            }
        }

        /// <summary>
        /// Clears the elements of the financial transaction edit form
        /// </summary>
        /// <param name="type"></param>
        public void CancelAction(object parameter)
        {
            if (IsDirty)
            {
                try
                {
                    string transactionType = parameter as string;

                    if ("Invoice" == transactionType)
                    {
                        dc.Refresh(RefreshMode.OverwriteCurrentValues, TheInvoice);
                        dc.Refresh(RefreshMode.OverwriteCurrentValues, TheInvoice.Payments);
                        dc.Refresh(RefreshMode.OverwriteCurrentValues, TheInvoice.Payment_X_Invoices);
                    }
                    else
                    {
                        dc.Refresh(RefreshMode.OverwriteCurrentValues, ThePayment);
                        dc.Refresh(RefreshMode.OverwriteCurrentValues, ThePayment.Invoices);
                        dc.Refresh(RefreshMode.OverwriteCurrentValues, ThePayment.Payment_X_Invoices);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Null Reference")) { return; }
                    else
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            /// Null out (reset) the new payment/invoice objects....
            /// 
            ThePayment = null;
            TheInvoice = null;

            /// Set the VM properties that are bound to UI elements so the UI updates.
            /// 
            IsCollapsedInvoice = true;
            IsCollapsedPayments = true;
            IsCollapsedHistoryGrid = false;
            IsInvoiceEnabled = true;
            IsPaymentEnabled = true;
            CanSaveExecute = false;
            //ChangeSet cs = dc.GetChangeSet();
            bool foo = IsDirty;
        }

        /// <summary>
        ///  Delete Transaction Command 
        /// </summary>
        private ICommand _deleteTransactionCommand;
        public ICommand DeleteTransactionCommand
        {
            get
            {
                return _deleteTransactionCommand ?? (_deleteTransactionCommand = new CommandHandlerWparm((object parameter) => DeleteTransactionAction(parameter), ApplPermissions.CanEditOwner));
            }
        }

        /// <summary>
        /// Delete Transaction Action
        /// </summary>
        /// <param name="type"></param>
        public void DeleteTransactionAction(object parameter)
        {
            v_OwnerTransaction t = parameter as v_OwnerTransaction;

            TransactionType transactionType = TransactionType.None;
            MessageBoxResult result = MessageBoxResult.None;

            /// If it's a Payment...
            /// 
            if (t.Type == "Payment")
            {
                transactionType = TransactionType.Payment;
                /// Since the SelectedTransaction is set by the selection from the Transaction History
                /// grid, it will be of type v_OwnerTransaction.  Therefore, we have to retrieve the
                /// actual Payment record from the database.
                /// 
                ThePayment = (from x in dc.Payments
                              where x.TransactionID == SelectedTransaction.TransactionID
                              select x).FirstOrDefault();

                /// Check to see if there are invoices associated with the Payment. If payments exist,
                /// raise a warning message.
                /// 
                ThePayment.Invoices = GetInvoicesForPayment(ThePayment, SortOrder.Decending);

                if (ThePayment.Invoices.Count() > 0)
                {
                    if (null != ThePayment.Memo && ThePayment.Memo.Contains("QuickBooks"))
                    {
                        MessageBox.Show("You cannot delete the initial record.", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        result = MessageBox.Show("Deleting the Payment will affect any associated Invoice(s).\n" +
                            "It will also permanently remove the payment from the account.\n" +
                            "Are you sure you want to delete the transaction?", "Warning",
                            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    }
                }
                /// When there are no associations, set the response to "Yes" so the deletion will be processed.
                /// 
                else
                {
                    result = MessageBoxResult.Yes;
                }
            }
            /// If it's an Invoice......
            /// 
            else
            {
                transactionType = TransactionType.Invoice;
                /// Since the SelectedTransaction is set by the selection from the Transaction History
                /// grid, it will be of type v_OwnerTransaction.  Therefore, we have to retrieve the
                /// actual Payment record from the database.
                /// 
                TheInvoice = (from x in dc.Invoices
                              where x.TransactionID == SelectedTransaction.TransactionID
                              select x).FirstOrDefault();

                TheInvoice.InvoiceItems = DeserializeInvoiceItems();

                /// Check to see if there are payments associated with the Invoice. If payments exist,
                /// raise a warning message.
                /// 
                TheInvoice.Payments = GetPaymentsForInvoice(TheInvoice);
                if (TheInvoice.Payments.Count() > 0)
                {
                    /// 
                    ///
                    if (null != TheInvoice.Memo && TheInvoice.Memo.Contains("Sum balance"))
                    {
                        MessageBox.Show("You cannot delete the initial record.", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        result = MessageBox.Show("Deleting the Invoice will affect any associated Payment(s).\n" +
                        "It will also permanently remove the invoice from the account.\n" +
                        "Are you sure you want to delete the transaction?", "Warning",
                        MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                    }
                }
                /// When there are no associations, set the response to "Yes" so the deletion will be processed.
                /// 
                else
                {
                    result = MessageBoxResult.Yes;
                }
            }

            /// Depending on the user's choice....
            /// 
            switch (result)
            {
                case MessageBoxResult.Yes:
                    this.IsBusy = true;
                    /// Does the Payment/Invoice have associations?
                    /// 
                    if (transactionType == TransactionType.Payment)
                    {
                        if (ThePayment.HasInvoices)
                        {
                            /// Unapply the payment from any associated invoices
                            /// 
                            foreach (Invoice i in ThePayment.Invoices)
                            {
                                TheInvoice = i;
                                TheInvoice.InvoiceItems = DeserializeInvoiceItems();
                                PXI = Financial.UnapplyPayment(ThePayment, TheInvoice, TransactionType.Payment);

                                /// If the invoice being unapplied from this payment was invoiced
                                /// for a golf cart sticker, we need to reverse (remove) the payment
                                /// from the golf cart table to indicate it is unpaid.
                                /// 
                                var gcItem = (from x in TheInvoice.InvoiceItems
                                              where x.Item == "Golf Cart Sticker"
                                              select x);

                                if (null != gcItem)
                                {
                                    GolfCart gc = (from g in dc.GolfCarts
                                                   where g.OwnerID == TheInvoice.OwnerID &&
                                                   g.Year == CurrentSeason.TimePeriod
                                                   select g).FirstOrDefault();

                                    gc.IsPaid = false;
                                    gc.PaymentDate = null;
                                }
                            }

                            /// We need to delete the PxI records associated to the payment
                            /// 
                            dc.Payments.DeleteOnSubmit(ThePayment);
                            dc.Payment_X_Invoices.DeleteAllOnSubmit(ThePayment.Payment_X_Invoices);
                            AllPayments.Remove(ThePayment);

                            //#if DEBUG
                            //                            ChangeSet cs = dc.GetChangeSet();
                            //#endif
                        }
                        else
                        {
                            /// Add the absolute value of the payment amount back to the account balance.
                            /// Keep in mind, payments are entered as negative values.
                            /// 
                            dc.Payments.DeleteOnSubmit(ThePayment);
                            AllPayments.Remove(ThePayment);
                        }
                    }
                    if (transactionType == TransactionType.Invoice)
                    {
                        if (TheInvoice.Payments.Count() > 0)
                        {
                            /// Unapply the invoice from any associated payments
                            /// 
                            foreach (Payment payment in TheInvoice.Payments)
                            {
                                ThePayment = payment;
                                PXI = Financial.UnapplyPayment(ThePayment, TheInvoice, TransactionType.Invoice);
                            }
                        }

                        /// Check the Invoice Items collection to see if the invoice has a golf cart sticker.
                        /// 
                        TheInvoice.InvoiceItems = DeserializeInvoiceItems();
                        foreach (InvoiceItem item in TheInvoice.InvoiceItems)
                        {
                            if (item.Item == "Golf Cart Sticker")
                            {
                                var gc = (from x in dc.GolfCarts
                                          where x.OwnerID == TheInvoice.OwnerID &&
                                          x.Year == CurrentSeason.TimePeriod
                                          select x);

                                /// TO-DO: What do we do if they have paid for, and picked up, a sticker, and are now deleting
                                /// the invoice?

                                if (null != gc)
                                {
                                    MessageBoxResult res = MessageBoxResult.None;
                                    foreach (GolfCart g in gc)
                                    {
                                        if (g.IsReceived)
                                        {
                                            res = MessageBox.Show("NOTICE: This member has already received their golf cart sticker. By deleting " +
                                                "this transaction they are in essence getting their sticker for free. Click <OK> to delete", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Hand);
                                            if (res == MessageBoxResult.OK)
                                            {
                                                dc.GolfCarts.DeleteOnSubmit(g);
                                            }
                                        }
                                        /// It is paid for, but hasn't been picked up yet so it is safe to delete
                                        /// the record from the GolfCarts table.
                                        /// 
                                        else
                                        {
                                            dc.GolfCarts.DeleteOnSubmit(g);
                                        }
                                    }
                                }
                            }
                        }

                        dc.Invoices.DeleteOnSubmit(TheInvoice);
                        dc.Payment_X_Invoices.DeleteAllOnSubmit(TheInvoice.Payment_X_Invoices);
                        AllInvoices.Remove(TheInvoice);
                    }

                    /// Remove the SelectedTransaction from the in memory financial transaction collection
                    /// so it is reflected on the UI.
                    /// 
                    FinancialTransactions.Remove(SelectedTransaction);
                    bool foo = IsDirty;
                    this.IsBusy = false;
                    break;
                case MessageBoxResult.No:
                    break;
            }

            /// Set the transaction type to "None" to indicate we are not generating a PDF
            /// on Save.
            WhatIsBeingProcessed = TransactionType.None;
        }

        /// <summary>
        ///  Delete Invoice Item Command 
        /// </summary>
        private ICommand _deleteInvoiceItemCommand;
        public ICommand DeleteInvoiceItemCommand
        {
            get
            {
                return _deleteInvoiceItemCommand ?? (_deleteInvoiceItemCommand = new CommandHandlerWparm((object parameter) => DeleteInvoiceItemAction(parameter), true));
            }
        }

        /// <summary>
        /// Delete Invoice Item Action
        /// </summary>
        /// <param name="type"></param>
        public void DeleteInvoiceItemAction(object parameter)
        {
            InvoiceItem invoiceItem = parameter as InvoiceItem;
            TheInvoice.InvoiceItems.Remove(invoiceItem);
            /// Although the InvoiceItems collection is registered for collection changed,
            /// the removal of an item from the collection does not raise the event. Therefore,
            /// we raise it manually so the Invoice.Amount is recalculated after the item
            /// is removed.
            /// 
            PropertyChangedEventArgs e = new PropertyChangedEventArgs("Remove");
            ListItem_PropertyChanged(invoiceItem, e);
        }

        ///// <summary>
        /////  Updates the payment method when the value of the radio button group changes
        ///// </summary>
        private ICommand _radioButtonCommand;
        public ICommand RadioButtonCommand
        {
            get
            {
                return _radioButtonCommand ?? (_radioButtonCommand = new CommandHandlerWparm((object parameter) => RadioButtonAction(parameter), true));
            }
        }

        ///// <summary>
        ///// Updates the payment method when the value of the radio button group changes
        ///// </summary>
        ///// <param name="type"></param>
        public void RadioButtonAction(object parameter)
        {
            ThePayment.PaymentMethod = parameter as string;
        }

        /// <summary>
        ///  Re-Print Command 
        /// </summary>
        private ICommand _rePrintCommand;
        public ICommand ReprintCommand
        {
            get
            {
                return _rePrintCommand ?? (_rePrintCommand = new CommandHandlerWparm((object parameter) => RePrintAction(parameter), ApplPermissions.CanImport));
            }
        }

        /// <summary>
        /// Re-Print Transaction Action
        /// </summary>
        /// <param name="type"></param>
        public void RePrintAction(object parameter)
        {
            int transactionID = 0;

            if (parameter.GetType() == typeof(Invoice))
            {
                transactionID = TheInvoice.TransactionID;
            }
            else if (parameter.GetType() == typeof(Payment))
            {
                transactionID = ThePayment.TransactionID;
            }
            else if (parameter.GetType() == typeof(v_OwnerTransaction))
            {
                transactionID = SelectedTransaction.TransactionID;
            }
            else
            {
                transactionID = 0;
            }

            /// Get the MVVM binder for this viewmodel.  We need reference to the ViewModel's View
            /// in order to display the transaction report.
            ///
            foreach (MvvmBinder m in Host.OpenMvvmBinders)
            {
                if (m.ViewModel == this)
                {
                    Binder = m;
                    break;
                }
            }

            if (WhatIsBeingProcessed == TransactionType.Invoice)
            {
                Reports.StandardInvoiceReport report = new Reports.StandardInvoiceReport();

                /// The Xtra report requires a collection type with an interface (IEnumerable, IList, etc)
                /// A Linq entity set satisfies the requirement, so we query to get the selected invoice
                /// 
                var invoices = (from x in dc.Invoices
                                where x.TransactionID == transactionID
                                select x);

                /// Although we only returned a single invoice, it is in a collection, so it has
                /// to be iterated over.
                /// 
                foreach (Object o in invoices)
                {
                    /// Deserilize the invoice items so it can be put into it's own collection.
                    /// The collection is then used by the Xtra report to show each invoice item.
                    /// 
                    TheInvoice = o as Invoice;
                    TheInvoice.Owner = SelectedOwner;
                    TheInvoice.Season = CurrentSeason;
                    TheInvoice.InvoiceItems = DeserializeInvoiceItems();
                }

                /// Assign the 'invoices' entity set to the report's datasource property
                /// Create and display the FinancialTransaction Recepit
                /// 
                report.DataSource = invoices;
                PrintHelper.ShowPrintPreview((HVCC.Shell.Views.FinancialTransactionView)Binder.View, report);
            }
            if (WhatIsBeingProcessed == TransactionType.Payment)
            {
                Reports.PaymentReceiptReport report = new Reports.PaymentReceiptReport();

                /// The Xtra report requires a collection type with an interface (IEnumerable, IList, etc)
                /// A Linq entity set satisfies the requirement, so we query to get the selected invoice
                /// 
                var payments = (from x in dc.Payments
                                where x.TransactionID == transactionID
                                select x);

                /// Although we only returned a single invoice, it is in a collection, so it has
                /// to be iterated over.
                /// 
                foreach (Object p in payments)
                {
                    ThePayment = p as Payment;
                    ThePayment.Owner = SelectedOwner;
                }

                /// Assign the 'payment' entity set to the report's datasource property
                /// Create and display the FinancialTransaction Recepit
                /// 
                report.DataSource = payments;
                PrintHelper.ShowPrintPreview((HVCC.Shell.Views.FinancialTransactionView)Binder.View, report);
            }
        }

        /// <summary>
        ///  
        /// </summary>
        private ICommand _importBalancesCommand;
        public ICommand ImportBalancesCommand
        {
            get
            {
                return _importBalancesCommand ?? (_importBalancesCommand = new CommandHandlerWparm((object parameter) => ImportBalancesAction(parameter), true));
            }
        }

        public ObservableCollection<Owner> Owners { get; set; }
        public ObservableCollection<FinancialTransaction> OwnerTransactions { get; set; }

        /// <summary>
        /// Clears the elements of the financial transaction edit form
        /// </summary>
        /// <param name="type"></param>
        public void ImportBalancesAction(object parameter)
        {
            Owners = new ObservableCollection<Owner>();
            OwnerTransactions = new ObservableCollection<FinancialTransaction>();

            var _owners = (from x in dc.Owners
                           where x.IsCurrentOwner == true
                           //where x.OwnerID == 000549 //000742  //000087 //000082
                           select x);
            Owners = new ObservableCollection<Owner>(_owners);

            IsBusy = true;

            foreach (Owner selectedOwner in Owners)
            {
                ProcessingOwnerID =  selectedOwner.OwnerID;

                this.SelectedOwner = (from x in this.dc.Owners
                                      where x.OwnerID == ProcessingOwnerID
                                      select x).FirstOrDefault();

                this.SelectedOwner.AccountBalance = 0;

                var _transactions = (from t in dc.FinancialTransactions
                                     where t.OwnerID == SelectedOwner.OwnerID
                                     select t);

                AllInvoices = new ObservableCollection<Invoice>();
                AllPayments = new ObservableCollection<Payment>();

                ObservableCollection<FinancialTransaction> tList = new ObservableCollection<FinancialTransaction>();
                tList = new ObservableCollection<FinancialTransaction>(_transactions);

                ChangeSet cs = dc.GetChangeSet();
                decimal accountBalance = 0m;
                foreach (FinancialTransaction x in tList)
                {

                    IsTransactionState = TransactionState.New;
                    TheInvoice = new Invoice();
                    InvoiceItem invoiceItem = new InvoiceItem();
                    TheInvoice.InvoiceItems = new ObservableCollection<InvoiceItem>();
                    TheInvoice.Payments = new ObservableCollection<Payment>();

                    ThePayment = new Payment();
                    ThePayment.Invoices = new ObservableCollection<Invoice>();
                    PendingPmtAmount = 0;

                    if ("Opening Balance" == x.TransactionAppliesTo)
                    {
                        if (x.Balance > 0) /// They have an outstanding ballance owed
                        {
                            accountBalance = x.Balance;
                            invoiceItem = new InvoiceItem()
                            {
                                Item = "Opening Balance"
                            ,
                                Description = "QuickBooks balance when account was established"
                            ,
                                Quanity = 1
                            ,
                                Rate = TheInvoice.Amount
                            ,
                                Amount = TheInvoice.Amount
                            };
                            TheInvoice.InvoiceItems.Add(invoiceItem);

                            TheInvoice.OwnerID = SelectedOwner.OwnerID;
                            TheInvoice.BalanceDue = x.Balance;
                            TheInvoice.TermsDays = 0;
                            TheInvoice.TermsDescriptive = "Due Now";
                            TheInvoice.DueDate = x.TransactionDate;
                            TheInvoice.GUID = Guid.NewGuid();
                            TheInvoice.IsPaid = false;
                            TheInvoice.IssuedDate = x.TransactionDate;
                            TheInvoice.PaymentsApplied = 0m;
                            TheInvoice.ItemDetails = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
                            TheInvoice.Memo = x.Comment;
                            TheInvoice.Payments = GetAvailablePaymentsForOwner();
                            TheInvoice.Amount = x.Balance;

                            this.SelectedOwner.AccountBalance = accountBalance;

                        AllInvoices.Add(TheInvoice);
                            dc.Invoices.InsertOnSubmit(TheInvoice);
                        }
                        else /// (x.Balance >= 0)  They have a zero balance or credit balance
                        {
                            accountBalance = x.Balance;

                            ThePayment.OwnerID = SelectedOwner.OwnerID;
                            ThePayment.GUID = Guid.NewGuid();
                            ThePayment.PaymentDate = x.TransactionDate;
                            ThePayment.PaymentMethod = x.TransactionMethod;
                            ThePayment.ReceiptNumber = x.ReceiptNumber;
                            ThePayment.Memo = "QuickBooks balance when account was established";
                            if (x.Balance == 0)
                            {
                                ThePayment.IsApplied = true;
                            }
                            else
                            {
                                ThePayment.IsApplied = false;
                            }
                            ThePayment.Amount = Math.Abs((decimal)x.Balance);

                            this.SelectedOwner.AccountBalance = accountBalance;

                        AllPayments.Add(ThePayment);
                            dc.Payments.InsertOnSubmit(ThePayment);
                        }
                        cs = dc.GetChangeSet();
                        dc.SubmitChanges();
                    }
                    /// It's an Invoice......
                    else if ((null != x.DebitAmount || x.DebitAmount > 0) && (null == x.CreditAmount || 0m == x.CreditAmount))
                    {
                        string[] sItems = x.TransactionAppliesTo.Split(' ');
                        foreach (string i in sItems)
                        {
                            invoiceItem = new InvoiceItem();

                            string[] a = i.Split(':');
                            invoiceItem.Item = a[0];
                            invoiceItem.Amount = Decimal.Parse(a[1].Replace('$', '0'));
                            invoiceItem.Rate = 1.0m;
                            switch (invoiceItem.Item)
                            {
                                case "Account":
                                    invoiceItem.Rate = 0m;
                                    invoiceItem.Quanity = 1;
                                    break;
                                case "Dues":
                                    invoiceItem.Rate = 378.00m;
                                    break;
                                case "Assessment":
                                    invoiceItem.Rate = 55.00m;
                                    break;
                                case "LateFee":
                                    invoiceItem.Rate = 20.00m;
                                    break;
                                case "GolfCart":
                                case "CartFee":
                                    invoiceItem.Rate = 50.00m;
                                    break;
                                case "Reconnect":
                                    invoiceItem.Rate = 150.00m;
                                    break;
                                case "LienFee":
                                    invoiceItem.Rate = 150.00m;
                                    break;
                                case "Other":
                                    invoiceItem.Rate = invoiceItem.Amount;
                                    break;
                                default:
                                    break;
                            }
                            /// If this is a new account, the values will be zero...
                            /// 
                            if (0m != invoiceItem.Rate || 0m != invoiceItem.Amount)
                            {
                                int mod = (int)(invoiceItem.Amount % invoiceItem.Rate);
                                if (mod != 0)
                                {
                                    invoiceItem.Rate = invoiceItem.Amount;
                                    invoiceItem.Quanity = 1;
                                }
                                else
                                {
                                    invoiceItem.Quanity = (int)(invoiceItem.Amount / invoiceItem.Rate);
                                }
                            }
                            TheInvoice.InvoiceItems.Add(invoiceItem);
                        }

                        TheInvoice.OwnerID = SelectedOwner.OwnerID;
                        TheInvoice.TermsDays = 0;
                        TheInvoice.TermsDescriptive = "Due Now";
                        TheInvoice.DueDate = x.TransactionDate;
                        TheInvoice.GUID = Guid.NewGuid();
                        TheInvoice.IsPaid = false;
                        TheInvoice.IssuedDate = x.TransactionDate;
                        TheInvoice.ItemDetails = TheInvoice.InvoiceItems.ToArray().XmlSerializeToString();
                        TheInvoice.Memo = x.Comment;
                        TheInvoice.PaymentsApplied = 0m;

                        /// When the invoice amount is changed, it will be added to the DC's transaction, and
                        /// payment processing will be attempted.  
                        /// 
                        TheInvoice.Payments = GetAvailablePaymentsForOwner();
                        TheInvoice.Amount = (decimal)x.DebitAmount;

                        this.SelectedOwner.AccountBalance += TheInvoice.Amount;
                        //cs = dc.GetChangeSet();
                        dc.SubmitChanges();
                    }
                    /// Else it's a payment.....
                    /// Payments are automaticly applied to open invoices, so we do not have to worry about
                    /// applying payments here.
                    /// 
                    else if ((null != x.CreditAmount || x.CreditAmount > 0) && (null == x.DebitAmount || 0m == x.DebitAmount))
                    {
                        ThePayment.OwnerID = SelectedOwner.OwnerID;
                        ThePayment.GUID = Guid.NewGuid();
                        ThePayment.PaymentDate = x.TransactionDate;
                        ThePayment.PaymentMethod = x.TransactionMethod;
                        ThePayment.ReceiptNumber = x.ReceiptNumber;
                        ThePayment.Memo = x.Comment;
                        ThePayment.IsApplied = false;

                        /// When the payment amount changes, it will be added to the DC transaction, and
                        /// invoice processing will be attempted.
                        /// 
                        ThePayment.Invoices = GetOpenInvoicesForOwner();
                        ThePayment.Amount = (decimal)x.CreditAmount;

                        this.SelectedOwner.AccountBalance += ThePayment.Amount;
                        //cs = dc.GetChangeSet();
                        dc.SubmitChanges();
                    }
                    else
                    {
                        ;
                    }

                    TheInvoice.Payments = null;
                    TheInvoice.InvoiceItems = null;
                    TheInvoice = null;

                    ThePayment.Invoices = null;
                    ThePayment = null;

                }

                AllInvoices = null;
                AllPayments = null;
            }
            bool foo = IsDirty;
            IsBusy = false;
        }

        /// <summary>
        ///  Command Template
        /// </summary>
        private ICommand _templateCommand;
        public ICommand TemplateCommand
        {
            get
            {
                return _templateCommand ?? (_templateCommand = new CommandHandlerWparm((object parameter) => TemplateAction(parameter), ApplPermissions.CanImport));
            }
        }

        /// <summary>
        /// Facility Usage by date range report
        /// </summary>
        /// <param name="type"></param>
        public void TemplateAction(object parameter)
        {
        }
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
#region public partial class OwnerInvoiceViewModel : IDisposable
    public partial class FinancialTransactionViewModel : IDisposable
    {
        // Resources that must be disposed:
        private HVCCDataContext dc = null;

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
            // No op.
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TableForm"/> class.  (a.k.a. destructor)
        /// </summary>
        ~FinancialTransactionViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}
