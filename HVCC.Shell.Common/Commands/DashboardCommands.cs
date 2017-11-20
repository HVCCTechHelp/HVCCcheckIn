namespace HVCC.Shell.Common.Commands
{
    using System.Windows.Input;
    public class DashboardCommands
    {
        private static RoutedUICommand increaseTileSize = new RoutedUICommand("Increase the size of the tile.", "increaseTileSize", typeof(DashboardCommands));
        public static RoutedUICommand IncreaseTileSize { get { return increaseTileSize; } }

        private static RoutedUICommand decreaseTileSize = new RoutedUICommand("Decrease the size of the tile.", "sendSelectedEmails", typeof(DashboardCommands));
        public static RoutedUICommand DecreaseTileSize { get { return decreaseTileSize; } }

        private static RoutedUICommand increaseAllTileSize = new RoutedUICommand("Increase the size of all tiles.", "sendIndividualEmail", typeof(DashboardCommands));
        public static RoutedUICommand IncreaseAllTileSize { get { return increaseAllTileSize; } }

        private static RoutedUICommand decreaseAllTileSize = new RoutedUICommand("Decrease the size of all tiles.", "attachFiles", typeof(DashboardCommands));
        public static RoutedUICommand DecreaseAllTileSize { get { return decreaseAllTileSize; } }
    }
}
