namespace HVCC.Shell.Common
{
    using DevExpress.Mvvm.UI;
    using DevExpress.Xpf.Grid;

    public interface IGridControlService
    {

        void HideEditForm();
        void CloseEditForm();

    }

    public class GridControlService : ServiceBase, IGridControlService
    {
        /// <summary>
        /// Collapses a grids inline edit form
        /// </summary>
        public void CloseEditForm()
        {
            // Since I don't know if the master or detail grid invoked the Close(), I
            // simpley call the Close() on both....

            // For Master Grid edit form
            (this.AssociatedObject as TableView).CloseEditForm();
            // For Detail Grid edit form
            ((TableView)(this.AssociatedObject as TableView).FocusedView).CloseEditForm();
        }

        /// <summary>
        /// Collapses a grids inline edit form
        /// </summary>
        public void HideEditForm()
        {
            (this.AssociatedObject as TableView).HideEditForm();
            ((TableView)(this.AssociatedObject as TableView).FocusedView).HideEditForm();
        }

    }
}

