namespace HVCC.Property.Validation
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ValidationRules
    {
        public static string IsRelationshipValid(Relationship row)
        {
            string message = String.Empty;
            string control = String.Empty;

            if ((String.IsNullOrEmpty(row.FName) ||
                String.IsNullOrEmpty(row.LName)) ||
                (String.IsNullOrEmpty(row.RelationToOwner))
                && (0 != row.PropertyID))
            {
                if (String.IsNullOrEmpty(row.FName)) control = "FName";
                else if (String.IsNullOrEmpty(row.LName)) control = "LName";
                else control = "RelationshipToOwner";
            }
            return control;
        }
    }
}
