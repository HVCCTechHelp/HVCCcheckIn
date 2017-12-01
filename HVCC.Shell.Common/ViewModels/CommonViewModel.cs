namespace HVCC.Shell.Common.ViewModels
{
    using DevExpress.Mvvm;
    using HVCC.Shell.Common.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using DevExpress.Xpf.Grid;

    public abstract class CommonViewModel : ViewModelBase, IViewModel
    {

        private string _caption = string.Empty;
        public string Caption
        {
            get
            {
                return _caption;
            }
            set
            {
                if (value != _caption)
                {
                    _caption = value;
                    // Need to rasise CaptionChanged
                    //this.RaiseCaptionChangedEvent(new CaptionChangedEventArgs() { NewCaption = value });
                }
            }
        }

        public abstract bool IsValid { get; }

        public abstract bool IsDirty { get; }

        public abstract bool IsBusy { get; set; }

        public IHost Host { get; set; }
        public TableView Table { get; set; }

        public event EventHandler CaptionChanged;

        // Added for Caption set property.
        //private void RaiseCaptionChangedEvent(CaptionChangedEventArgs e)
        //{
        //    if (this.CaptionChanged != null)
        //    {
        //        this.CaptionChanged(this, e);
        //    }
        //}

        public void Closing(out bool cancelCloseOperation)
        {
            if (IsDirty)
            {
                // ask to save or close
                // If 'result' = YES, return true
                cancelCloseOperation = true;
                // else return false
            }
            cancelCloseOperation = false;
        }

    }

    // Added for Caption set property.
    //public class CaptionChangedEventArgs : EventArgs
    //{
    //    public string NewCaption { get; internal set; }
    //
}
