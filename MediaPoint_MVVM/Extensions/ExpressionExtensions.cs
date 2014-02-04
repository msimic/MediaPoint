using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace MediaPoint.MVVM.Extensions
{
    /// <summary>
    /// Class containing Expression extensions.
    /// </summary>
    static class ExpressionExtensions
    {
        /// <summary>
        /// Gets the name of the expression.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="nameExpression">The name expression.</param>
        /// <returns>The name of the expression.</returns>
        public static string GetName<T>(this Expression<T> extension)
        {
            UnaryExpression unaryExpression = extension.Body as UnaryExpression;

            // Convert name expression into MemberExpression
            MemberExpression memberExpression = unaryExpression != null ?
                (MemberExpression)unaryExpression.Operand :
                (MemberExpression)extension.Body;

            return memberExpression.Member.Name;
        }
    }
}
