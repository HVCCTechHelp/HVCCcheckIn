namespace HVCC.Shell.Views
{
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Editors;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.ViewModels;
    using Models;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Linq;
    using System.IO;
    using HVCC.Shell.Common.Interfaces;
    using System.Windows.Media.Imaging;
    using System.Windows.Input;


    /// <summary>
    /// Interaction logic for PropertyEditDialogView.xaml
    /// </summary>
    public partial class PropertyEditView : UserControl, IView
    {
        ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public PropertyEditView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            //this.Loaded += OnLoaded;
            appPermissions = Host.Instance.AppPermissions as ApplicationPermission;
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
        /// Property Changed event handler for the view model
        /// </summary>
        /// <param name="sender">object invoking the method</param>
        /// <param name="e">property change event arguments</param>
        //protected void PropertyViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    switch (e.PropertyName)
        //    {
        //        case "<property>":
        //            break;
        //        default:
        //            break;
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void Te_Validate(object sender, ValidationEventArgs e)
        //{
        //    string input = e.Value as string;
        //    if (string.IsNullOrEmpty(input))
        //    {
        //        e.IsValid = false;
        //    }
        //    else
        //    {
        //        e.IsValid = true;
        //    }
        //    e.Handled = true;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editform_Validate(object sender, ValidationEventArgs e)
        {
            //// The ImageEdit control doesn't validate the image by default, therefore
            //// we manually validate the control here so validation is passed.
            if (sender is ImageEdit)
            {
                if (((ImageEdit)sender).HasImage)
                {
                    e.IsValid = true;
                }
                else
                {
                    e.IsValid = false;
                }
            }
            // Validate the RelationToOwner ComboBox
            if (sender is ComboBoxEdit)
            {
                if (string.IsNullOrEmpty((string)e.Value))
                {
                    e.IsValid = false;
                }
                else
                {
                    e.IsValid = true;
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void teParcel_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //// It is plusuable to assume that the parcel number may get entered incorrectly, or
            //// need to be replaced at some point.  Therefore, I've put in a hot-key <LeftCtrl> that
            //// will enable editing of this control. Otherwise the control remains read-only.
            //if (e.Key == System.Windows.Input.Key.LeftCtrl && Host.Instance.AppPermissions.CanEditPropertyInfo)
            //{
            //    object resource = TryFindResource("TextEditEditStyle");
            //    this.teParcel.Style = (Style)resource;
            //    this.teParcel.IsReadOnly = false;
            //}
        }

        private void EditProperty_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }
}
