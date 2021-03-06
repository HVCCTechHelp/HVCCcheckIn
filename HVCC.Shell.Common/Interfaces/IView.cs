﻿namespace HVCC.Shell.Common.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IView
    {
        IViewModel ViewModel { get; set; }

        object SaveState();
        void RestoreState(object state);
    }
}
