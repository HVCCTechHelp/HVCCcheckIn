namespace HVCC.Shell.Validation
{
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ValidationRules
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static string IsRelationshipRowValid(Relationship row)
        {
            // All three of the Relationship cells must contain data
            if (String.IsNullOrEmpty(row.FName)) return "FName";
            else if (String.IsNullOrEmpty(row.LName)) return "LName";
            else if (String.IsNullOrEmpty(row.RelationToOwner)) return "RelationshipToOwner";
            else return String.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        internal static string IsWaterMeterReadingRowValid(WaterMeterReading row)
        {
            if (String.IsNullOrEmpty(row.ReadingDate.ToString())) return "Date";
            else if (String.IsNullOrEmpty(row.MeterReading.ToString())) return "Reading";
            else return String.Empty;
        }

        private static string states = "|AL|AK|AS|AZ|AR|CA|CO|CT|DE|DC|FM|FL|GA|GU|HI|ID|IL|IN|IA|KS|KY|LA|ME|MH|MD|MA|MI|MN|MS|MO|MT|NE|NV|NH|NJ|NM|NY|NC|ND|MP|OH|OK|OR|PW|PA|PR|RI|SC|SD|TN|TX|UT|VT|VI|VA|WA|WV|WI|WY|";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool isStateAbbreviation(string state)
        {
            return state.Length == 2 && states.IndexOf(state) > 0;
        }

    }
}
