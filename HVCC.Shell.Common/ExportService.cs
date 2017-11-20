namespace HVCC.Shell.Common
{
    using DevExpress.Mvvm.UI;
    using DevExpress.Xpf.Grid;
    using System.Windows;
    using System.Linq;
    using System.Collections.ObjectModel;

    public interface IExportService
    {
        void ExportToPDF(TableView view, string fileName);
        void ExportToXLSX(TableView view, string fileName);
        //void ShowPrintPreview(TableView view);
        void ShowPrintPreview(TableView view);
        void Print(TableView view);
    }

    public abstract class ExportServiceBase<T> : ServiceBase, IExportService where T : DataViewBase
    {

        public void ExportToPDF(TableView view, string fileName)
        {
            if (view != null && !string.IsNullOrWhiteSpace(fileName))
                ExportToPDFInternal(view, fileName);
        }
        public void ExportToXLSX(TableView view, string fileName)
        { 
            if (view != null && !string.IsNullOrWhiteSpace(fileName))
                ExportToXLSXInternal(view, fileName);
        }

        public void ShowPrintPreview(TableView view)
        {
            if (view != null)
                ShowPrintPreviewInternal(view);
        }

        public void Print(TableView view)
        {
            if (view != null)
                PrintInternal(view);
        }

        protected abstract void ExportToPDFInternal(TableView view, string fileName);
        protected abstract void ExportToXLSXInternal(TableView view, string fileName);
        protected abstract void ShowPrintPreviewInternal(TableView view);

        protected abstract void PrintInternal(TableView view);
    }

    public class TableViewExportService : ExportServiceBase<TableView>
    {
        protected override void ExportToPDFInternal(TableView view, string fileName)
        {
            view.ExportToPdf(fileName);
        }

        protected override void ExportToXLSXInternal(TableView view, string fileName)
        {
            view.ExportToXlsx(fileName);
        }

        protected override void ShowPrintPreviewInternal(TableView view)
        {
            var window = (Window)LayoutTreeHelper.GetVisualParents(view).Where(x => x is Window).FirstOrDefault();
            if (window != null)
                view.ShowPrintPreview(window);
        }

        protected override void PrintInternal(TableView view)
        {
            view.Print();
        }
    }
}