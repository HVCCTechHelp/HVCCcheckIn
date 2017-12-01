namespace HVCC.Shell.Common.Interfaces
{
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Common;

    public interface IMvvmBinder
    {
        IDataContext DataContext { get; }

        IView View { get; }

        IViewModel ViewModel { get; }
    }
}
