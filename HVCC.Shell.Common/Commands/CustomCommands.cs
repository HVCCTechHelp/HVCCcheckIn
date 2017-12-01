namespace HVCC.Shell.Common.Commands
{
    using System;
    using System.Windows.Input;

    public static class CustomCommands
    {
        private static RoutedUICommand saveAll = new RoutedUICommand("Saves changes across all ViewModels", "saveAll", typeof(CustomCommands));
        public static RoutedUICommand SaveAll
        {
            get
            {
                return saveAll;
            }
        }

        private static RoutedUICommand export = new RoutedUICommand("Exports grid table data", "export", typeof(CustomCommands));
        public static RoutedUICommand Export
        {
            get
            {
                return export;
            }
        }

        ////private static RoutedUICommand xxx = new RoutedUICommand("", "xxx", typeof(CustomCommands));
        ////public static RoutedUICommand Xxx
        ////{
        ////    get
        ////    {
        ////        return xxx;
        ////    }
        ////}
    }
}
