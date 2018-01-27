namespace HVCC.Shell.Common.Commands
{
    using DevExpress.Mvvm;
    using DevExpress.Xpf.Grid;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;

    public class CommandAction : ViewModelBase
    {
        public enum ExportType { PDF, XLSX }
        public enum PrintType { PREVIEW, PRINT }

        /// <summary>
        /// Exports data grid to Excel
        /// </summary>
        /// <param name="type"></param>
        public void ExportAction(object parameter, object view, object dialogService = null, object exService=null) //ExportCommand
        {
        string fn;
            try
            {
                if (dialogService is ISaveFileDialogService)
                {
                    ISaveFileDialogService saveFileDialogService = dialogService as ISaveFileDialogService;
                    IExportService exportService = exService as IExportService;
                    TableView tv = view as TableView;
                    Enum.TryParse(parameter.ToString(), out ExportType type);

                    switch (type)
                    {
                        case ExportType.PDF:
                            saveFileDialogService.Filter = "PDF files|*.pdf";
                            if (saveFileDialogService.ShowDialog())

                                fn = saveFileDialogService.GetFullFileName();

                            exportService.ExportToPDF(tv, saveFileDialogService.GetFullFileName());
                            break;
                        case ExportType.XLSX:
                            saveFileDialogService.Filter = "Excel 2007 files|*.xlsx";
                            if (saveFileDialogService.ShowDialog())
                                exportService.ExportToXLSX(tv, saveFileDialogService.GetFullFileName());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error exporting data:" + ex.Message);
            }
        }

        /// <summary>
        /// Prints the current document
        /// </summary>
        /// <param name="type"></param>
        public void PrintAction(object parameter, object view, object service = null) //PrintCommand
        {
            try
            {
                if (service is IExportService)
                {
                    IExportService exportService = service as IExportService;
                    TableView tv = view as TableView;
                    Enum.TryParse(parameter.ToString(), out PrintType type);

                    switch (type)
                    {
                        case PrintType.PREVIEW:
                            exportService.ShowPrintPreview(tv);
                            break;
                        case PrintType.PRINT:
                            exportService.Print(tv);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error printing data:" + ex.Message);
            }
        }

    }
}
