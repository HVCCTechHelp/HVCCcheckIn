namespace HVCC.Shell.Common.Converters
{
    using DevExpress.Mvvm.UI;
    using DevExpress.Xpf.Grid;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RowClickConverter : IEventArgsConverter
    {
        public object Convert(object sender, object args)
        {
            RowDoubleClickEventArgs e = args as RowDoubleClickEventArgs;
            if (e.HitInfo.RowHandle == GridControl.NewItemRowHandle)
                return true;
            return false;
        }
    }
}
