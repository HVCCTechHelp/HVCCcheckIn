namespace HVCC.Shell.ViewModels
{
    using System;
    using System.Windows.Input;

    public delegate void CommandExecutingEventHandler(object sender, CommandExecutingEventArgs e);

    public class CommandExecutingEventArgs : EventArgs
    {
        public CommandExecutingEventArgs(ICommand command)
        {
            this.Command = command;
        }

        public ICommand Command { get; private set; }
    }
}
