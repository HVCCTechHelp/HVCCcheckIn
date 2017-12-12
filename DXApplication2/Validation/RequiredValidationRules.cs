namespace HVCC.Shell.Validation
{
    using System.Text;
    using System.Threading.Tasks;

    using DevExpress.Mvvm;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Windows.Controls;

    class RequiredValidationRule : ValidationRule
    {
        public string FieldName { get; set; }

        /// <summary>
        /// Validates that required field is not blank/empty
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static string CheckNullInput(string fieldName, object fieldValue, object nullValue = null)
        {
            string errorMessage = string.Empty;  // Is Hit...

            if (nullValue != null && nullValue.Equals(fieldValue))
            {
                errorMessage = string.Format("You cannot leave the {0} field empty.", fieldName);
            }

            else if (fieldValue == null || string.IsNullOrEmpty(fieldValue.ToString()))
            {
                errorMessage = string.Format("You cannot leave the {0} field empty.", fieldName);
            }

            return errorMessage;
        }

        /// <summary>
        /// Validates that the passed in property is not blank/empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="fieldValue"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static string CheckNullInput<T>(Expression<Func<T>> expression, object fieldValue, object nullValue = null)
        {
            //// Determine which field (property) we are validating
            string fieldName = BindableBase.GetPropertyName(expression);   // Is hit...
            if (
                   "OwnerFName" == fieldName
                || "OwnerLName" == fieldName
                || "OwnerAddress" == fieldName
                || "OwnerAddress2" == fieldName
                || "OwnerCity" == fieldName
                || "OwnerZip" == fieldName
                )
            {
                return CheckNullInput(fieldName, fieldValue, nullValue);
            }
            else
            {
                return string.Empty;
            }
        }

        private static string states = "|AL|AK|AS|AZ|AR|CA|CO|CT|DE|DC|FM|FL|GA|GU|HI|ID|IL|IN|IA|KS|KY|LA|ME|MH|MD|MA|MI|MN|MS|MO|MT|NE|NV|NH|NJ|NM|NY|NC|ND|MP|OH|OK|OR|PW|PA|PR|RI|SC|SD|TN|TX|UT|VT|VI|VA|WA|WV|WI|WY|";
        /// <summary>
        /// Validates state abbrevation
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static string CkStateAbbreviation(string fieldName, object fieldValue, object nullValue = null)
        {
            string state = fieldValue.ToString().ToUpper();
            string errorMessage = string.Empty;  

            if ( state.Length != 2 && states.IndexOf(state) == 0)
            {
                errorMessage = string.Format("{0} invalid state abbreviation.", state);
            }
            return errorMessage;
        }

        /// <summary>
        /// Validates that the passed in property is a valid state abbreviation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="fieldValue"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static string CkStateAbbreviation<T>(Expression<Func<T>> expression, object fieldValue, object nullValue = null)
        {
            //// Determine which field (property) we are validating
            string fieldName = BindableBase.GetPropertyName(expression);   // Is hit...

            if ("OwnerState" == fieldName)
            {
                return CkStateAbbreviation(fieldName, fieldValue, nullValue);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// ViewModel property validation. Invoked by a PropertyChanged event
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string error = CheckNullInput(FieldName, value);

            if (!string.IsNullOrEmpty(error))
                return new ValidationResult(false, error);
            return ValidationResult.ValidResult;
        }

    }
}

