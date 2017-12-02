namespace HVCC.Shell.Views
{
    using System;
using System.Windows;
using System.Windows.Controls;
using HVCC.Shell.Models;
using DevExpress.Xpf.Grid;
using HVCC.Shell.Common.Interfaces;

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
            //INotifyPropertyChanged pc = vm as INotifyPropertyChanged;
            //if (null != pc)
            //{
            //    pc.PropertyChanged += Pc_PropertyChanged;
            //}

             this.ViewModel.Table = this.tableViewCarts;
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

        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
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
            e.Handled = true;
        }

        //private void bb_XXXXXClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        //{
        //    throw new System.ArgumentException("Button not implemented", "Information");
        //}

    }
}
