namespace HVCC.Shell.Common.Interfaces
{
    using System.Windows;
    using System.Windows.Controls;

    public interface IMvvmBinder
    {
        IDataContext DataContext { get; }

        IView View { get; }

        IViewModel ViewModel { get; }
    }
}
