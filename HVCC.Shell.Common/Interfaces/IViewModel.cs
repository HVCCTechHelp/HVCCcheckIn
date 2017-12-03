namespace HVCC.Shell.Common.Interfaces
{
    using DevExpress.Xpf.Grid;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IViewModel
    {
        string Caption { get; set; }

        //event EventHandler CaptionChanged;

        TableView Table { get; set; }

        bool IsValid { get; }
        bool IsDirty { get; set; }
        void Closing(out bool cancelCloseOperation);

        IHost Host { get; set; }
    }
}
