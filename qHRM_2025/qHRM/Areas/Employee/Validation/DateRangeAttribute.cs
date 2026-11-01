using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Employee.CustomValidation
{

    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private string _startDate;       

        public DateGreaterThanAttribute(string startDate)
        {
            _startDate = startDate;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var propertyInfo = validationContext.ObjectType.GetProperty(_startDate);
            var propertyValue = propertyInfo.GetValue(validationContext.ObjectInstance, null);
                                    
            try
            {
                if ((DateTime) value > (DateTime)propertyValue)
                {
                    return ValidationResult.Success;                    
                }
                else
                {
                    return new ValidationResult("Start date can not be greater than end date");
                }

            }
            catch (Exception)
            {
                return new ValidationResult("");
            }
        }

    }
}
