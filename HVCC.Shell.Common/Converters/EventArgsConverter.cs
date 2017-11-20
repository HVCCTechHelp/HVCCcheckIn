namespace HVCC.Shell.Common.Converters
{
    using System.Linq;
    using DevExpress.Mvvm.UI;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using DevExpress.Xpf.LayoutControl;

    public class ButtonEventArgsConverter : EventArgsConverterBase<MouseEventArgs>
    {
        protected override object Convert(object sender, MouseEventArgs args)
        {
            var button = LayoutTreeHelper.GetVisualParents((DependencyObject)args.OriginalSource, (DependencyObject)sender).OfType<Button>().FirstOrDefault();
            object btnParent = button.Parent as LayoutGroup;
            var layout = LayoutTreeHelper.GetVisualParents((DependencyObject)args.OriginalSource, (DependencyObject)btnParent).OfType<LayoutGroup>().FirstOrDefault();
            return layout != null ? layout.DataContext : null;
            //return null;
        }
    }
    public class ListBoxEventArgsConverter : EventArgsConverterBase<MouseEventArgs>
    {
        protected override object Convert(object sender, MouseEventArgs args)
        {
            var element = LayoutTreeHelper.GetVisualParents((DependencyObject)args.OriginalSource, (DependencyObject)sender).OfType<ListBoxItem>().FirstOrDefault();
            return element != null ? element.DataContext : null;
        }
    }
}
