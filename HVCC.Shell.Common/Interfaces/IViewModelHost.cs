namespace HVCC.Shell.Common.Interfaces
{
    using System.Collections.ObjectModel;

    public enum HostMessageType { Information, Warning, Error, None }

    public interface IViewModelHost
    {
        void ShowMessage(string message, string caption, HostMessageType messageType = HostMessageType.None);
        bool PromptYesNo(string messagePrompt, string caption);

        /// <summary>
        /// Returns 3 state boolean (bool?)
        /// </summary>
        /// <returns>Yes==true, No==false, Cancel==null</returns>
        bool? PromptYesNoCancel(string messagePrompt, string caption);
        void Close(IMvvmBinder mvvmBinder);
        void RefocusOrOpenViewModel(IMvvmBinder mvvmBinder);

        /// <summary>
        /// Log the error in in database table: [ApplicationSettings].[Diagnostics].[ApplicationEventLog]
        /// </summary>
        /// <param name="type">"Error", "Warning", or "Information"</param>
        /// <param name="tags">Zero or more tag values that will help you filter/query the event log. Any format is allowed.</param>
        /// <param name="message">The details of this Error, Warning, or Information log record.</param>
        /// <returns>True if the message was successfully written to the database.</returns>
        bool LogApplicationEvent(string type, string tags, string message);

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
    }
}