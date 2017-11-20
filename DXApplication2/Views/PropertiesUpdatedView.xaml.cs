namespace HVCC.Shell.Views
{
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

    /// <summary>
    /// Interaction logic for PropertiesUpdatedView.xaml
    /// </summary>
    public partial class PropertiesUpdatedView : UserControl
    {
        public PropertiesUpdatedView()
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
            PropertiesViewModel vm = ((PropertiesViewModel)this.DataContext);
            vm.GridTableView = this.propertiesTableView;

            //// Bind the properties of the view  to the properties of the view model.  
            //// The properties are INotify, so when one changes it registers a PropertyChange
            //// event on the other.  Also note, this code must reside outside of the
            //// constructor or a XAML error will be thrown.
            //if (null != vm)
            //{
            //    vm.PropertyChanged +=
            //        new System.ComponentModel.PropertyChangedEventHandler(this.PropertiesViewModel_PropertyChanged);
            //}
        }

        /// <summary>
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        //protected void PropertiesViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    PropertiesViewModel vm = this.DataContext as PropertiesViewModel;

        //    switch (e.PropertyName)
        //    {
        //        default:
        //            break;
        //    }
        //}
    }
}
