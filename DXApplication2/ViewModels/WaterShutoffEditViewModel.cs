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
    using HVCC.Shell.Reports;
    using System.Diagnostics;
    using System.Windows;
    using System.Text;
    using DevExpress.XtraReports.UI;

    public partial class WaterShutoffEditViewModel : CommonViewModel, ICommandSink
    {
        // The WaterShutoffViewModel should only ever be created by an OwnerEdit Ribbon button event.
        // 'parameter' will be a reference to the SelectedOwner of the OwnerEditViewModel.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public WaterShutoffEditViewModel(IDataContext dc, object parameter, IDataContext pDC = null)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            if (parameter is v_WaterShutoff)
            {
                v_WaterShutoff p = parameter as v_WaterShutoff;
                SelectedOwner = (from x in this.dc.v_OwnerDetails
                                 where x.OwnerID == p.OwnerID
                                 select x).FirstOrDefault();
            }
            else
            {
                Owner p = parameter as Owner;
                SelectedOwner = (from x in this.dc.v_OwnerDetails
                                 where x.OwnerID == p.OwnerID
                                 select x).FirstOrDefault();
            }
            try
            {
                // datacontext is scoped to this ViewModel.
                object trytofind = (from x in this.dc.WaterShutoffs
                                    where x.OwnerID == SelectedOwner.OwnerID
                                    select x).FirstOrDefault();

                if (null == trytofind)
                {
                    WaterShutoff = new WaterShutoff();
                    WaterShutoff.OwnerID = SelectedOwner.OwnerID;
                    IsNew = true;
                }
                else
                {
                    WaterShutoff = trytofind as WaterShutoff;
                    AllNotes =  GetOwnerNotes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error with Owner: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.RegisterCommands();

            WaterShutoff.PropertyChanged +=
                 new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);
            this.PropertyChanged +=
                 new System.ComponentModel.PropertyChangedEventHandler(this.Property_PropertyChanged);

            IsBusy = false;
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

        public bool IsNew { get; set; }

        public string HeaderText
        {
            get
            {
                return string.Format("Owner #{0}", SelectedOwner.OwnerID);
            }
        }

        public string FiscalYear
        {
            get
            {
                 Season fiscalYear = (from x in this.dc.Seasons
                               where x.IsCurrent == true
                               select x).FirstOrDefault();

                return fiscalYear.TimePeriod;
            }
        }

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        private v_OwnerDetail _selectedOwner = new v_OwnerDetail();
        public v_OwnerDetail SelectedOwner
        {
            get
            {
                return _selectedOwner;
            }
            set
            {
                if (null != _selectedOwner)
                {
                    _selectedOwner = value;
                    RaisePropertyChanged("SelectedOwner");
                }
            }
        }

        private WaterShutoff _waterShutoff = null;
        public WaterShutoff WaterShutoff
        {
            get
            {
                return _waterShutoff;
            }
            set
            {
                if (_waterShutoff != value)
                {
                    _waterShutoff = value;
                }
            }
        }

        public string NotesHeader
        {
            get
            {
                return string.Format("HVCC Notes [{0}]", NotesCount);
            }
        }

        private string _allNotes = String.Empty;
        public string AllNotes
        {
            get
            {
                return _allNotes;
            }
            set
            {
                if (_allNotes != value)
                {
                    _allNotes = value;
                }
            }
        }

        public int NotesCount { get; set; }

        public ReportPrintTool ReportViewer { get; set; }

        /// <summary>
        /// Builds a history of property notes
        /// </summary>
        /// <returns></returns>
        private string GetOwnerNotes()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var notes = (from n in dc.Notes
                             where n.OwnerID == this.SelectedOwner.OwnerID
                             orderby n.Entered descending
                             select n);

                NotesCount = notes.Count();

                // Iterate through the notes collection and build a string of the notes in 
                // decending order.  This string will be reflected in the UI as a read-only
                // history of all note entries.
                foreach (Note n in notes)
                {
                    //n.EnteredBy.Remove(0, "HIGHVALLEYCC\\".Count());

                    sb.Append(n.Entered.ToShortDateString()).Append(" ");
                    sb.Append(n.EnteredBy.Remove(0, "HIGHVALLEYCC\\".Count())).Append(" - ");
                    sb.Append(n.Comment);
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Summary
        ///     Raises a property changed event when the NewOwner data is modified
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Property_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                // If a member is 30, 60 or 90 days late, the account will be suspended. Additionally,
                // if 90 or 60 days late is selected, than the other statuses will be set true.  
                // NOTE: this rule may change once the database has been seeded....
                case "IsLate30":
                    if (WaterShutoff.IsLate30)
                    {
                        WaterShutoff.FirstNotificationDate = DateTime.Now;
                        AddNote("30 Days Past Due", WaterShutoff.FirstNotificationDate);
                        GenerateFinancialTransaction(20.00m, "LateFee:$20.00", "30 Days Past Due");
                        Reports.PastDue30Days report = new PastDue30Days();
                        report.Parameters["OwnerID"].Value = SelectedOwner.OwnerID;
                        report.Parameters["InvoiceDate"].Value = DateTime.Now;
                        report.CreateDocument();
                        ReportPrintTool viewer = new ReportPrintTool(report);
                        ReportViewer = viewer;

                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsLate60":
                    if (WaterShutoff.IsLate60)
                    {
                        WaterShutoff.IsMemberSuspended = true;
                        WaterShutoff.IsLate30 = true;
                        WaterShutoff.SecondNotificationDate = DateTime.Now;
                        AddNote("60 Days Past Due", WaterShutoff.SecondNotificationDate);
                        GenerateFinancialTransaction(40.00m, "LateFee:$40.00", "60 Days Past Due");
                        Reports.PastDue60Days report = new PastDue60Days();
                        report.Parameters["OwnerID"].Value = SelectedOwner.OwnerID;
                        report.Parameters["InvoiceDate"].Value = DateTime.Now;
                        report.CreateDocument();
                        ReportPrintTool viewer = new ReportPrintTool(report);
                        ReportViewer = viewer;

                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsLate90":
                    if (WaterShutoff.IsLate90)
                    {
                        WaterShutoff.IsMemberSuspended = true;
                        WaterShutoff.IsLate60 = true;
                        WaterShutoff.IsLate30 = true;
                        WaterShutoff.ShutoffNoticeIssuedDate = DateTime.Now;
                        AddNote("90 Days Past Due", WaterShutoff.ShutoffNoticeIssuedDate);
                        GenerateFinancialTransaction(100.00m, "LateFee:$100.00", "90 Days Past Due");
                        Reports.PastDue90Days report = new PastDue90Days();
                        report.Parameters["OwnerID"].Value = SelectedOwner.OwnerID;
                        report.Parameters["InvoiceDate"].Value = DateTime.Now;
                        report.CreateDocument();
                        ReportPrintTool viewer = new ReportPrintTool(report);
                        ReportViewer = viewer;

                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsMemberSuspended":
                    if (WaterShutoff.IsMemberSuspended)
                    {
                        WaterShutoff.SuspensionDate = DateTime.Now;
                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsWaterShutoff":
                    if (WaterShutoff.IsWaterShutoff)
                    {
                        IsDirty = true;
                    }
                    break;
                case "IsMeterLocked":
                    if (WaterShutoff.IsMeterLocked)
                    {
                        AddNote("Water Meter locked ", DateTime.Now);
                        IsDirty = true;
                    }
                    break;
                case "IsRequestedHearing":
                    if (WaterShutoff.IsMemberRequestedHearing)
                    {
                        WaterShutoff.HearingDate = DateTime.Now;
                        AddNote("Member requested mitigation hearing", WaterShutoff.HearingDate);
                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsInCollections":
                    if (WaterShutoff.IsInCollections)
                    {
                        WaterShutoff.CollectionsDate = DateTime.Now;
                        AddNote("Account sent to collections", WaterShutoff.CollectionsDate);
                        Reports.CollectionsNotice report = new CollectionsNotice();
                        report.Parameters["OwnerID"].Value = SelectedOwner.OwnerID;
                        report.Parameters["InvoiceDate"].Value = WaterShutoff.CollectionsDate;
                        report.CreateDocument();
                        ReportPrintTool viewer = new ReportPrintTool(report);
                        ReportViewer = viewer;

                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsIntentToLien":
                    if (WaterShutoff.IsIntentToLien)
                    {
                        WaterShutoff.IntentToLienDate = DateTime.Now;
                        AddNote("Member has been notified of intent to lien", WaterShutoff.IntentToLienDate);
                        Reports.IntentToLienNotice report = new IntentToLienNotice();
                        report.Parameters["OwnerID"].Value = SelectedOwner.OwnerID;
                        report.Parameters["InvoiceDate"].Value = WaterShutoff.CollectionsDate;
                        report.CreateDocument();
                        ReportPrintTool viewer = new ReportPrintTool(report);
                        ReportViewer = viewer;

                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsLienFiled":
                    if (WaterShutoff.IsLienFiled)
                    {
                        WaterShutoff.LienFiledDate = DateTime.Now;
                        AddNote("Lien filed against property", WaterShutoff.LienFiledDate);
                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                case "IsResolved":
                    if (WaterShutoff.IsResolved)
                    {
                        WaterShutoff.ResolutionDate = DateTime.Now;
                        AddNote("Resolution reached, Water service restored", WaterShutoff.ResolutionDate);
                        IsDirty = true;
                        RaisePropertyChanged("DataChanged");
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Creates a one-line Note.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        private void AddNote(string p, DateTime? d)
        {
            Note note = new Note();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Water Shutoff Status changed: {0} on {1:MM/dd/yyyy}", p, d);
            note.Comment = sb.ToString().Trim();
            note.OwnerID = SelectedOwner.OwnerID;
            dc.Notes.InsertOnSubmit(note);
        }

        private void GenerateFinancialTransaction(decimal amount, string appliesTo, string comment)
        {
            FinancialTransaction transaction = new FinancialTransaction();

            transaction.OwnerID = SelectedOwner.OwnerID;
            transaction.FiscalYear = FiscalYear;
            transaction.DebitAmount = amount;
            transaction.Balance = SelectedOwner.Balance + amount;
            transaction.TransactionDate = DateTime.Now;
            transaction.TransactionMethod = "Machine Generated";
            transaction.TransactionAppliesTo = appliesTo;
            transaction.Comment = comment;
            dc.FinancialTransactions.InsertOnSubmit(transaction);
        }
    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class WaterShutoffEditViewModel : CommonViewModel, ICommandSink
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
                return true;
            }
        }

        /// <summary>
        /// Summary
        ///     Commits data context changes to the database
        /// </summary>
        private void SaveExecute()
        {
            this.IsBusy = true;
            if (IsNew)
            {
                dc.WaterShutoffs.InsertOnSubmit(WaterShutoff);
            }
            ChangeSet cs = dc.GetChangeSet();
            dc.SubmitChanges();
            RaisePropertyChanged("DataChanged");
            if (null != ReportViewer)
            {
                ReportViewer.ShowPreview();
            }
            this.IsBusy = false;
            MessageBox.Show("Changes successfully saved", "Success", MessageBoxButton.OK, MessageBoxImage.None);
            IsDirty = false;
            Host.Execute(HostVerb.Close, this.Caption);
            IsNew = false;
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
    public partial class WaterShutoffEditViewModel : CommonViewModel
    {

        //private bool _canViewParcel = true;
        ///// <summary>
        ///// View Parcel Command
        ///// </summary>
        //private ICommand _viewParcelCommand;
        //public ICommand ViewParcelCommand
        //{
        //    get
        //    {
        //        return _viewParcelCommand ?? (_viewParcelCommand = new CommandHandler(() => ViewParcelAction(), _canViewParcel));
        //    }
        //}

        //public void ViewParcelAction()
        //{
        //    try
        //    {
        //        string absoluteUri = "http://parcels.lewiscountywa.gov/" + SelectedProperty.Parcel;
        //        Process.Start(new ProcessStartInfo(absoluteUri));
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
        //    finally
        //    {
        //    }
        //}

    }

    /*================================================================================================================================================*/
    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class WaterShutoffViewModel : IDisposable
    public partial class WaterShutoffEditViewModel : IDisposable
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
        ~WaterShutoffEditViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
