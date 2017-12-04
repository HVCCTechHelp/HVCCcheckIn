namespace HVCC.Shell.Common.Interfaces
{
    using System.Collections.ObjectModel;

    public enum HostMessageType { Information, Warning, Error, None }

    public enum HostVerb { Open, Close, Update }

    public interface IHost
    {
        //void ShowMessage(string message, string caption, HostMessageType messageType = HostMessageType.None);
        //bool PromptYesNo(string messagePrompt, string caption);

        /// <summary>
        /// Returns 3 state boolean (bool?)
        /// </summary>
        /// <returns>Yes==true, No==false, Cancel==null</returns>
        //bool? PromptYesNoCancel(string messagePrompt, string caption);

        void Execute(HostVerb verb, object param, object arg = null);

        void Close(IMvvmBinder mvvmBinder);

        //void RefocusOrOpenViewModel(IMvvmBinder mvvmBinder);

        /// <summary>
        /// Gets the list of all open View/ViewModel pairs (as <code>IMvvmBinders</code>).
        /// </summary>
        /// <example><code>
        /// // Get a list of all open forms that support refresh:
        /// IEnumerable<IForm> refreshableForms = from mvvm in this.OpenMvvmBinders
        ///                                       where mvvm.ViewModel is IForm && ((IForm)mvvm.ViewModel).AllowRefresh
        ///                                       select mvvm.ViewModel as IForm;
        /// </code></example>
        ObservableCollection<IMvvmBinder> OpenMvvmBinders { get; }

        /// <summary>
        /// Application permissions list
        /// </summary>
        /// <example><code>
        /// VM implementation:  ApplicationPermission permissions = Host.AppPermissions as ApplicationPermission;
        ///</code></example>
        object AppPermissions { get; }
        object AppDefault { get; }

        object Parameter { get; set; }
    }
}