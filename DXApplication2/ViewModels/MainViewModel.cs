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
    using HVCC.Shell.Common.ViewModels;
    using System.Windows.Input;
    using HVCC.Shell.Resources;

    /// <summary>
    /// Shell.Instance is the "ViewModel" to the MainWindow "View".
    /// </summary>
    public partial class MainViewModel : CommonViewModel // MVVM
    {
        public void Initialize(MainWindow view)
        {
            this.View = view;
        }

        internal MainWindow View { get; private set; }

        public override bool IsValid
        {
            get { return true; }
        }

        public override bool IsDirty
        {
            get
            {
                return false;
            }
        }

        private bool _isBusy = false;
        public override bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if (value != _isBusy)
                {
                    _isBusy = value;
                    RaisePropertyChanged("IsBusy");
                }
            }
        }

        public override bool Save()
        {
            return true;
        }

    }

    public partial class MainViewModel : CommonViewModel, ICommandSink //, CommandSink // Singleton Implementation & Command registration
    {
        public MainViewModel()
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
            this.RegisterSaveHandler();
            //this.RegisterExportHandler();
            //this.RegisterPrintHandler();
        }

        private void RegisterSaveHandler()
        {
            _sink.RegisterCommand(
                ApplicationCommands.Save,
                param => this.CanSaveExecute,
                param => this.SaveExecute());
        }

        private bool CanSaveExecute
        {
            get
            {
                return true;
            }
        }

        private void SaveExecute()
        {
            this.RaiseCommandExecuting(new CommandExecutingEventArgs(ApplicationCommands.Save));

            System.Windows.MessageBox.Show("Execute Save command", "Save", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            this.RaiseCommandExecuted(new CommandExecutedEventArgs(ApplicationCommands.Save, MessageType.None, string.Empty));
        }


#region ICommandSink Implementation
        private CommandSink _sink = new CommandSink();

        // Required by the ICommandSink Interface
        public bool CanExecuteCommand(ICommand command, object parameter, out bool handled)
        {
            return _sink.CanExecuteCommand(command, parameter, out handled);
        }

        // Required by the ICommandSink Interface
        public void ExecuteCommand(ICommand command, object parameter, out bool handled)
        {
            _sink.ExecuteCommand(command, parameter, out handled);
        }
#endregion

        public static MainViewModel Instance
        {
            get { return Nested.Instance; }
        }

        private class Nested
        {
            internal static readonly MainViewModel Instance = new MainViewModel();
        }

    }

    public partial class MainViewModel: CommonViewModel // Keep Track of Open IMvvmBinders
    {
        private ObservableCollection<IMvvmBinder> openMvvmBinders = new ObservableCollection<IMvvmBinder>();

        public ObservableCollection<IMvvmBinder> OpenMvvmBinders
        {
            get { return this.openMvvmBinders; }
        }

        internal void Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindow mw = this.View as MainWindow;

            IEnumerable<IViewModel> allOpenDocuments = from x in this.OpenMvvmBinders
                                                  where x.ViewModel is IViewModel
                                                       select x.ViewModel as IViewModel;
            if (null != allOpenDocuments && allOpenDocuments.Count() > 0)
            {
                bool cancelClose = false;
                foreach (IViewModel form in allOpenDocuments)
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
                foreach (IViewModel form in dirtyDocuments)
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
                                IEnumerable<IViewModel> stillDirtyDocuments = from x in this.OpenMvvmBinders
                                                                         where x.ViewModel is IViewModel && ((IViewModel)x.ViewModel).IsDirty
                                                                         select x.ViewModel as IViewModel;
                                e.Cancel = stillDirtyDocuments.Count() > 0;
                            }
                        }

                        break;
                }
            }
        }
    }

    public partial class MainViewModel
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

    public partial class MainViewModel
    {
        public bool IsConnected
        {
            get
            {
                //try
                //{
                //    dc.CommandTimeout = 5;
                //    dc.Connection.Open();
                //    dc.Connection.Close();
                //    return true;
                //}
                //catch (Exception ex)
                //{
                //    return false;
                //}
                return true;
            }
        }
     
    }
}