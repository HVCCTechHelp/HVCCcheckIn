namespace HVCC.Shell.Common.Commands
{
    using System;
    using System.Windows.Input;

    public static class CustomCommands
    {
        private static RoutedUICommand discardChanges = new RoutedUICommand("Discard edits and restore original values read from the database.", "discardChanges", typeof(CustomCommands));
        public static RoutedUICommand DiscardChanges
        {
            get
            {
                return discardChanges;
            }
        }

        private static RoutedUICommand requeryReload = new RoutedUICommand("Re-reads records from the database, while maintaining any unsaved edits.", "requeryReload", typeof(CustomCommands));
        public static RoutedUICommand RequeryReload
        {
            get
            {
                return requeryReload;
            }
        }

        private static RoutedUICommand saveAll = new RoutedUICommand("Saves user-edits to the database, for all open documents.", "saveAll", typeof(CustomCommands));
        public static RoutedUICommand SaveAll
        {
            get
            {
                return saveAll;
            }
        }

        private static RoutedUICommand requeryReloadAll = new RoutedUICommand("Re-reads records from the windows for all open documents, while maintaining any unsaved edits.", "requeryReloadAll", typeof(CustomCommands));
        public static RoutedUICommand RequeryReloadAll
        {
            get
            {
                return requeryReloadAll;
            }
        }

        private static RoutedUICommand about = new RoutedUICommand("Describe the application's 'About' details.", "about", typeof(CustomCommands));
        public static RoutedUICommand About
        {
            get
            {
                return about;
            }
        }

        private static RoutedUICommand restoreDefaultGridLayout = new RoutedUICommand("Restores the original layout of the grid.", "restoreDefaultGridLayout", typeof(CustomCommands));
        public static RoutedUICommand RestoreDefaultGridLayout
        {
            get
            {
                return restoreDefaultGridLayout;
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
