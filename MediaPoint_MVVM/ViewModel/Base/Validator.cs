using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MediaPoint.MVVM.Extensions;

namespace MediaPoint.MVVM
{
    /// <summary>
    /// Class responsible for validating a ViewModel.
    /// </summary>
    class Validator
    {
        private readonly IList<ValidationData> rules;
        private readonly IList<string> errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class.
        /// </summary>
        public Validator()
        {
            rules = new List<ValidationData>();
            errors = new List<string>();
        }


        /// <summary>
        /// Adds a validation rule.
        /// </summary>
        /// <param name="nameExpression">The expression pointing to the property.</param>
        /// <param name="validationRule">The validation rule to add.</param>
        public void Add(Expression<Func<object>> nameExpression, IValidationRule validationRule)
        {
            rules.Add(new ValidationData(nameExpression, validationRule));
        }

        /// <summary>
        /// Returns the enumeration of errors
        /// </summary>
        /// <returns>IEnumerable&lt;string&gt; of error strings</returns>
        public IEnumerable<string> GetErrors()
        {
            return errors;
        }

        /// <summary>
        /// Gets if this validator has errors after calling Validate or ValidateAll
        /// </summary>
        public bool HasErrors
        {
            get { return errors.Count > 0; }
        }

        /// <summary>
        /// Validates a specific property on a ViewModel.
        /// </summary>
        /// <param name="propertyName">The property to validate.</param>
        /// <returns>The error message for the property. The default is an empty string ("").</returns>
        public string Validate(string propertyName, bool clearErrors = true)
        {
            if (clearErrors) errors.Clear();

            IEnumerable<ValidationData> relevantRules = rules.Where(r => r.Name == propertyName);

            foreach (ValidationData relevantRule in relevantRules)
            {
                if (!relevantRule.Rule.Validate(relevantRule.Property()))
                {
                    if (errors.Contains(relevantRule.Rule.ErrorMessage)) errors.Add(relevantRule.Rule.ErrorMessage);
                    return relevantRule.Rule.ErrorMessage;
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// Validates all added validation rules.
        /// </summary>
        /// <returns>true if validation succeeds; otherwise false.</returns>
        public bool ValidateAll()
        {
            errors.Clear();
            return rules.Aggregate(true, (success, rule) => success && Validate(rule.Name, false) == string.Empty);
        }


        /// <summary>
        /// Class acting as data carrier for a validation information.
        /// </summary>
        private class ValidationData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Validator"/> class.
            /// </summary>
            /// <param name="nameExpression">The expression pointing to the property.</param>
            /// <param name="rule">The rule.</param>
            public ValidationData(Expression<Func<object>> nameExpression, IValidationRule rule)
            {
                Name = nameExpression.GetName();
                Property = nameExpression.Compile();
                Rule = rule;
            }


            /// <summary>
            /// Gets the name of the property.
            /// </summary>
            public string Name { get; private set; }


            /// <summary>
            /// Gets the name expression to validate.
            /// </summary>
            public Func<object> Property { get; private set; }


            /// <summary>
            /// Gets the rule.
            /// </summary>
            public IValidationRule Rule { get; private set; }
        }
    }
}
