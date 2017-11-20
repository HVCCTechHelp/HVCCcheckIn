namespace HVCC.Shell.Common.Interfaces
{
    using System;

    public interface IForm
    {
        // Summary:
        //     Occurs when a property value changes.
        event EventHandler Saved;

        string Caption { get; }

        bool IsDirty { get; }
        bool IsValid { get; }
        bool IsReadOnly { get; }
        bool Save();
        bool IsSaveExecuting { get; }

        bool AllowRefresh { get; }
        bool AllowRefreshDuringSave { get; }
        void Refresh();

        void Closing(out bool cancelCloseOperation);
    }
}