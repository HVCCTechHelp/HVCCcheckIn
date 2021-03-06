﻿namespace HVCC.Shell.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Models;
    using DevExpress.Xpf.Grid;
    using HVCC.Shell.Common.Interfaces;
    using System.Collections.Generic;
    using System;
    using DevExpress.Xpf.Printing;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for PropertyDetailView.xaml
    /// </summary>
    public partial class PropertyDetailsView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public PropertyDetailsView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;

            this.ViewModel.Table = this.propertiesView;
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
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void view_ValidateRow(object sender, GridRowValidationEventArgs e)
        {
            if (!e.IsValid)
            {
                e.IsValid = false;
                e.ErrorType = DevExpress.XtraEditors.DXErrorProvider.ErrorType.Critical;
                e.ErrorContent = "The highlighted field cannot be blank";
                e.SetError(e.ErrorContent);
            }
        }

        private void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
        }
    }
}
