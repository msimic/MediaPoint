using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.MVVM
{
    /// <summary>
    /// Interface describing a validation rule.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Validates a value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>true if validation is successful; otherwise false.</returns>
        bool Validate(object value);


        /// <summary>
        /// Gets the error message if validation fails; otherwise an empty string ("").
        /// </summary>
        string ErrorMessage { get; }
    }
}
