namespace HVCC.Shell.Resources
{
    using System;
    using System.Windows.Input;

    public class CommandHandler : ICommand
    {

        private bool _canExecute;
        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        private Action _action;
        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class CommandHandlerWparm : ICommand
    {

        private bool _canExecute;
        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        private Action<string> _actionWparm;
        public CommandHandlerWparm(Action<string> action, bool canExecute)
        {
            _actionWparm = action;
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            _actionWparm(parameter as string);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

}
