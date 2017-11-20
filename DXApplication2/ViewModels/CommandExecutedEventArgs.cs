namespace HVCC.Shell.ViewModels
{
    using System;
    using System;
    using System.Windows.Input;

    public delegate void CommandExecutedEventHandler(object sender, CommandExecutedEventArgs e);

    public enum MessageType
    {
        None,
        Message,
        Warning,
        Error
    }

    public class CommandExecutedEventArgs : EventArgs
    {
        public CommandExecutedEventArgs(ICommand command, MessageType type, string message)
        {
            this.Command = command;
            this.MessageType = type;
            this.Message = message;
        }

        public ICommand Command { get; private set; }

        public MessageType MessageType { get; private set; }

        public string Message { get; private set; }
    }
}