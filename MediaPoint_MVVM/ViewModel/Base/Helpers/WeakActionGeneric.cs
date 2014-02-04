using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace MediaPoint.MVVM.Helpers
{
    /// <summary>
    /// Stores an Action without causing a hard reference
    /// to be created to the Action's owner. The owner can be garbage collected at any time.
    /// </summary>
    /// <typeparam name="T">The type of the Action's parameter.</typeparam>
    public class WeakAction<T> : WeakAction, IObjectAction
    {
        private readonly Action<T> _action;

        /// <summary>
        /// Initializes a new instance of the WeakAction class.
        /// </summary>
        /// <param name="target">The action's owner.</param>
        /// <param name="action">The action that will be associated to this instance.</param>
        public WeakAction(object target, Action<T> action)
            : base(target, null)
        {
            _action = action;
        }

        /// <summary>
        /// Gets the Action associated to this instance.
        /// </summary>
        public new Action<T> Action
        {
            get
            {
                return _action;
            }
        }

        /// <summary>
        /// Executes the action. This only happens if the action's owner
        /// is still alive. The action's parameter is set to default(T).
        /// </summary>
		[DebuggerStepThrough]
		public new void Execute()
        {
            if (_action != null
                && IsAlive)
            {
                _action(default(T));
            }
        }

        /// <summary>
        /// Executes the action. This only happens if the action's owner
        /// is still alive.
        /// </summary>
        /// <param name="parameter">A parameter to be passed to the action.</param>
		[DebuggerStepThrough]
        public void Execute(T parameter)
        {
            if (_action != null
                && IsAlive)
            {
                _action(parameter);
            }
        }

        /// <summary>
        /// Executes the action with a parameter of type object. This parameter
        /// will be casted to T. This method implements <see cref="IExecuteWithObject.ExecuteWithObject" />
        /// and can be useful if you store multiple WeakAction{T} instances but don't know in advance
        /// what type T represents.
        /// </summary>
        /// <param name="parameter">The parameter that will be passed to the action after
        /// being casted to T.</param>
		[DebuggerStepThrough]
		public void Execute(object parameter)
        {
            var parameterCasted = (T)parameter;
            Execute(parameterCasted);
        }

        /// <summary>
        /// Returns the action as object
        /// </summary>
        /// <returns></returns>
        public object GetObject()
        {
            return _action;
        }
    }

    /// <summary>
    /// Represents a weak reference, which references an object while still allowing
    /// that object to be reclaimed by garbage collection.
    /// </summary>
    /// <typeparam name="T">The type of the object that is referenced.</typeparam>
    [Serializable]
    public class WeakReference<T>
        : WeakReference where T : class
    {
        /// <summary>
        /// Initializes a new instance of the WeakReference{T} class, referencing
        /// the specified object.
        /// </summary>
        /// <param name="target">The object to reference.</param>
        public WeakReference(T target)
            : base(target)
        { }
        /// <summary>
        /// Initializes a new instance of the WeakReference{T} class, referencing
        /// the specified object and using the specified resurrection tracking.
        /// </summary>
        /// <param name="target">An object to track.</param>
        /// <param name="trackResurrection">Indicates when to stop tracking the object. 
        /// If true, the object is tracked
        /// after finalization; if false, the object is only tracked 
        /// until finalization.</param>
        public WeakReference(T target, bool trackResurrection)
            : base(target, trackResurrection)
        { }
        protected WeakReference(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
        /// <summary>
        /// Gets or sets the object (the target) referenced by the 
        /// current WeakReference{T} object.
        /// </summary>
        public new T Target
        {
            get
            {
                return (T)base.Target;
            }
            set
            {
                base.Target = value;
            }
        }
    }  
}
