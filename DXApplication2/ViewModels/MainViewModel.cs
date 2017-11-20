namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Xml;
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Commands;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.Views;

    /// <summary>
    /// Shell.Instance is the "ViewModel" to the MainWindow "View".
    /// </summary>
    internal partial class MainViewModel // MVVM
    {
        //private DashboardViewModel dashboard = new DashboardViewModel();

        public void Initialize(MainWindow view)
        {
            this.View = view;
            //this.LoadModules(); // (NOT IMPLEMENTED)

            //DashboardView dashboardView = this.dashboard.ViewUI as DashboardView;
            //if (null != dashboardView)
            //{
            //    this.View.OpenDashboard(dashboardView);
            //}

            ////IViewModel form1 = PlugInManager.Construct<IViewModel>(FeatureKind.Form, "HelloWorldForm");  // dynamic call to: void .ctor()
            ////if (form1 != null)
            ////{
            ////    this.View.OpenDocumentPanel(form1, "Hello World");
            ////}

            ////IViewModel form2 = PlugInManager.Construct<IViewModel>(FeatureKind.Form, "HelloWorldForm");  // dynamic call to: void .ctor()
            ////if (form2 != null)
            ////{
            ////    form2["Message"] = "This text was set via IForm[\"Message\"]=<some string>";
            ////    this.View.OpenDocumentPanel(form2, "HW2");
            ////}

        }

        internal MainWindow View { get; private set; }
    }

    internal partial class MainViewModel : CommandSink // Singleton Implementation & Command registration
    {
        private MainViewModel()
        {
            this.RegisterCommands();
            this.OpenMvvmBinders.CollectionChanged += this.OpenMvvmBinders_CollectionChanged;
        }

        private void OpenMvvmBinders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.OpenMvvmBinders != null)
            {
                App.OpenDocuments = this.OpenMvvmBinders.Select(x => x.GetType().Name).ToList();
            }
        }

        private void RegisterCommands()
        {
            this.RegisterAboutHandler();
            //this.RegisterHelpHandler();
            //this.RegisterSaveAllHandler();
            //this.RegisterRefreshAllHandler();
        }

        public static MainViewModel Instance
        {
            get { return Nested.Instance; }
        }

        private class Nested
        {
            internal static readonly MainViewModel Instance = new MainViewModel();
        }

    }

    internal partial class MainViewModel // Keep Track of Open IMvvmBinders
    {
        private ObservableCollection<IMvvmBinder> openMvvmBinders = new ObservableCollection<IMvvmBinder>();

        public ObservableCollection<IMvvmBinder> OpenMvvmBinders
        {
            get { return this.openMvvmBinders; }
        }

        internal void Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow mw = this.View as MainWindow;
            //this.dashboard.SaveTilePositions();
            //this.dashboard.SaveDashboardWidth(mw.toolWindows.ItemWidth.Value);

            IEnumerable<IViewModel> allOpenDocuments = from x in this.OpenMvvmBinders
                                                  where x.ViewModel is IViewModel
                                                       select x.ViewModel as IViewModel;
            if (null != allOpenDocuments && allOpenDocuments.Count() > 0)
            {
                bool cancelClose = false;
                foreach (IForm form in allOpenDocuments)
                {
                    bool cancel = false;
                    form.Closing(out cancel);
                    cancelClose |= cancel;
                }

                if (cancelClose)
                {
                    e.Cancel = cancelClose;
                    return;
                }
            }

            IEnumerable<IViewModel> dirtyDocuments = from x in this.OpenMvvmBinders
                                                where x.ViewModel is IViewModel && ((IViewModel)x.ViewModel).IsDirty
                                                select x.ViewModel as IViewModel;
            if (null != dirtyDocuments && dirtyDocuments.Count() > 0)
            {
                string seperator = string.Empty;
                StringBuilder sb = new StringBuilder();
                foreach (IForm form in dirtyDocuments)
                {
                    sb.Append(seperator);
                    sb.Append(form.Caption);
                    seperator = ", ";
                }

                MessageBoxResult result = MessageBox.Show(string.Format("Do you want to save your changes to the following files?: {0}", sb.ToString()), "Save Changes?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;

                    case MessageBoxResult.Yes:
                        {
                            try
                            {
                                if (CustomCommands.SaveAll.CanExecute(null, this.View))
                                {
                                    CustomCommands.SaveAll.Execute(null, this.View);
                                }
                            }
                            finally
                            {
                                IEnumerable<IForm> stillDirtyDocuments = from x in this.OpenMvvmBinders
                                                                         where x.ViewModel is IForm && ((IForm)x.ViewModel).IsDirty
                                                                         select x.ViewModel as IForm;
                                e.Cancel = stillDirtyDocuments.Count() > 0;
                            }
                        }

                        break;
                }
            }
        }
    }

    internal partial class MainViewModel
    {
        public event CommandExecutingEventHandler CommandExecuting;

        private void RaiseCommandExecuting(CommandExecutingEventArgs e)
        {
            if (null != this.CommandExecuting)
            {
                this.CommandExecuting(this, e);
            }
        }

        public event CommandExecutedEventHandler CommandExecuted;

        private void RaiseCommandExecuted(CommandExecutedEventArgs e)
        {
            if (null != this.CommandExecuted)
            {
                this.CommandExecuted(this, e);
            }
        }
    }
}