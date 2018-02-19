namespace HVCC.Shell.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Helpers;
    using HVCC.Shell.Models;
    using HVCC.Shell.ViewModels;
    using DevExpress.Mvvm;
    using DevExpress.Xpf.Grid;
    using DevExpress.Xpf.Docking;
    using HVCC.Shell.Common.Interfaces;
    using System.Collections.Generic;
    using System;
    using DevExpress.Xpf.Printing;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for Administration.xaml
    /// </summary>
    public partial class InvoiceView : UserControl, IView
    {
        public IViewModel ViewModel
        {
            get { return this.DataContext as IViewModel; }
            set { this.DataContext = value; }
        }

        public InvoiceView(IViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
            this.Loaded += OnLoaded;

            this.ViewModel.Table = this.tableViewInvoices;
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
    }
}

