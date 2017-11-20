namespace HVCC.Property.Views
{
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
    using HVCC.Property.Models;
    using HVCC.Property.ViewModels;
    using DevExpress.Xpf.Docking;
    using DevExpress.Xpf.Bars;
    using DevExpress.Xpf.Grid;    
    
    /// <summary>
    /// Interaction logic for PropertyDetailView.xaml
    /// </summary>
    public partial class PropertyDetailsView : UserControl
    {
        public PropertyDetailsView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PropertiesViewModel vm = this.DataContext as PropertiesViewModel;

            switch (e.PropertyName)
            {
                // TO-DO:  The property changed isn't being called when the details are changed
                case "IsDirty":
                    if (vm.IsDirty)
                    {
                        this.grpboxProperties.Header += "*";
                    }
                    break;
                case "PropertiesList":
                    this.propertyGrid.RefreshData();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This method is invoked when the 'edit' option is selected from the context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        ////private void editRowItem_ItemClick(object sender, ItemClickEventArgs e)
        ////{
        ////    ////MessageBox.Show("editRowItem_ItemClick");
        ////    try
        ////    {
        ////        IsEditMode(true);
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        MessageBox.Show("Error from editRowItem_ItemClick: " + ex.Message);
        ////    }
        ////}



        /// <summary>
        /// Invoked from the class constructor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);

            //// Bind the properties of the view  to the properties of the view model.  
            //// The properties are INotify, so when one changes it registers a PropertyChange
            //// event on the other.  Also note, this code must reside outside of the
            //// constructor or a XAML error will be thrown.
            if (null != vm)
            {
                vm.PropertyChanged +=
                    new System.ComponentModel.PropertyChangedEventHandler(this.PropertiesViewModel_PropertyChanged);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        object expandedRow;
        private void grid_MasterRowExpanded(object sender, RowEventArgs e)
        {
            if (expandedRow != null)
            {
                var expandedRowHandle = propertyGrid.FindRow(expandedRow);
                propertyGrid.CollapseMasterRow(expandedRowHandle);
            }
            expandedRow = e.Row;
        }
    }
}
