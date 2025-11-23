using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Abp.Runtime.Validation.Interception;
using FluentValidation;

namespace Abp.FluentValidation {
    public class FluentValidationMethodParameterValidator(IValidatorFactory validatorFactory) : IMethodParameterValidator {
        public IReadOnlyList<ValidationResult> Validate(object validatingObject) {
            var validationErrors = new List<ValidationResult>();

            IValidator fvValidator = validatorFactory.GetValidator(validatingObject.GetType());

            if (fvValidator != null) {
                var validationContext = new ValidationContext<object>(validatingObject);
                var validationResult = fvValidator.Validate(validationContext);

                var mappedValidationErrors = validationResult.Errors
                    .Select(e => new ValidationResult(e.ErrorMessage, new[] { e.PropertyName }))
                    .ToList();

                validationErrors.AddRange(mappedValidationErrors);
            }

            return validationErrors;
        }
    }
}