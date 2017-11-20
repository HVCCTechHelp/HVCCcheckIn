namespace HVCC.Shell.Resources
{
    using DevExpress.Xpf.Grid;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using HVCC.Shell.Models;

    /// <summary>
    /// Selects the textbox or combobox edit control dependent on cells value
    /// </summary>
    public class RowTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CmbTemplate { get; set; }
        public DataTemplate TxtTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            EditGridCellData row = item as EditGridCellData;
            Models.Relationship p = row.RowData.Row as Relationship;

            if (null == p || String.IsNullOrEmpty(p.RelationToOwner))
                return CmbTemplate;
            else
                return TxtTemplate;
        }
    }
}
