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
        //     Occurs when a property value changes.
        event EventHandler Saved;

        bool IsValid { get; }
        bool IsDirty { get; }
        bool Save();
        void Closing(out bool cancelCloseOperation);


        IViewModelHost Host { get; set; }
    }
}
