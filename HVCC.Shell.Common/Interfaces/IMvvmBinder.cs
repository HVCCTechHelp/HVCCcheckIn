namespace HVCC.Shell.Common.Interfaces
{
    using System.Windows;
    using System.Windows.Controls;

    public interface IMvvmBinder
    {
        IView View { get; }

        IViewModel ViewModel { get; }

        UserControl ViewUI { get; }

        Window ViewWindow { get; }
    }
}
