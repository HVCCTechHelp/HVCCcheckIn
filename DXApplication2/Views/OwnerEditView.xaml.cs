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


    /// <summary>
    /// Interaction logic for PropertyEditDialogView.xaml
    /// </summary>
    public partial class OwnerEditView : UserControl, IView
    {
        ApplicationPermission appPermissions;
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public OwnerEditView(IViewModel vm)
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
        private void tableViewDetail_CellValueChanging(object sender, CellValueChangedEventArgs e)
        {
            this.tableViewDetail.PostEditor();
        }

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

    }
}

