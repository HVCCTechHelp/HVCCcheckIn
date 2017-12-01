namespace HVCC.Shell.Models
{
    using HVCC.Shell.Common;
    using HVCC.Shell.Common.Interfaces;
    using HVCC.Shell.ViewModels;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class Tile
    {
        public string Identifier { get; set; }

        private ObservableCollection<IMvvmBinder> _binder = new ObservableCollection<IMvvmBinder>();
        public ObservableCollection<IMvvmBinder> Binder { get; set; }
    }
}
