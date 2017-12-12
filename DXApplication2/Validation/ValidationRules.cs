using HVCC.Shell.Models;
using System;

namespace HVCC.Shell.Validation
{

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

    }
}
