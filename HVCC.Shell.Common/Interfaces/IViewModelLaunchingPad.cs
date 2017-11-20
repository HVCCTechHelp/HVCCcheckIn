namespace HVCC.Shell.Common.Interfaces
{
    public interface IViewModelLaunchingPad
    {
        void Start(IMvvmBinder mvvmBinder);

        void OpenHelpWindow(int? helpId);
    }
}