using System;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Employee.CustomValidation
{
    //public class DateRangeAttribute : RangeAttribute
    //{
    //    public DateRangeAttribute(string minVal) : base(typeof(DateTime), minVal, DateTime.Now.ToShortDateString())
    //    {

    //    }

    //}

    public class ValidateDateRange : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            DateTime dt = (DateTime)value;
            if (dt >= DateTime.UtcNow)
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage ?? "Make sure your date is >= than today");
        }
    }
}
