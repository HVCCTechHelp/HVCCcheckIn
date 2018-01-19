namespace HVCC.Shell.ViewModels
{
    using DevExpress.Mvvm;
    using DevExpress.Spreadsheet;
    using DevExpress.Xpf.Grid;
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

        public string HeaderText
        {
            get
            {
                return string.Format("Post Transaction for Owner #{0}", SelectedOwner.OwnerID);
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

        private decimal _creditAmount = 0m;
        public decimal CreditAmount
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

        private decimal _debitAmount = 0m;
        public decimal DebitAmount
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
                    RaisePropertyChanged("TransactionAppliesTo");
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

        private decimal _duesAmount = 0m;
        public decimal DuesAmount
        {
            get
            {
                return _duesAmount;
            }
            set
            {
                if (_duesAmount != value)
                {
                    string tmp = string.Format("Dues:{0:c} ", value);
                    if (_duesAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    _duesAmount = value;
                    TotalAmount += (decimal)_duesAmount;
                }
            }
        }

        private decimal _feeAmount = 0m;
        public decimal FeeAmount
        {
            get
            {
                return _feeAmount;
            }
            set
            {
                if (_feeAmount != value)
                {
                    string tmp = string.Format("Fees:{0:c} ", value);
                    if (_feeAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    _feeAmount = value;
                    TotalAmount += (decimal)_feeAmount;
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
                string tmp = string.Format("CartFee:{0:c} ", value);
                if (_cartAmount != 0)
                {
                    TransactionAppliesTo.Append(tmp);
                }
                else
                {
                    TransactionAppliesTo.Replace(tmp, "");
                }

                if (_cartAmount != value)
                {
                    _cartAmount = value;
                    RaisePropertyChanged("CartAmount");
                    TotalAmount += (decimal)_cartAmount;
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
                    string tmp = string.Format("Assessment:{0:c} ", value);
                    if (_assessmentAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    _assessmentAmount = value;
                    TotalAmount += (decimal)_assessmentAmount;
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
                    string tmp = string.Format("Reconnect:{0:c} ", value);
                    if (_reconnectAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    _reconnectAmount = value;
                    TotalAmount += (decimal)_reconnectAmount;
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
                    string tmp = string.Format("LienFee:{0:c} ", value);
                    if (_lienFeeAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    _lienFeeAmount = value;
                    TotalAmount += (decimal)_lienFeeAmount;
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
                    string tmp = string.Format("Other:{0:c} ", value);
                    if (_otherAmount != 0)
                    {
                        TransactionAppliesTo.Append(tmp);
                    }
                    else
                    {
                        TransactionAppliesTo.Replace(tmp, "");
                    }

                    _otherAmount = value;
                    TotalAmount += (decimal)_otherAmount;
                }
            }
        }

        private decimal _totalAmount = 0;
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
                Note note = new Note();
                StringBuilder sb = new StringBuilder();

                // Determine if we are posting a credit or a debit.  
                if (0 != CreditAmount)
                {
                    // Only Dues and Late Fees credits are deducted from the balance owed, or added
                    // as a positive credit.  All other types of credits are journaled for history.
                    transaction.Balance = AccountBalance - DuesAmount - FeeAmount;
                    sb.Append("Credit ");
                    sb.Append(TransactionAppliesTo.ToString().Trim());
                }
                else
                {
                    transaction.Balance = AccountBalance + DebitAmount;
                    sb.Append("Debit ");
                    sb.Append(TransactionAppliesTo.ToString().Trim());
                }

                // Add this transaction to the DC as an Insert....
                transaction.OwnerID = SelectedOwner.OwnerID;
                transaction.FiscalYear = FiscalYear;
                transaction.CreditAmount = CreditAmount;
                transaction.DebitAmount = DebitAmount;
                transaction.TransactionDate = TransactionDate;
                transaction.TransactionMethod = TransactionMethod;
                transaction.TransactionAppliesTo = TransactionAppliesTo.ToString().Trim();
                transaction.Comment = TransactionComment;
                dc.FinancialTransactions.InsertOnSubmit(transaction);

                // Add/Attach a comment to the Owner's Notes table.
                sb.Append(" - ");
                sb.Append(transaction.Comment);
                sb.AppendLine();
                note.OwnerID = transaction.OwnerID;
                note.Comment = sb.ToString().Trim();
                dc.Notes.InsertOnSubmit(note);

                // If there is credit applied to a golf cart, we create a Golf Cart record.
                if (0 < CartAmount)
                {
                    GolfCart golfCart = new GolfCart();
                    golfCart.OwnerID = SelectedOwner.OwnerID;
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

                this.dc.SubmitChanges();

                SelectedOwner.FinancialTransactions.Add(transaction);

                RaisePropertyChanged("DataChanged");
                CanSaveExecute = IsDirty;
                IsBusy = false;
                MessageBox.Show("Transaction successfully saved", "Success", MessageBoxButton.OK, MessageBoxImage.None);
                Host.Execute(HostVerb.Close, this.Caption);
            }
            catch (Exception ex)
            {
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
            Property p = parameter as Property;
            //Host.Execute(HostVerb.Open, "PropertyEdit", p);
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
                   ((CreditAmount > 0 && 0 == DebitAmount)
                || (DebitAmount > 0 && 0 == CreditAmount))
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
