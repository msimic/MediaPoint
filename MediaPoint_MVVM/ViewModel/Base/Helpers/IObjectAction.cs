using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPoint.MVVM.Helpers
{
    /// <summary>
    /// This interface is meant for the <see cref="WeakAction{T}" /> class and can be 
    /// useful if you store multiple WeakAction{T} instances but don't know in advance
    /// what type T represents.
    /// </summary>
    public interface IObjectAction
    {
        /// <summary>
        /// Executes an action.
        /// </summary>
        /// <param name="parameter">A parameter passed as an object,
        /// to be casted to the appropriate type.</param>
        void Execute(object parameter);

        /// <summary>
        /// Gets the underlying object
        /// </summary>
        /// <returns>Object it contains</returns>
        object GetObject();
    }
}

