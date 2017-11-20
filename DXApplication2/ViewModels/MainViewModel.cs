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
            this.RegisterHelpHandler();
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

    #region Load Modules (NOT IMPLEMENTED)
    /******** NOT IMPLEMENTED
    internal partial class MainViewModel // Load Modules
    {
        /// <summary>
        /// This method loads the assemblies found in the private assemblies path and 
        /// registers all valid features defined within.  The private assemblies path 
        /// is defined in the application's configuration file's (CleanAir.exe.config) "privatePath" property of the "probing" 
        /// element.  Features are types attributed with the [Feature(...)] attribute, 
        /// and they are valid, if they implement the required interfaces
        /// </summary>
        /// <remarks>
        /// Its okay to call this method multiple times.  Each subsequent time will re-check the modules path for module DLLs, and 
        /// Application Settings, and load any that it finds that were not previously loaded.
        /// </remarks>
        internal void LoadModules()
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            string assemblyName = null;
            foreach (string fileName in PrivateAssemblies)
            {
                try
                {
                    assemblyName = Path.GetFileNameWithoutExtension(fileName);

                    // Each of our assemblies is (potentially) paired with an application setting indicating
                    // if the assembly is 'Enabled' or 'Disabled'. The paired setting names match the Assembly 
                    // names. For each assembly, if the paired setting is found but does NOT specify 'Enabled',  
                    // then it should be ignored (not loaded).
                    if (!UserPreferences.Instance.SettingExists(assemblyName)
                        || UserPreferences.Instance.SettingValue(assemblyName).Equals("Enabled", StringComparison.OrdinalIgnoreCase))
                    {
                        // check if the assembly is already loaded.
                        var foundAssembly = (from x in loadedAssemblies where x.GetName().Name == assemblyName select x).SingleOrDefault();

                        if (null == foundAssembly)
                        {
                            Assembly assembly = Assembly.Load(assemblyName);
                            PlugInManager.Instance.LoadFeatures(assembly);
                        }
                    }
                }
                catch (Exception ex)
                {
                    StringBuilder sb = new StringBuilder(string.Format("The '{0}' module did not load.  Features of this module will not be available.  Features from other modules will continue to work normally.", assemblyName, ex.Message));

                    ReflectionTypeLoadException rtlex = ex as ReflectionTypeLoadException;
                    if (null != rtlex)
                    {
                        int lexCount = 0;
                        foreach (Exception lex in rtlex.LoaderExceptions)
                        {
                            lexCount++;

                            if (1 == lexCount)
                            {
                                sb.Append("\n\nTechnical Details:");
                            }

                            sb.AppendLine(string.Format("\n\n{0}. {1}", lexCount, lex.Message));
                        }
                    }

                    System.Windows.MessageBox.Show(
                        sb.ToString(),
                        string.Format("Module Not Loaded"),
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information,
                        System.Windows.MessageBoxResult.OK,
                        System.Windows.MessageBoxOptions.DefaultDesktopOnly);
                }
            }

            // For each command of each module just loaded create a: Ribbon button; and CommandCommandBinding
            foreach (Feature moduleInfo in PlugInManager.Instance.Modules)
            {
                IModule module = PlugInManager.Construct<IModule>(moduleInfo);
                if (null != module && !this.loadedModules.Contains(module))
                {
                    this.loadedModules.Add(module);

                    if (null != module.Commands)
                    {
                        foreach (CommandInfo info in module.Commands)
                        {
                            this.View.AddRibbonButton(info, module);
                        }
                    }
                }
            }
        }

        private List<IModule> loadedModules = new List<IModule>();

        private static List<string> PrivateAssemblies
        {
            get
            {
                List<string> list = new List<string>();

                try
                {
                    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "CleanAir.*.dll", SearchOption.TopDirectoryOnly);
                    list.AddRange(files.Where(x => !x.Contains("Common") && !x.Contains("UniformAddressing")));
                }
                catch (Exception ex)
                {
                    Debug.Fail(string.Format("Check your CleanAir.exe.config's probing privatPath setting. \n{0}", ex.Message));
                }

                return list;
            }
        }

        private static List<string> GetProbingPrivatePathList()
        {
            List<string> paths = new List<string>();

            XmlTextReader reader = new XmlTextReader("CleanAir.exe.config");
            while (reader.Read())
            {
                if (XmlNodeType.Element == reader.NodeType
                    && "probing" == reader.Name)
                {
                    string privatePath = reader.GetAttribute("privatePath");
                    if (!string.IsNullOrWhiteSpace(privatePath))
                    {
                        string[] entries = privatePath.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string entry in entries)
                        {
                            paths.Add(entry);
                        }
                    }
                }
            }

            return paths;
        }
    }
    **********************************************/
    #endregion

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

            IEnumerable<IForm> allOpenDocuments = from x in this.OpenMvvmBinders
                                                  where x.ViewModel is IForm
                                                  select x.ViewModel as IForm;
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

            IEnumerable<IForm> dirtyDocuments = from x in this.OpenMvvmBinders
                                                where x.ViewModel is IForm && ((IForm)x.ViewModel).IsDirty
                                                select x.ViewModel as IForm;
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