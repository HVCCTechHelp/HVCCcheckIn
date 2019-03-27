using DevExpress.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using static HVCC.Shell.Common.Resources.Enumerations;

namespace HVCC.Shell.Models.Financial
{
    /// <summary>
    /// UI element that sets values to indicate if a payment is under/over the account balance due
    /// </summary>
    public class PaymentMessage : BindableBase
    {
        private Visibility _visibility = Visibility.Hidden;
        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    RaisePropertyChanged("Visibility");
                }
            }
        }

        private string _header = string.Empty;
        public string Header
        {
            get { return _header; }
            set
            {
                if (_header != value)
                {
                    _header = value;
                    RaisePropertyChanged("Header");
                }
            }
        }

        private string _label = string.Empty;
        public string Label
        {
            get { return _label; }
            set
            {
                if (_label != value)
                {
                    _label = value;
                    RaisePropertyChanged("Label");
                }
            }
        }

        private string _textBlock = string.Empty;
        public string TextBlock
        {
            get { return _textBlock; }
            set
            {
                if (_textBlock != value)
                {
                    _textBlock = value;
                    RaisePropertyChanged("TextBlock");
                }
            }
        }
    }

    /// <summary>
    /// Describes the properties of an Invoice Item
    /// </summary>
    public partial class InvoiceItem : BindableBase, ICloneable
    {
        private string _item = string.Empty;
        public string Item
        {
            get
            {
                return this._item;
            }
            set
            {
                if ((this._item != value))
                {
                    this._item = value;
                    RaisePropertyChanged("Item");
                }
            }
        }

        private string _description = null;
        public string Description
        {
            get
            {
                return this._description;
            }
            set
            {
                if ((this._description != value))
                {
                    this._description = value;
                    RaisePropertyChanged("Description");
                }
            }
        }

        private int _quanity = 0;
        public int Quanity
        {
            get
            {
                return this._quanity;
            }
            set
            {
                if ((this._quanity != value))
                {
                    this._quanity = value;
                    RaisePropertyChanged("Quanity");
                }
            }
        }

        public decimal _rate = 0m;
        public decimal Rate
        {
            get
            {
                return _rate;
            }
            set
            {
                if (_rate != value)
                {
                    _rate = value;
                    RaisePropertiesChanged("Rate");
                }
            }
        }

        public decimal Amount { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Encapsalates the list of invoice items collection so it can be deserlized back into a list
    /// </summary>
    [XmlRoot("ArrayOfInvoiceItem")]
    public class InvoiceItemList
    {
        public InvoiceItemList() { Items = new List<InvoiceItem>(); }
        [XmlElement("InvoiceItem")]
        public List<InvoiceItem> Items { get; set; }
    }

    /// <summary>
    /// Describes a payment term in days and descriptive text
    /// </summary>
    public partial class TermsList : BindableBase
    {
        public string DescriptiveTerm { get; set; }
        public int Days { get; set; }
    }

    /// <summary>
    /// Static Methods: ApplyPayment, UnapplyPayment
    /// </summary>
    public partial class Financial
    {
        /// <summary>
        /// Applies a payment to an open invoice
        /// </summary>
        /// <param name="ThePayment"></param>
        /// <param name="TheInvoice"></param>
        /// <param name="transactionType"></param>
        /// <returns>Returns the amount applied to the invoice</returns>
        public static void ApplyPayment(Payment ThePayment, Invoice TheInvoice, TransactionType transactionType)
        {
            try
            {
                /// When the equity balance is greater than, or equal to, the invoice balance due.
                /// 
                if (ThePayment.EquityBalance >= TheInvoice.BalanceDue)
                {
                    /// When the payment's equity balance is greater than, or equal to,
                    /// the invoice balance due.
                    /// 
                    TheInvoice.PaymentAmount = TheInvoice.BalanceDue;
                    TheInvoice.PaymentsApplied += TheInvoice.PaymentAmount;
                    TheInvoice.BalanceDue -= TheInvoice.PaymentAmount; // = 0;
                    TheInvoice.IsPaid = true;
                    ThePayment.EquityBalance -= TheInvoice.PaymentAmount;
                    ThePayment.IsApplied = true;
                    if (ThePayment.EquityBalance > 0)
                    {
                        ThePayment.PaymentMsg.Visibility = Visibility.Visible;
                        ThePayment.PaymentMsg.Header = "OVER PAYMENT";
                        ThePayment.PaymentMsg.Label = string.Format("Amount: {0:C}", ThePayment.EquityBalance);
                        ThePayment.PaymentMsg.TextBlock = "The payment amount is greater than the amount owed on the account. The payment will reflect an equity balance.";
                    }
                }
                else
                {
                    /// When the equity balance is less than the invoice balance due
                    /// 
                    decimal underAmt = TheInvoice.BalanceDue - ThePayment.EquityBalance;
                    TheInvoice.PaymentAmount = ThePayment.EquityBalance;
                    TheInvoice.PaymentsApplied += TheInvoice.PaymentAmount;
                    TheInvoice.BalanceDue -= TheInvoice.PaymentAmount;
                    TheInvoice.IsPaid = false;
                    ThePayment.EquityBalance -= TheInvoice.PaymentAmount;
                    ThePayment.IsApplied = true;
                    /// Display the Under Payment Message
                    /// 
                    ThePayment.PaymentMsg.Visibility = Visibility.Visible;
                    ThePayment.PaymentMsg.Header = "UNDER PAYMENT";
                    ThePayment.PaymentMsg.Label = string.Format("Amount: {0:C}", underAmt);
                    ThePayment.PaymentMsg.TextBlock = "The payment amount is less than the amount owed on the account. The payment will reflect a balance due.";
                }

                /// Depending on the transaction type, we have to add it to the correct
                /// object for Linq2SQL to insert it so we do not get a Foreign Key error.
                ///
                Payment_X_Invoice pxi = null;
                if (transactionType == TransactionType.Payment)
                {
                    /// Query the PxI collection to see if a PxI record already exists
                    /// for this Payment/Invoice combination.
                    /// 
                    Payment_X_Invoice ck = (from x in ThePayment.Payment_X_Invoices
                                            where x.InvGUID == TheInvoice.GUID &&
                                            x.PmtGUID == ThePayment.GUID
                                            select x).FirstOrDefault();
                    if (null == ck)
                    {
                        /// Create the PxI record to associate the payment with this invoice
                        /// 
                        pxi = (new Payment_X_Invoice()
                        {
                            OwnerID = ThePayment.OwnerID,
                            PaymentID = ThePayment.TransactionID,
                            PmtGUID = ThePayment.GUID,
                            InvoiceID = TheInvoice.TransactionID,
                            InvGUID = TheInvoice.GUID,
                            PaymentAmount = TheInvoice.PaymentAmount
                        });
                        ThePayment.Payment_X_Invoices.Add(pxi);
                    }
                    else
                    {
                        /// Otherwise, we just need to update the PxI with the amount paid against
                        /// this invoice.
                        ck.PaymentAmount = TheInvoice.PaymentAmount;
                    }
                }
                else
                {
                    /// Query the PxI collection to see if a PxI record already exists
                    /// for this Payment/Invoice combination.
                    /// 
                    Payment_X_Invoice ck = (from x in TheInvoice.Payment_X_Invoices
                                            where x.PmtGUID == ThePayment.GUID &&
                                            x.InvGUID == TheInvoice.GUID
                                            select x).FirstOrDefault();
                    if (null == ck)
                    {
                        /// Create the PxI record to associate the invoice with this payment
                        /// 
                        pxi = (new Payment_X_Invoice()
                        {
                            OwnerID = ThePayment.OwnerID,
                            PaymentID = ThePayment.TransactionID,
                            PmtGUID = ThePayment.GUID,
                            InvoiceID = TheInvoice.TransactionID,
                            InvGUID = TheInvoice.GUID,
                            PaymentAmount = TheInvoice.PaymentAmount
                        });
                        TheInvoice.Payment_X_Invoices.Add(pxi);
                    }
                    else
                    {
                        /// Otherwise, we just need to update the PxI with the amount paid against
                        /// this invoice.
                        ck.PaymentAmount = TheInvoice.PaymentAmount;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying payment to invoice: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Reverses a payment associated with the Invoice. 
        /// </summary>
        /// <param name="thePayment"></param>
        /// <param name="theInvoice"></param>
        public static Payment_X_Invoice UnapplyPayment(Payment thePayment, Invoice theInvoice, TransactionType transactionType)
        {
            /// Retrieve the PxI so the invoice amount paid value can be referenced.
            /// 
            Payment_X_Invoice pxi = null;
            if (transactionType == TransactionType.Payment)
            {
                pxi = (from x in thePayment.Payment_X_Invoices
                       where x.PaymentID == thePayment.TransactionID
                       && x.InvoiceID == theInvoice.TransactionID
                       select x).FirstOrDefault();
            }
            else
            {
                pxi = (from x in theInvoice.Payment_X_Invoices
                       where x.PaymentID == thePayment.TransactionID
                       && x.InvoiceID == theInvoice.TransactionID
                       select x).FirstOrDefault();
            }

            theInvoice.BalanceDue += pxi.PaymentAmount;
            theInvoice.PaymentsApplied -= pxi.PaymentAmount;
            theInvoice.PaymentAmount = 0;
            theInvoice.IsPaid = false;
            theInvoice.IsPaymentApplied = false;

            thePayment.EquityBalance += pxi.PaymentAmount;
            /// If the full amount of the payment was reversed, we update the IsApplied flag
            /// 
            if (thePayment.EquityBalance == Math.Abs(thePayment.Amount))
            {
                thePayment.IsApplied = false;
            }
            return pxi;
        }

        public static decimal GetAccountBalance(Owner owner)
        {
            decimal payments = 0m;
            decimal invoices = 0m;
            
            foreach (Payment p in owner.Payments) { payments += p.Amount; }
            foreach (Invoice i in owner.Invoices) { invoices += i.Amount; }
            return (payments + invoices);
        }
    }
}
