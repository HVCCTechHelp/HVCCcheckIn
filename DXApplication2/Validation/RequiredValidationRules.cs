namespace HVCC.Shell.Validation
{
    using DevExpress.Mvvm;
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Windows.Controls;

    class RequiredValidationRule : ValidationRule
    {
        public string FieldName { get; set; }

        /// <summary>
        /// ViewModel property validation. Invoked by a PropertyChanged event
        /// </summary>
        /// <param name="value"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            //string error = CheckNullInput(FieldName, value);
            string error = string.Empty;

            switch (FieldName)
            {
                case "MailTo":
                case "Address":
                case "City":
                case "Zip":
                case "TransactionMethod":
                case "TransactionAppliesTo":
                case "TransactionComment":
                    error = CheckNullInput(FieldName, value);
                    break;
                case "CreditAmount":
                case "DebitAmount":
                    error = CheckDecimalInput(FieldName, value);
                    break;
                case "State":
                    error = CkStateAbbreviation(FieldName, value);
                    break;
                case "TransactionDate":
                    error = CheckDateInput(FieldName, value);
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(error))
                return new ValidationResult(false, error);
            return ValidationResult.ValidResult;
        }

        /// <summary>
        /// Validates that required field is not blank/empty
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="fieldValue"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static string CheckNullInput(string fieldName, object fieldValue, object nullValue = null)
        {
            string errorMessage = string.Empty; 

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
                   "MailTo" == fieldName
                || "Address" == fieldName
                || "City" == fieldName
                || "State" == fieldName
                || "Zip" == fieldName
                || "TransactionMethod" == fieldName
                || "TransactionAppliesTo" == fieldName
                || "TransactionComment" == fieldName
                )
            {
                return CheckNullInput(fieldName, fieldValue, nullValue);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string CheckDecimalInput(string fieldName, object fieldValue, object nullValue = null)
        {
            string errorMessage = string.Empty;
            decimal? value = fieldValue as decimal?;
            if (nullValue != null && nullValue.Equals(fieldValue))
            {
                errorMessage = string.Format("You cannot leave the {0} field empty.", fieldName);
            }

            else if (fieldValue == null || value == 0) 
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
        public static string CheckDecimalInput<T>(Expression<Func<T>> expression, object fieldValue, object nullValue = null)
        {
            //// Determine which field (property) we are validating
            string fieldName = BindableBase.GetPropertyName(expression);   // Is hit...
            if (
                   "CreditAmount" == fieldName
                || "DebitAmount" == fieldName
                )
            {
                return CheckDecimalInput(fieldName, fieldValue, nullValue);
            }
            else
            {
                return string.Empty;
            }
        }

        public static string CheckDateInput(string fieldName, object fieldValue, object nullValue = null)
        {
            string errorMessage = string.Empty;
            DateTime dt = (DateTime)fieldValue;

            if (nullValue != null && nullValue.Equals(fieldValue))
            {
                errorMessage = string.Format("You cannot leave the {0} field empty.", fieldName);
            }

            else if (fieldValue == null || (1 == dt.Year && 1 == dt.Month && 1 == dt.Day))
            {
                errorMessage = string.Format("You must enter a valid date.", fieldName);
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
        public static string CheckDateInput<T>(Expression<Func<T>> expression, object fieldValue, object nullValue = null)
        {
            //// Determine which field (property) we are validating
            string fieldName = BindableBase.GetPropertyName(expression);   // Is hit...
            if (
                   "TransactionDate" == fieldName
                )
            {
                return CheckDateInput(fieldName, fieldValue, nullValue);
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
            string errorMessage = string.Empty;
            if (null != fieldValue)
            {
                if (fieldName.Length != 2 && states.IndexOf(fieldValue.ToString()) < 1)
                {
                    errorMessage = string.Format("{0} invalid state abbreviation.", fieldName);
                }
            }
            else
            {
                return CheckNullInput(fieldName, fieldValue, nullValue);
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

            if ("State" == fieldName)
            {
                return CkStateAbbreviation(fieldName, fieldValue, nullValue);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}

