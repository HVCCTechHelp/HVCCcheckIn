using HVCC.Shell.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HVCC.Shell.Models;
using DevExpress.Xpf.Grid;

namespace HVCC.Shell.Views
{
    /// <summary>
    /// Interaction logic for GolfCartView.xaml
    /// </summary>
    public partial class GolfCartView : UserControl
    {
        //PropertiesViewModel vm = null; 
        GolfCartViewModel vm = null;
        PropertiesViewModel pvm = null;

        public GolfCartView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // When this view is loaded we associate the ViewModel. On the first load, the data context
            // is pre-set to the parent VM. We capture that VM, and reset the view's VM to this view's VM.
            // This is just done on the first load. Otherwise, we associated the VM to the existing VM object.
            // Also note, OnLoaded() is called when the document panel becomes the active doc panel. 
            if (this.DataContext is GolfCartViewModel)
            {
                // ViewModel (vm) and ParentViewModel (pvm) is already assigned
                // When this view becomes active, we assign the primary gridTable view for exporing.
                pvm.GridTableView = this.tableViewCarts;
            }
            else
            {
                pvm = this.DataContext as PropertiesViewModel;
                this.DataContext = new GolfCartViewModel();

                pvm.ViewModels.Add(this.DataContext);
                vm = this.DataContext as GolfCartViewModel;
                vm.ParentViewModel = pvm;
                pvm.GridTableView = this.tableViewCarts;
            }
        }

        /// <summary>
        /// Validates a grid row 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_ValidateRow(object sender, DevExpress.Xpf.Grid.GridRowValidationEventArgs e)
        {
            GolfCart row = e.Row as GolfCart;

            try
            {
                if (null != row)
                {
                   // TO-DO: add validation rules
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Row Error: " + ex.Message);
            }
            finally
            {
                if (e.IsValid)
                {
                    // TO-DO:  ?
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Cancels addition of new row
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_RowCanceled(object sender, RowEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Grid row updated event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tableViewDetail_RowUpdated(object sender, RowEventArgs e)
        {
            bool dirty = pvm.IsDirty;
            e.Handled = true;
        }

        private void tableViewCarts_CellValueChanging(object sender, CellValueChangedEventArgs e)
        {
            // Set the focus to the name search text box to force a CellValueChanged event to fire. 
            // This in turn will cause IsDirty to be evaluated.
            this.tbSearchName.Focus();
            e.Handled = true;
        }
        private void tableViewCarts_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            bool dirty = pvm.IsDirty;
            e.Handled = true;
        }

        private void bb_ReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            throw new System.ArgumentException("Button not implemented", "Information");
        }
    }
}
