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

    public partial class WaterShutoffViewModel : CommonViewModel, ICommandSink
    {
        enum IsLate
        {
            Current,
            ThirtyDays,
            SixtyDays,
            NintyDays
        };

        // The WaterShutoffViewModel should only ever be created by an OwnerEdit Ribbon button event.
        // 'parameter' will be a reference to the SelectedOwner of the OwnerEditViewModel.
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="parameter"></param>
        public WaterShutoffViewModel(IDataContext dc, object parameter, IDataContext pDC = null)
        {
            this.dc = dc as HVCCDataContext;
            this.Host = HVCC.Shell.Host.Instance;
            SelectedOwner = parameter as Owner;
            try
            {
                // datacontext is scoped to this ViewModel.
                object trytofind = (from x in this.dc.WaterShutoffs
                                    where x.OwnerID == SelectedOwner.OwnerID
                                    select x).FirstOrDefault();

                if (null == trytofind)
                {
                    WaterShutoff = new WaterShutoff();
                }
                else
                {
                    WaterShutoff = trytofind as WaterShutoff;
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
                Caption = caption[0].TrimEnd(' ') + "* ";
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
                return string.Format("Owner #{0}", SelectedOwner.OwnerID);
            }
        }

        /// <summary>
        /// Currently selected property from a property grid view
        /// </summary>
        private Owner _selectedOwner = new Owner();
        public Owner SelectedOwner
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
                    AllNotes = GetOwnerNotes();
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
                return string.Format("HVCC Notes [{0}]", SelectedOwner.Notes.Count());
            }
        }

        private string _allNotes = string.Empty;
        public string AllNotes
        {
            get
            {
                return this._allNotes;
            }
            set
            {
                if (value != this._allNotes)
                {
                    this._allNotes = value;
                }
            }
        }


        /// <summary>
        /// Builds a history of property notes
        /// </summary>
        /// <returns></returns>
        private string GetOwnerNotes()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                var notes = (from n in SelectedOwner.Notes
                             where n.OwnerID == this.SelectedOwner.OwnerID
                             orderby n.Entered descending
                             select n);

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
                case "IsMemberSuspended":
                    if (WaterShutoff.IsMemberSuspended)
                    {
                        WaterShutoff.SuspensionDate = DateTime.Now;
                    }
                    break;
                case "IsLate30":
                    if (WaterShutoff.IsLate30)
                    {
                        WaterShutoff.IsMemberSuspended = true;
                        WaterShutoff.FirstNotificationDate = DateTime.Now;
                        //WaterShutoff.IsLate60 = false;
                        //WaterShutoff.IsLate90 = false;
                    }
                    break;
                case "IsLate60":
                    if (WaterShutoff.IsLate60)
                    {
                        WaterShutoff.IsMemberSuspended = true;
                        WaterShutoff.SecondNotificationDate = DateTime.Now;
                    }
                    break;
                case "IsLate90":
                    if (WaterShutoff.IsLate90)
                    {
                        WaterShutoff.IsMemberSuspended = true;
                        WaterShutoff.ShutoffNoticeIssuedDate = DateTime.Now;
                    }
                    break;
                case "IsRequestedHearing":
                    if (WaterShutoff.IsLate90)
                    {
                        WaterShutoff.HearingDate = DateTime.Now;
                    }
                    break;
                case "IsInCollections":
                    if (WaterShutoff.IsLate90)
                    {
                        WaterShutoff.CollectionsDate = DateTime.Now;
                    }
                    break;
                case "IsLienFiled":
                    if (WaterShutoff.IsLate90)
                    {
                        WaterShutoff.LienFiledDate = DateTime.Now;
                    }
                    break;
                default:
                    break;
            }
            //RaisePropertyChanged("DataChanged");
        }

    }
    /*================================================================================================================================================*/

    /// <summary>
    /// Command sink bindings......
    /// </summary>
    public partial class WaterShutoffViewModel : CommonViewModel, ICommandSink
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

    /*================================================================================================================================================*/
    public partial class WaterShutoffViewModel : CommonViewModel
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
    public partial class WaterShutoffViewModel : IDisposable
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
        ~WaterShutoffViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion
}
