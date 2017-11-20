namespace HVCC.Shell.Resources
{
    using System;
    using System.Windows.Input;
    using DevExpress.Mvvm.UI.Interactivity;
    using DevExpress.Xpf.Core;

    public class CloseByEscapeBehavior : Behavior<DXDialogWindow>
    {
        DXDialogWindow Window { get { return AssociatedObject; } }

        protected override void OnAttached()
        {
            base.OnAttached();
            Window.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            Window.PreviewKeyDown -= OnPreviewKeyDown;
            base.OnDetaching();
        }

        void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F22)
                Window.Close();
        }
    }
}
