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
using HVCC.Shell.Common.Interfaces;
using System.ComponentModel;

namespace HVCC.Shell.Views
{
    /// <summary>
    /// Interaction logic for GolfCartView.xaml
    /// </summary>
    public partial class GolfCartView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public GolfCartView(IViewModel vm)
        {
            InitializeComponent();

            this.DataContext = vm;
            this.Loaded += OnLoaded;
            INotifyPropertyChanged pc = vm as INotifyPropertyChanged;
            if (null != pc)
            {
                pc.PropertyChanged += Pc_PropertyChanged;
            }
        }

        private void Pc_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsDirty")
            {
                // Do a caption change.....?
            }
        }

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            //this.DataContext = vm;
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
            //bool dirty = pvm.IsDirty;
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
            //bool dirty = pvm.IsDirty;
            e.Handled = true;
        }

        private void bb_ReportClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            throw new System.ArgumentException("Button not implemented", "Information");
        }

        public object SaveState()
        {
            //throw new NotImplementedException();
            return null;
        }

        public void RestoreState(object state)
        {
            //throw new NotImplementedException();
        }
    }
}
