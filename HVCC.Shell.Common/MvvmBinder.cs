using HVCC.Shell.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HVCC.Shell.Common
{
    public class MvvmBinder : IMvvmBinder
    {
        public MvvmBinder(IDataContext dc, IView v, IViewModel vm)
        {
            this.DataContext = dc;
            this.View = v;
            this.ViewModel = vm;
        }

        public IDataContext DataContext { get; private set; }

        public IView View { get; private set; }

        public IViewModel ViewModel { get; private set; }
    }
}
