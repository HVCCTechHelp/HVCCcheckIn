namespace HVCC.Property.ViewModels
{
    using System;
    using System.Linq;
    using HVCC.Property.Models;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Media.Imaging;
    using System.Windows;
    using System.IO;
    using DevExpress.Xpf.Docking;
    using System.Data.Linq;
    using Shell.Common;
    using DevExpress.Mvvm;
    using DevExpress.Mvvm.POCO;
    using HVCC.Shell.Common;
    public partial class PropertiesViewModel : INotifyPropertyChanged
    {
        public static String baseURI = @"C:\HVCC\Photos\";
        bool garbage = false;

        public virtual ISaveFileDialogService SaveFileDialogService { get { return null; } }
        public virtual IExportService ExportService { get { return null; } }

        public enum ExportType
        {
            PDF,
            XLSX
        }

        public enum PrintType
        {
            PREVIEW,
            PRINT
        }

        public PropertiesViewModel ()
        {
            relationshipTypes.Add("Owner");
            relationshipTypes.Add("Child");
            relationshipTypes.Add("In-Law");
            relationshipTypes.Add("Grandchild");
            relationshipTypes.Add("Grandparent");
        }

        /// <summary>
        /// 
        /// </summary>
        private BaseLayoutItem activeDocPanel = null;
        public BaseLayoutItem ActiveDocPanel
        {
            get
            {
                return this.activeDocPanel;
            }
            set
            {
                if (this.activeDocPanel != value)
                {
                    this.activeDocPanel = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private ObservableCollection<Property> propertiesList = null;
        public ObservableCollection<Property> PropertiesList
        {
            get
            {
                if (this.propertiesList == null)
                {
                    this.propertiesList = GetProperties();
                }
                garbage = IsDirty;
                return this.propertiesList;
            }
            set
            {
                if (this.propertiesList != value)
                {
                    this.propertiesList = value;
                    RaisePropertyChanged("PropertiesList");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<Property> GetProperties()
        {
            try
            {
                //// Get the list of "Properties" from the database
                    var list = (from a in this.dc.Properties
                                select a);

                    return new ObservableCollection<Property>(list);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private Property selectedProperty = new Property();
        public Property SelectedProperty
        {
            get
            {
                return this.selectedProperty;
            }
            set
            {
                //// wrap the setter with a check for a null value.  This condition happens when
                //// a Relationship is selected from the Relationship grid. Therefore, when
                //// a Relationship is selected we won't null out the SelectedProperty.
                if (null != value)
                {
                    if (value != this.selectedProperty)
                    {
                        this.selectedProperty = value;
                    }
                    RaisePropertyChanged("SelectedProperty");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private Relationship selectedRelation = new Relationship();
        public Relationship SelectedRelation
        {
            get
            {
                return this.selectedRelation;
            }
            set
            {
                if (value != this.selectedRelation)
                {
                    this.selectedRelation = value;
                    //this.SelectedRelation.Image = GetImage(this.SelectedRelation.PhotoURI);
                    RaisePropertyChanged("SelectedRelation");
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private ObservableCollection<v_PropertyHistory> propertiesHistory = null;
        public ObservableCollection<v_PropertyHistory> PropertiesHistory
        {
            get
            {
                this.propertiesHistory = GetPropertiesHistory();
                return this.propertiesHistory;
            }
            set
            {
                if (this.propertiesHistory != value)
                {
                    this.propertiesHistory = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<v_PropertyHistory> GetPropertiesHistory()
        {
            try
            {
                var list = (from a in this.dc.v_PropertyHistories
                            orderby a.Year
                            select a);

                return new ObservableCollection<v_PropertyHistory>(list);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private v_PropertyHistory selectedPropertyHistory = new v_PropertyHistory();
        public v_PropertyHistory SelectedPropertyHistory
        {
            get
            {
                return this.selectedPropertyHistory;
            }
            set
            {
                if (value != this.selectedPropertyHistory)
                {
                    this.selectedPropertyHistory = value;
                }
                RaisePropertyChanged("SelectedPropertyHistory");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private ObservableCollection<String> relationshipTypes = new ObservableCollection<string>();
        public ObservableCollection<String> RelationshipTypes
        {
            get { return this.relationshipTypes;  }
            set
            {
                if (value != this.relationshipTypes)
                {
                    this.relationshipTypes = value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public String SaveNewImage(BitmapImage image)
        {
            try
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                //// If this is a new relationship that is being added, the SelectedRelationship first and last
                //// names will be null.
                //// Calculate the relationshipID to be 1 more than the current SelectedProperty's 
                //// relationship collection count.  Otherwise, we assume they are updating/replacing
                //// the current image and therefore we use the existing relationshipID
                int relationID = 0;
                if (String.IsNullOrEmpty(this.SelectedRelation.FName) && String.IsNullOrEmpty(this.SelectedRelation.LName))
                {
                    relationID = this.SelectedProperty.Relationships.Count() + 1;
                }
                else
                {
                    relationID = this.SelectedRelation.RelationshipID;
                }

                String photoURI = baseURI + this.SelectedProperty.Section.ToString() + "-" +
                    this.SelectedProperty.Block.ToString() + "-" +
                    this.SelectedProperty.Lot.ToString() + "-" +
                    this.SelectedProperty.SubLot.ToString() + "-" +
                    relationID + ".jpg";

                encoder.Frames.Add(BitmapFrame.Create(image));

                using (var filestream = new FileStream(photoURI, FileMode.Create))
                    encoder.Save(filestream);
                return photoURI;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>,
        /// 
        /// </summary>
        #region INotifyPropertyChagned implementaiton
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// EventHandler: OnPropertyChanged raises a handler to notify a property has changed.
        /// </summary>
        /// <param name="propertyName">The name of the property being changed</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        //// <summary>
        //// Public Members and Methods.....
        //// </summary>
        #region PublicMembers Region........
        public int MinYear
        {
            get
            {
                return DateTime.Now.Year;
            }
        }
        public int MaxYear
        {
            get
            {
                return (DateTime.Now.Year + 1);
            }
        }

        public bool IsDirty {
            get
            {
                var changeSet =  this.dc.GetChangeSet();
                if (0 == changeSet.Inserts.Count + changeSet.Updates.Count + changeSet.Deletes.Count)
                {
                    return false;
                }
                return true;
            }
        }

        public void SaveChanges()
        {
            try
            {
            this.dc.Log = System.Console.Out;
                ChangeSet cs = dc.GetChangeSet();
            this.dc.SubmitChanges();
            }
            catch (Exception ex)
            {
                //// To-Do: This needs to be moved to the View.....
                MessageBox.Show("Error Saving data:" + ex.Message);
            }
        }

        public void DiscardChanges()
        {
            try
            {
                this.dc.Log = System.Console.Out;
                this.dc.Dispose();
            }
            catch (Exception ex)
            {
                //// To-Do: This needs to be moved to the View.....
                MessageBox.Show("Error Saving data:" + ex.Message);
            }
        }

        public void Export(ExportType type)
        {
            switch (type)
            {
                case ExportType.PDF:
                    SaveFileDialogService.Filter = "PDF files|*.pdf";
                    if (SaveFileDialogService.ShowDialog())
                        ExportService.ExportToPDF(SaveFileDialogService.GetFullFileName());
                    break;
                case ExportType.XLSX:
                    SaveFileDialogService.Filter = "Excel 207 files|*.xlsx";
                    if (SaveFileDialogService.ShowDialog())
                        ExportService.ExportToPDF(SaveFileDialogService.GetFullFileName());
                    break;
            }
        }

        public void Print(PrintType type)
        {
            switch (type)
            {
                case PrintType.PREVIEW:
                    ExportService.ShowPrintPreview();
                    break;
                case PrintType.PRINT:
                    ExportService.Print();
                    break;
            }
        }

        #endregion

    }

    /// <summary>
    /// Disposition.......
    /// </summary>
    #region public partial class PropertiesViewModel : IDisposable
    public partial class PropertiesViewModel : IDisposable
    {
        // Resources that must be disposed:
        private HVCCDataContext dc = new HVCCDataContext();

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
        ~PropertiesViewModel()
        {
            // call Dispose with false.  Since we're in the
            // destructor call, the managed resources will be
            // disposed of anyways.
            this.Dispose(false);
        }
    }
    #endregion

}
