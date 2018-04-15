namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Printing;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Common.ViewModels;
    using HVCC.Shell.Models;
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

    public partial class FinancialTransactionViewModel : CommonViewModel, ICommandSink
    {
        public FinancialTransactionViewModel(IDataContext dc, object parameter)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            Owner p = parameter as Owner;

            try
            {
                // Fetch the Owner record and its Relationships from the database so the 
                // datacontext will be scoped to this ViewModel.
                SelectedOwner = (from x in this.dc.Owners
                                 where x.OwnerID == p.OwnerID
                                 select x).FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Owner Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ApplPermissions = this.Host.AppPermissions as ApplicationPermission;
            ApplDefault = this.Host.AppDefault as ApplicationDefault;
            this.RegisterCommands();

            this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);
        }

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

        public MvvmBinder Binder { get; set; }

        public string HeaderText
        {
            get
            {
                return string.Format("Post Transaction for Owner #{0}", SelectedOwner.OwnerID);
            }
        }

        private string _duesNullText = string.Empty;
        public string DuesNullText
        {
            get
            {
                return _duesNullText;
            }
            set
            {
                if (value != _duesNullText)
                {
                    _duesNullText = value;
                    RaisePropertyChanged("DuesNullText");
                }
            }
        }

        public ObservableCollection<Season> Seasons
        {
            get
            {
                var list = (from x in dc.Seasons
                            select x);
                return new ObservableCollection<Season>(list);
            }
        }

        public ObservableCollection<string> FiscalYears
        {
            get
            {
                var list = (from x in Seasons
                            where x.IsVisible == true
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
                    SelectedFiscalYear = (from x in Seasons
                                          where x.TimePeriod == _fiscalYear
                                          select x).FirstOrDefault();

                    DuesNullText = string.Format("${0:#.00}", SelectedFiscalYear.AnnualDues);
                    RaisePropertyChanged("FiscalYear");
                }
            }
        }

        private Season _selectedFiscalYear = null;
        public Season SelectedFiscalYear
        {
            get
            {
                return _selectedFiscalYear;
            }
            set
            {
                if (_selectedFiscalYear != value)
                {
                    _selectedFiscalYear = value;
                    FiscalYear = _selectedFiscalYear.TimePeriod;
                    RaisePropertyChanged("SelectedFiscalYear");
                }
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

        private bool _isEditTransaction = false;
        public bool IsEditTransaction
        {
            get { return _isEditTransaction; }
            set
            {
                if (value != _isEditTransaction)
                {
                    _isEditTransaction = value;
                }
            }
        }

        /// <summary>
        /// A collection of Payments 
        /// </summary>
        public ObservableCollection<FinancialTransaction> FinancialTransactions
        {
            get
            {
                var list = (from x in dc.FinancialTransactions
                            where x.OwnerID == SelectedOwner.OwnerID
                            select x).OrderByDescending(x => x.TransactionDate);
                return new ObservableCollection<FinancialTransaction>(list);
            }
        }

        private FinancialTransaction _selectedTransaction = new FinancialTransaction();
        public FinancialTransaction SelectedTransaction
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
                    RaisePropertyChanged("SelectedTransaction");
                }
            }
        }

        public int SelectedTransactionIndex
        {
            get
            {
                int ndx = 0;
                foreach (FinancialTransaction t in FinancialTransactions)
                {
                    if (t == SelectedTransaction) { break; }
                    ++ndx;
                }
                return ndx;
            }
        }

        private FinancialTransaction _newPayment = new FinancialTransaction();
        public FinancialTransaction NewTransaction
        {
            get
            {
                return _newPayment;
            }
            set
            {
                if (_newPayment != value)
                {
                    _newPayment = value;
                    RaisePropertyChanged("NewTransaction");
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

        public decimal AccountBalance
        {
            get
            {
                try
                {
                    var b = SelectedOwner.FinancialTransactions.Select(x => x).LastOrDefault();

                    if (b.Balance > 0)
                    {
                        TextColor = new SolidColorBrush(Colors.DarkRed);
                    }
                    return b.Balance;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return 0m;
                }
            }
        }

        private decimal? _creditAmount = null;
        public decimal? CreditAmount
        {
            get
            { return _creditAmount; }
            set
            {
                if (_creditAmount != value)
                {
                    _creditAmount = value;
                    RaisePropertyChanged("CreditAmount");
                }
            }
        }

        private decimal? _debitAmount = null;
        public decimal? DebitAmount
        {
            get
            { return _debitAmount; }
            set
            {
                if (_debitAmount != value)
                {
                    _debitAmount = value;
                    RaisePropertyChanged("DebitAmount");
                }
            }
        }

        private DateTime _transactionDate = new DateTime();
        public DateTime TransactionDate
        {
            get
            {
                return _transactionDate;
            }
            set
            {
                if (_transactionDate != value)
                {
                    _transactionDate = value;
                    RaisePropertyChanged("TransactionDate");
                }
            }
        }

        private string _transactionMethod = String.Empty;
        public string TransactionMethod
        {
            get
            {
                return _transactionMethod;
            }
            set
            {
                if (_transactionMethod != value)
                {
                    _transactionMethod = value;
                    RaisePropertyChanged("TransactionMethod");
                }
            }
        }

        private StringBuilder _transactionAppliesTo = new StringBuilder();
        public StringBuilder TransactionAppliesTo
        {
            get
            {
                return _transactionAppliesTo;
            }
            set
            {
                if (_transactionAppliesTo != value)
                {
                    _transactionAppliesTo = value;
                    //RaisePropertyChanged("TransactionAppliesTo");
                }
            }
        }

        private string _transactionComment = String.Empty;
        public string TransactionComment
        {
            get
            {
                return _transactionComment;
            }
            set
            {
                if (_transactionComment != value)
                {
                    _transactionComment = value;
                    RaisePropertyChanged("TransactionComment");
                }
            }
        }

        private decimal? _duesAmount = null;
        public decimal? DuesAmount
        {
            get
            {
                return _duesAmount;
            }
            set
            {
                if (_duesAmount != value)
                {
                    _duesAmount = value;
                    string tmp = string.Format("Dues:{0:c} ", value);
                    if (_duesAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }
                    TotalAmount += (decimal)_duesAmount;
                    RaisePropertyChanged("DuesAmount");
                }
            }
        }

        private decimal? _feeAmount = null;
        public decimal? FeeAmount
        {
            get
            {
                return _feeAmount;
            }
            set
            {
                if (_feeAmount != value)
                {
                    _feeAmount = value;
                    string tmp = string.Format("LateFee:{0:c} ", value);
                    if (_feeAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }
                    TotalAmount += (decimal)_feeAmount;
                    RaisePropertyChanged("FeeAmount");
                }
            }
        }

        private int? _golfCartQuanity = null;
        public int? GolfCartQuanity
        {
            get
            {
                return _golfCartQuanity;
            }
            set
            {
                if (_golfCartQuanity != value)
                {
                    _golfCartQuanity = value;
                    CartAmount = _golfCartQuanity * SelectedFiscalYear.CartFee;
                    RaisePropertyChanged("GolfCartQuanity");
                }
            }
        }

        private decimal? _cartAmount = null;
        public decimal? CartAmount
        {
            get
            {
                return _cartAmount;
            }
            set
            {
                if (value != _cartAmount)
                {
                    // Before we update the CartAmount, we need to subtract the current value from the total
                    // so it does not get added again when the amount changes.
                    if (null != _cartAmount)
                    {
                        TotalAmount -= (decimal)_cartAmount;
                    }
                    _cartAmount = value;
                    string tmp = string.Format("CartFee:{0:c} ", value);
                    if (_cartAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    TotalAmount += (decimal)_cartAmount;
                    RaisePropertyChanged("CartAmount");
                }
            }
        }

        private decimal? _assessmentAmount = null;
        public decimal? AssessmentAmount
        {
            get
            {
                return _assessmentAmount;
            }
            set
            {
                if (_assessmentAmount != value)
                {
                    _assessmentAmount = value;
                    string tmp = string.Format("Assessment:{0:c} ", value);
                    if (_assessmentAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    TotalAmount += (decimal)_assessmentAmount;
                    RaisePropertyChanged("AssessmentAmount");
                }
            }
        }

        private decimal? _reconnectAmount = null;
        public decimal? ReconnectAmount
        {
            get
            {
                return _reconnectAmount;
            }
            set
            {
                if (_reconnectAmount != value)
                {
                    _reconnectAmount = value;
                    string tmp = string.Format("Reconnect:{0:c} ", value);
                    if (_reconnectAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    TotalAmount += (decimal)_reconnectAmount;
                    RaisePropertyChanged("ReconnectAmount");
                }
            }
        }

        private decimal? _lienFeeAmount = null;
        public decimal? LienFeeAmount
        {
            get
            {
                return _lienFeeAmount;
            }
            set
            {
                if (_lienFeeAmount != value)
                {
                    _lienFeeAmount = value;
                    string tmp = string.Format("LienFee:{0:c} ", value);
                    if (_lienFeeAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    TotalAmount += (decimal)_lienFeeAmount;
                    RaisePropertyChanged("LienFeeAmount");
                }
            }
        }

        private decimal? _otherAmount = null;
        public decimal? OtherAmount
        {
            get
            {
                return _otherAmount;
            }
            set
            {
                if (_otherAmount != value)
                {
                    _otherAmount = value;
                    string tmp = string.Format("Other:{0:c} ", value);
                    if (_otherAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    TotalAmount += (decimal)_otherAmount;
                    RaisePropertyChanged("OtherAmount");
                }
            }
        }

        private decimal _totalAmount = 0m;
        public decimal TotalAmount
        {
            get
            {
                return _totalAmount;
            }
            set
            {
                if (_totalAmount != value)
                {
                    _totalAmount = value;
                    RaisePropertyChanged("TotalAmount");
                }
            }
        }

        /* ----------------------------------- Style Propertes ---------------------------------------- */
        #region Style Properties

        private GridViewNavigationStyle _navigationStyle = GridViewNavigationStyle.Cell;
        public GridViewNavigationStyle NavigationStyle
        {
            get
            {
                return _navigationStyle;
            }
            set
            {
                if (value != _navigationStyle)
                {
                    this._navigationStyle = value;
                    RaisePropertyChanged("NavigationStyle");
                }
            }
        }

        private NewItemRowPosition _newItemPosition = NewItemRowPosition.None;
        public NewItemRowPosition NewItemPosition
        {
            get
            {
                return _newItemPosition;
            }
            set
            {
                if (value != _newItemPosition)
                {
                    this._newItemPosition = value;
                    RaisePropertyChanged("NewItemPosition");
                }
            }
        }

        /// <summary>
        /// Get TextBox control adornments
        /// </summary>
        private System.Windows.Style _tbStyle = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
        public System.Windows.Style TbStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditProperty)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
                }
            }
        }

        /// <summary>
        /// Get TextEdit control adornments
        /// </summary>
        private System.Windows.Style _teStyle = (System.Windows.Style)App.Current.MainWindow.Resources["TextBoxDisplayStyle"];
        public System.Windows.Style TeStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditOwner)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextEditEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["TextEditDisplayStyle"];
                }
            }
        }

        /// <summary>
        /// Get ComboBoxEdit control adornments
        /// </summary>
        private System.Windows.Style _cbStyle = (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
        public System.Windows.Style CbStyle
        {
            get
            {
                if (this.ApplPermissions.CanEditProperty)
                {
                    System.Windows.Style st = (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                    return (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxEditStyle"];
                }
                else
                {
                    return (System.Windows.Style)App.Current.MainWindow.Resources["ComboBoxDisplayStyle"];
                }
            }
        }
        #endregion

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
            if (e.PropertyName != "DataChanged")
            {
                if (CkIsValid())
                {
                    this.CanSaveExecute = true; // IsDirty;
                    RaisePropertyChanged("DataChanged");
                }
            }
        }

        #endregion
    }

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

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            try
            {
                this.IsBusy = true;
                FinancialTransaction transaction = new FinancialTransaction();
                decimal accountBalance = AccountBalance;
                transaction.Balance = accountBalance;
                Note note = new Note();
                StringBuilder sb = new StringBuilder();

                // If we are editing an existing transaction.....
                if (IsEditTransaction)
                {
                    transaction = SelectedTransaction;
                    transaction.TransactionAppliesTo = String.Empty;

                    string xxx = TransactionAppliesTo.ToString();

                    // adjust balance..... In order to correctly calculate the account balance, we first need to add/subtract the 
                    // previous posted amount (credit/debit) from the current account balance.  The new/adjusted balance will
                    // be calculated below.
                    if (null != transaction.CreditAmount) { accountBalance += (decimal)transaction.CreditAmount; }
                    if (null != transaction.DebitAmount) { accountBalance -= (decimal)transaction.DebitAmount; }
                    transaction.Balance = accountBalance;

                    sb.Append(String.Format("Revised #{0}: ", SelectedTransaction.RowId));
                }

                // Determine if we are posting a credit or a debit.  
                if (null != CreditAmount)
                {
                    DebitAmount = 0;
                    if (null != DuesAmount)
                    { transaction.Balance -= (decimal)DuesAmount; }
                    if (null != FeeAmount)
                    { transaction.Balance -= (decimal)FeeAmount; }
                    if (null != AssessmentAmount)
                    { transaction.Balance -= (decimal)AssessmentAmount; }
                    if (null != CartAmount)
                    { transaction.Balance -= (decimal)CartAmount; }
                    if (null != ReconnectAmount)
                    { transaction.Balance -= (decimal)ReconnectAmount; }
                    if (null != LienFeeAmount)
                    { transaction.Balance -= (decimal)LienFeeAmount; }
                    if (null != OtherAmount)
                    { transaction.Balance -= (decimal)OtherAmount; }

                    sb.Append("Credit ");
                    sb.Append(TransactionAppliesTo.ToString().Trim());
                }
                else
                {
                    CreditAmount = 0;
                    transaction.Balance = accountBalance + (decimal)DebitAmount;
                    sb.Append("Debit ");
                    sb.Append(TransactionAppliesTo.ToString().Trim());
                }

                // Assign the field values to the transaction object......
                transaction.OwnerID = SelectedOwner.OwnerID;
                transaction.FiscalYear = FiscalYear;
                transaction.CreditAmount = (decimal)CreditAmount;
                transaction.DebitAmount = (decimal)DebitAmount;
                transaction.TransactionDate = TransactionDate;
                transaction.TransactionMethod = TransactionMethod;
                transaction.TransactionAppliesTo = TransactionAppliesTo.ToString().Trim();
                transaction.Comment = TransactionComment;

                // If this is a new transaction, we have to add it to the datacontext change set as an insert
                if (!IsEditTransaction)
                {
                    dc.FinancialTransactions.InsertOnSubmit(transaction);
                }

                // Add/Attach a comment to the Owner's Notes table.
                sb.Append(" - ");
                sb.Append(transaction.Comment);
                sb.AppendLine();
                note.OwnerID = transaction.OwnerID;
                note.Comment = sb.ToString().Trim();
                dc.Notes.InsertOnSubmit(note);

                // If there is credit applied to a golf cart, we create or update a Golf Cart record.
                if (0 != CreditAmount && 0 < CartAmount)
                {
                    GolfCart golfCart = new GolfCart();
                    golfCart.OwnerID = SelectedOwner.OwnerID;
                    golfCart.Customer = SelectedOwner.Customer;
                    golfCart.Year = FiscalYear;
                    golfCart.PaymentDate = TransactionDate;
                    golfCart.Quanity = (int)GolfCartQuanity;
                    golfCart.IsPaid = true;
                    dc.GolfCarts.InsertOnSubmit(golfCart);
                }

                // Lastly, if the balance is zero, or a negative (they have a credit to their account), check
                // to see if the account is in a Water Shutoff status.
                if (transaction.Balance >= 0)
                {
                    WaterShutoff wsOff = (from x in dc.WaterShutoffs
                                          where x.OwnerID == SelectedOwner.OwnerID
                                          select x).FirstOrDefault();

                    // If a WS record is found, we set the status to closed with balance paid.
                    if (null != wsOff)
                    {
                        wsOff.IsResolved = true;
                        wsOff.ResolutionDate = DateTime.Now;
                        wsOff.Resolution = "Owner has paid off full balance due.";
                        wsOff.IsMemberSuspended = false;
                    }
                }

                ChangeSet cs = dc.GetChangeSet();
                this.dc.SubmitChanges();

                // Add this transaction to the in-memory Selected owner record so it can be reflected without a 
                // dc refresh from the database.
                SelectedOwner.FinancialTransactions.Add(transaction);

                IsBusy = false;
                CanSaveExecute = false;
                RaisePropertyChanged("DataChanged");

                // I need to buffer the transactions so I can get the RowID of the last transaction; the one that was entered.
                FinancialTransaction ft = null;
                if (!IsEditTransaction)
                {
                    var transactions = (from x in dc.FinancialTransactions
                                        where x.OwnerID == SelectedOwner.OwnerID
                                        orderby x.TransactionDate descending
                                        select x);

                    // Break on the first record, which will be the last record entered.
                    foreach (FinancialTransaction f in transactions)
                    {
                        ft = f;
                        break;
                    }
                }
                else
                {
                    ft = SelectedTransaction;
                }

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

                // Create and display the FinancialTransaction Recepit
                Reports.TransactionReceipt report = new Reports.TransactionReceipt();
                report.Parameters["transactionID"].Value = ft.RowId;
                //string fileName = string.Format(@"D:\Transaction-{0}.PDF", ft.RowId);
                //report.CreateDocument();
                //report.ExportToPdf(fileName);
                PrintHelper.ShowPrintPreview((HVCC.Shell.Views.FinancialTransactionView)Binder.View, report);

                MessageBox.Show("Transaction successfully saved", "Success", MessageBoxButton.OK, MessageBoxImage.None);
                Host.Execute(HostVerb.Close, this.Caption);
            }
            catch (Exception ex)
            {
                IsBusy = false;
                MessageBox.Show("Error saving transaction: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        /// Print Command
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
            if (0 != SelectedTransactionIndex)
            {
                MessageBox.Show("Only the last transaction can be modified.", "Warning", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                FinancialTransaction p = parameter as FinancialTransaction;
                IsEditTransaction = true;
                TransactionDate = p.TransactionDate;
                if (null != p.CreditAmount)
                {
                    // If the amount is $0.00, we need to null the value to make the validation work.  This is because on a new
                    // transaction, the Credit/Debit amounts are null. It's not until it is saved to the DB that it is saved as $0
                    if (0 == p.CreditAmount) { p.CreditAmount = null; }
                    CreditAmount = p.CreditAmount;
                }
                if (null != p.DebitAmount)
                {
                    if (0 == p.DebitAmount) { p.DebitAmount = null; }
                    DebitAmount = p.DebitAmount;
                }

                TransactionMethod = p.TransactionMethod;
                FiscalYear = p.FiscalYear;
                TransactionComment = p.Comment;

                // We remove the "Pool " string from the transaction description so it just leaves "Assessment".  
                // The pool assessment ended in FY18/19.  If in the future, another assessment is added we will 
                // need to still be able to account for it.
                string tmp = p.TransactionAppliesTo.ToString().Replace("Pool ", "");
                String[] substrngs = tmp.Split(' ');

                foreach (string s in substrngs)
                {
                    String[] items = s.Split(':');
                    string amt = items[1].Replace("$", "");

                    switch (items[0])
                    {
                        case "Dues":
                            Decimal.TryParse(amt, out decimal duesAmount);
                            this.DuesAmount = duesAmount;
                            break;
                        case "LateFee":
                            Decimal.TryParse(amt, out decimal feeAmount);
                            this.FeeAmount = feeAmount;
                            break;
                        case "GolfCart":
                        case "CartFee":
                            Decimal.TryParse(amt, out decimal cartAmount);
                            int count = Decimal.ToInt32(cartAmount / SelectedFiscalYear.CartFee);
                            this.GolfCartQuanity = count;
                            break;
                        case "Asessment":
                        case "Assessment":
                            Decimal.TryParse(amt, out decimal assessmentAmount);
                            this.AssessmentAmount = assessmentAmount;
                            break;
                        case "Reconnect":
                            Decimal.TryParse(amt, out decimal reconnectAmount);
                            this.ReconnectAmount = reconnectAmount;
                            break;
                        case "LienFee":
                            Decimal.TryParse(amt, out decimal lienFeeAmount);
                            this.LienFeeAmount = lienFeeAmount;
                            break;
                        case "Other":
                            Decimal.TryParse(amt, out decimal otherAmount);
                            this.OtherAmount = otherAmount;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        ///// <summary>
        /////  Command Template
        ///// </summary>
        //private ICommand _templateCommand;
        //public ICommand TemplateCommand
        //{
        //    get
        //    {
        //        return _templateCommand ?? (_templateCommand = new CommandHandlerWparm((object parameter) => TemplateAction(parameter), ApplPermissions.CanImport));
        //    }
        //}

        ///// <summary>
        ///// Facility Usage by date range report
        ///// </summary>
        ///// <param name="type"></param>
        //public void TemplateAction(object parameter)
        //{
        //}

    }

    /*===============================================================================================================================================*/
    /// <summary>
    /// ViewModel Validation
    /// </summary>

    public partial class FinancialTransactionViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CkIsValid()
        {
            StringBuilder message = new StringBuilder();

            if (
                   ((CreditAmount > 0 && null == DebitAmount))
                || (DebitAmount > 0 && (null == CreditAmount))
                && ((TotalAmount == CreditAmount)
                || (TotalAmount == DebitAmount))
                && (DateTime.Now > TransactionDate)
                && !String.IsNullOrEmpty(FiscalYear)
                && !String.IsNullOrEmpty(TransactionMethod)
                && !String.IsNullOrEmpty(FiscalYear)
                && !String.IsNullOrEmpty(TransactionComment)
               )
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers the required Properties to be validated
        /// </summary>
        string IDataErrorInfo.Error
        {
            get
            {
                //if (!allowValidation) return null;

                IDataErrorInfo iDataErrorInfo = (IDataErrorInfo)this;
                string error = String.Empty;

                //// The following properties must contain data in order to pass basic validation.
                //// Properties must be standalone, not a property of a property (ex. Prop.Prop1)
                error =
                    iDataErrorInfo[BindableBase.GetPropertyName(() => CreditAmount)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => DebitAmount)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => TransactionDate)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => FiscalYear)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => TransactionMethod)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => TotalAmount)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => FiscalYear)]
                    + iDataErrorInfo[BindableBase.GetPropertyName(() => TransactionComment)]
                    ;

                if (!string.IsNullOrEmpty(error))
                {
                    return "Please check input data.";
                    //return error;
                }
                return null;
            }
        }

        public StringBuilder errorMsg = new StringBuilder();
        /// <summary>
        /// Assign the validation rule on based on the property name
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                //// If invoked, will throw validation error if property is null/blank

                //StringBuilder errorMsg = new StringBuilder();

                if (columnName == BindableBase.GetPropertyName(() => CreditAmount))
                {
                    errorMsg.Append(RequiredValidationRule.CheckDecimalInput(() => "CreditAmount", CreditAmount));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => DebitAmount))
                {
                    errorMsg.Append(RequiredValidationRule.CheckDecimalInput(() => "DebitAmount", DebitAmount));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => TransactionDate))
                {
                    errorMsg.Append(RequiredValidationRule.CheckDateInput(() => "TransactionDate", TransactionDate.ToShortDateString()));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => FiscalYear))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "FiscalYear", FiscalYear));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => TransactionMethod))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "TransactionMethod", TransactionMethod));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => TotalAmount))
                {
                    errorMsg.Append(RequiredValidationRule.CheckDecimalInput(() => "TotalAmount", TotalAmount));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => FiscalYear))
                {
                    errorMsg.Append(RequiredValidationRule.CheckNullInput(() => "FiscalYear", FiscalYear));
                    return errorMsg.ToString();
                }
                else if (columnName == BindableBase.GetPropertyName(() => TransactionComment))
                {
                    errorMsg.Append(RequiredValidationRule.CkStateAbbreviation(() => "TransactionComment", TransactionComment));
                    return errorMsg.ToString();
                }
                //// No errors found......
                return null;
            }
        }
        #region IsValid
        /// <summary>
        /// Runs validation on the view model
        /// </summary>
        /// <returns></returns>
        //public bool IsValid()
        //{
        //    StringBuilder message = new StringBuilder();

        //    message.Append(this.EnableValidationAndGetError());


        //    if (!String.IsNullOrEmpty(SelectedProperty.OwnerFName))
        //    {
        //        message.Append(RequiredValidationRule.CheckNullInput(SelectedProperty.OwnerFName, SelectedProperty.OwnerFName));
        //    }

        //    if (!string.IsNullOrEmpty(message.ToString()))
        //    {
        //        return false;
        //    }
        //    return true;
        //}
        #endregion


    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class OwnersEditViewModel : IDisposable
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
