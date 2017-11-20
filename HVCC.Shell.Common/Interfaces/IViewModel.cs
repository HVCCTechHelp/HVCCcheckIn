namespace HVCC.Shell.Common.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IViewModel
    {
        string Caption { get; set; }

        event EventHandler CaptionChanged;

        object this[string property] { get; set; }

        bool IsValid { get; }

        bool IsSelectGranted { get; }
        bool IsInsertGranted { get; }
        bool IsUpdateGranted { get; }
        bool IsDeleteGranted { get; }

        IViewModelHost Host { get; set; }
    }
}
