using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.ComponentModel;
using MediaPoint.MVVM.Helpers;

namespace MediaPoint.MVVM.Extensions
{
    /// <summary>
    /// Just some helpers for bindings
    /// </summary>
    public static class ViewModelBaseExtensions
    {
        /// <summary>
        /// Makes an inferred binding on any viewmodel without specifying T
        /// </summary>
        /// <typeparam name="T">Destination viewmodel</typeparam>
        /// <typeparam name="T2">Source viewmodel</typeparam>
        /// <typeparam name="R1">Type of target property</typeparam>
        /// <typeparam name="R2">Type of source property</typeparam>
        /// <param name="self">This viewmodel</param>
        /// <param name="destProp">Destination property expression</param>
        /// <param name="source">Source viewmodel</param>
        /// <param name="sourceProperty">Source property expression</param>
        public static void Bind<T, T2, R1, R2>(this T self, Expression<Func<T, R2>> destProp, T2 source, Expression<Func<T2, R1>> sourceProperty)
            where T : ViewModel
            where T2 : ViewModel
            where R1 : R2
        {			
            self.CreateBinding<T, T2, R1, R2>(self, destProp, source, sourceProperty);
        }
    }

}
