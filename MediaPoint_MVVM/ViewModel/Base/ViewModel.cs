using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;
using MediaPoint.MVVM.Extensions;
using System.Diagnostics;
using MediaPoint.MVVM.Messaging;

namespace MediaPoint.MVVM
{
    public class ViewModel : INotifyPropertyChanged, IDisposable, IDataErrorInfo
    {
        #region Members

        private readonly Dictionary<string, PropertyItem> _properties = new Dictionary<string,PropertyItem>();
        private Validator _validator;
        private IMessenger _messengerInstance;

        #endregion

        #region ctor
        
        /// <summary>
        /// Initializes a new instance of the ViewModelBase class.
        /// </summary>
        /// <param name="messenger">An instance of a <see cref="Messenger" />
        /// used to broadcast messages to other objects. If null, this class
        /// will attempt to broadcast using the Messenger's default
        /// instance.</param>
        public ViewModel(IMessenger messenger) : this()
        {
            Messenger = messenger;
        }

        /// <summary>
        /// Initializes a new instance of the ViewModelBase class.
        /// </summary>
        public ViewModel()
        {
#if DEBUG
            ViewModel.ObjectMapRegister(this.GetHashCode(), this);
#endif
        }

        #endregion

        #region Methods

		/// <summary>
		/// Helper method to register on the actual messanger of this VM
		/// </summary>
		/// <typeparam name="Mess">Type of message</typeparam>
		/// <param name="action">Action performed when received</param>
		[DebuggerStepThrough]
		protected void Register<Mess>(Action<Mess> action)
		{
			this.Messenger.Register<Mess>(this, action);
		}

		/// <summary>
		/// Shortcut for Messanger.Send(VM, Message);
		/// </summary>
		/// <typeparam name="Mess">Message type</typeparam>
		/// <param name="message">message</param>
		[DebuggerStepThrough]
		protected void Send<Mess>(Mess message) where Mess : MessageBase
		{
			this.Messenger.Send<Mess>(message);
		}

        /// <summary>
        /// Gets the value of a property matching the given expression.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="nameExpression">The expression pointing to the property.</param>
        protected T GetValue<T>(Expression<Func<T>> nameExpression)
        {
            // Get the key of the property
            string key = nameExpression.ToString();

            PropertyItem p;
            if (_properties.TryGetValue(key, out p))
            {
                // return the value
                return (T)p.Value;
            }
            else
            {
                // return null
                return default(T);
            }
        }

		/// <summary>
		/// Try getting the value of a property by name
		/// </summary>
		/// <param name="propertyName">Property name</param>
		/// <param name="value">Value</param>
		/// <returns>True if found false otherwise</returns>
		public bool TryGetValue(string propertyName, out object value)
		{
			PropertyItem p;
			string key = String.Format("() => value({0}.{1}).{2}", this.GetType().Namespace, this.GetType().Name, propertyName);
			if (_properties.TryGetValue(key, out p))
			{
				value = p.Value;
				return true;
			}
			else
			{
				value = null;
				return false;
			}
		}

        /// <summary>
        /// Sets the value of a property matching the given expression.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="nameExpression">The expression pointing to the property.</param>
        /// <param name="value">The value to set.</param>
		/// <returns>True if the value changed, false if not</returns>
        protected bool SetValue<T>(Expression<Func<T>> nameExpression, T value)
        {
            // Get the key of the property
            string key = nameExpression.ToString();

            PropertyItem p;
            lock (_properties)
            {
                if (_properties.TryGetValue(key, out p))
                {
                    // Make sure the property value has changed
                    if ((p.Value == null && value == null) || (p.Value != null && p.Value.Equals(value)))
                    {
                        return false;
                    }

                    // Set the new value
                    p.Value = value;
                }
                else
                {
                    // Create the new property item
                    p = new PropertyItem
                        {
                            Name = nameExpression.GetName(),
                            Value = value
                        };

                    // Add the new propery item
                    _properties.Add(key, p);
                }
            }

            // Raise property changed event
            OnPropertyChanged(new PropertyChangedEventArgs(p.Name));

            return true;
        }

        /// <summary>
        /// Unregisters this instance from the Messenger class.
        /// <para>To cleanup additional resources, override this method, clean
        /// up and then call base.Cleanup().</para>
        /// </summary>
        public virtual void Cleanup()
        {
            Messenger.Unregister(this);
            ClearBindings();
        }

        public void RefreshProperty<T,P>(Expression<Func<T,P>> nameExpression) {
            if (nameExpression.Parameters[0].Type.Name == this.GetType().Name)
                OnPropertyChanged(new PropertyChangedEventArgs(nameExpression.GetName()));
            else
                throw new ArgumentException("Wrong parameters to expression. Not for this ViewModel.", "nameExpression");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the user-friendly name of this object.
        /// Child classes can set this property to a new value,
        /// or override it to determine the value on-demand.
        /// </summary>
        public virtual string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets an instance of a <see cref="IMessenger" /> used to
        /// broadcast messages to other objects. If null, this class will
        /// attempt to broadcast using the Messenger's default instance.
        /// </summary>
        public IMessenger Messenger
        {
            get
            {
                return _messengerInstance ?? Messaging.Messenger.Default;
            }
            set
            {
                if (_messengerInstance != null)
                {
                    _messengerInstance.Unregister(this);
                }
                else
                {
					Messaging.Messenger.Default.Unregister(this);
                }

                _messengerInstance = value;
            }
        }

        #endregion

        #region Interface Implementations

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        [DebuggerStepThrough]
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Represents the method that will handle the PropertyChanged event raised when a property is
        /// changed on a component.
        /// </summary>
        /// <param name="e">A PropertyChangedEventArgs that contains the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            VerifyPropertyName(e.PropertyName);

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }


        /// <summary>
        /// Represents the method that will handle the PropertyChanged event raised when a property is
        /// changed on a component.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="nameExpression">The expression pointing to the property.</param>
        protected void OnPropertyChanged<T>(Expression<Func<T>> nameExpression)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameExpression.GetName()));
        }


        /// <summary>
        /// Warns the developer if this object does not have a public property with the specified name.
        /// This  method does not exist in a Release build.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void VerifyPropertyName(string propertyName)
        {
            if (!(string.IsNullOrEmpty(propertyName) || (GetType().GetProperty(propertyName) != null)))
            {
                throw new ArgumentException("Not a property.", propertyName);
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            this.OnDispose();
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
            Cleanup();
        }


        /// <summary>
        /// Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~ViewModel()
        {
#if xxxDEBUG
            // its not possible to access managed member variables inside a finalizer because the members might be destructed already
            string displayName = null;
            try
            {
                displayName = this.DisplayName;
            }
            catch
            {
                displayName = "(No name)";
            }

            ViewModelBase.ObjectMapUnRegister(this.GetHashCode());
            string msg = string.Format("{0} ({1}) ({2}) Finalized", this.GetType().Name, displayName, this.GetHashCode());
            System.Diagnostics.Debug.WriteLine(msg);
#endif
            ClearBindings();
        }

        #endregion // IDisposable Members

        #region IDataErrorInfo Members

        /// <summary>
        /// Gets an error message indicating what is wrong with this object.
        /// </summary>
        public string Error
        {
			get { return _validator.HasErrors ? String.Join(Environment.NewLine, new List<string>(_validator.GetErrors()).ToArray()) : String.Empty; }
        }


        /// <summary>
        /// Gets the error message for the property with the given name.
        /// </summary>
        /// <param name="propertyName">The name of the property whose error message to get.</param>
        /// <returns>The error message for the property. The default is an empty string ("").</returns>
        public string this[string propertyName]
        {
            get { return _validator != null ? _validator.Validate(propertyName) : string.Empty; }
        }

        /// <summary>
        /// Adds a validation rule.
        /// </summary>
        /// <param name="nameExpression">The expression pointing to the property.</param>
        /// <param name="validationRule">The validation rule to add.</param>
        protected void AddValidation(
            Expression<Func<object>> nameExpression,
            IValidationRule validationRule)
        {
            if (_validator == null)
            {
                _validator = new Validator();
            }

            _validator.Add(nameExpression, validationRule);
        }


        /// <summary>
        /// Validates all added validation rules.
        /// </summary>
        /// <returns>true if validation succeeds; otherwise false.</returns>
        protected bool Validate()
        {
            return _validator != null ? _validator.ValidateAll() : true;
        }

        #endregion

        #endregion

        #region "Static stuff" 

        #region "Static Members"

        private static readonly Dictionary<int, WeakReference> _instances = new Dictionary<int, WeakReference>();

        #endregion

        #region "Static Methods"

        /// <summary>
        /// Finds a viewmodel in the object map
        /// </summary>
        /// <param name="hashCode">The VM key in the map</param>
        /// <returns>Viewmodel reference of null if not found</returns>
        public static ViewModel FromHash(int hashCode)
        {
            lock (_instances)
            {
                if (_instances.Keys.Contains(hashCode) && _instances[hashCode].IsAlive)
                    return (ViewModel)_instances[hashCode].Target;
            }

            return null;
        }

        /// <summary>
        /// List of current viewmodels in existence
        /// Beware of taking the list and keeping references on top context...
        /// </summary>
        /// <returns>Enumeration of viewmodels in existence</returns>
        public static IEnumerable<ViewModel> GetObjects()
        {
            lock (_instances)
            {
                return from w in _instances where w.Value.IsAlive select (ViewModel)w.Value.Target;
            }
        }

        /// <summary>
        /// Puts a viewmodel in the VM object map
        /// </summary>
        /// <param name="hashCode">The hashcode to use as key (often object.GetHashCode())</param>
        /// <param name="vm">Viewmodel to store</param>
        public static void ObjectMapRegister(int hashCode, ViewModel vm)
        {
            lock (_instances)
            {
                _instances[hashCode] = new WeakReference(vm);
            }
        }

        /// <summary>
        /// Remove the specified object (by key) from the map
        /// </summary>
        /// <param name="hashCode"></param>
        public static void ObjectMapUnRegister(int hashCode)
        {
            lock (_instances)
            {
                _instances.Remove(hashCode);
            }
        }

        #endregion

        #endregion

        #region "Binding"

        /// <summary>
        /// Adds a creates binding for this VM to hold a reference to
        /// </summary>
        /// <param name="key">Key of the new binding</param>
        /// <param name="binding">The new binding</param>
        public void AddBinding(string key, IBinding<ViewModel, ViewModel> binding)
        {
            if (key.Contains("."))
            {
                var words = key.Split('.');
                key = words[words.Length - 1];
            }
            if (bindings.ContainsKey(key))
            {
                bindings[key].Dispose();
                bindings.Remove(key);
            }
            bindings.Add(key, binding);
            OnPropertyChanged("Bindings");
        }

        /// <summary>
        /// Removes all the bindings of this VM
        /// </summary>
        public void ClearBindings()
        {
            foreach (var b in bindings.Values)
            {
                b.Dispose();
            }
            bindings.Clear();
            OnPropertyChanged("Bindings");
        }

        /// <summary>
        /// Removes a binding by key
        /// </summary>
        /// <param name="key">The key of the binding short or long ('P' or 'Path: X.Y -> O.P')</param>
        public void RemoveBinding(string key)
        {
            lock (bindings)
            {
                if (key.Contains("."))
                {
                    var words = key.Split('.');
                    key = words[words.Length - 1];
                }

                var b = bindings[key];
                b.Dispose();
                bindings.Remove(key);
            }
            OnPropertyChanged("Bindings");
        }

        private Dictionary<string, IBinding<ViewModel, ViewModel>> bindings = new Dictionary<string, IBinding<ViewModel, ViewModel>>();

        /// <summary>
        /// Exposes the bindings that this VM has
        /// </summary>
        public IEnumerable<IBinding<ViewModel, ViewModel>> Bindings
        {
            get
            {
                foreach (var o in bindings.Values)
                    yield return o;
            }
        }

        public void CreateBinding<T, T2, R1, R2>(T self, Expression<Func<T, R2>> destProp, T2 source, Expression<Func<T2, R1>> sourceProperty)
            where T : ViewModel
            where T2 : ViewModel
            where R1 : R2
        {
            bool pathError = false;
            string bindingName = String.Empty;
            string lastStep = String.Empty; // in case of error
            string sourcePropertyName = null;
            string destinationPropertyName = ((MemberExpression)destProp.Body).Member.Name;
            List<string> bindingPath = new List<string>();

            var memberExpr = (MemberExpression)sourceProperty.Body;
            bindingPath.Add(memberExpr.Member.Name);

            // is this a deep binding?
            while (memberExpr.Expression is MemberExpression)
            {
                memberExpr = ((MemberExpression)memberExpr.Expression);
                bindingPath.Add(memberExpr.Member.Name);
            }

            // expressions go from the deepest, we need it in the reverse order
            bindingPath.Reverse();

			if (source != null && self != null)
			{
				object val = typeof(T2).GetProperty(bindingPath[bindingPath.Count - 1]).GetValue(source, null);
				if (typeof(T).GetProperty(destinationPropertyName).GetValue(self, null) != val)
					typeof(T).GetProperty(destinationPropertyName).SetValue(self, val, null);
			}

            if (bindingPath.Count == 1)
            {
                // simple binding X.Y <- A.B
                var binding = new Binder<T2, T>(bindingPath);
                sourcePropertyName = bindingPath[0];
                binding.Bind(source, self, sourcePropertyName, destinationPropertyName);
                bindingName = binding.Path;
                self.AddBinding(destinationPropertyName, binding);
            }
            else
            {
                // deep binding X.Y <- A.B.C..N
                var pathClone = new List<string>(bindingPath); // preserve path since getPathObject changes the parameter content
                var binding = new Binder<ViewModel, T>(source, bindingPath);
                var bindSource = (ViewModel)Binder<ViewModel, T>.GetPathObject(source, pathClone, 1); // penultimate object
                // bind to the penultimate object in path and its property
                binding.Bind(bindSource, self, bindingPath[bindingPath.Count - 1], destinationPropertyName);
                bindingName = binding.Path;

                // create eventwrappers (middleware) for the objects in the path
                // which trigger updating of this binding to be always functional
                for (int i = 0; i < bindingPath.Count - 1; i++)
                {
                    lastStep = bindingPath[i];
                    var tempPath = new List<string>(bindingPath); // modifiable copy
                    var temp = Binder<ViewModel, T>.GetPathObject(source, tempPath, bindingPath.Count - i);

                    if (temp is ViewModel)
                    {
                        // add event wrappers for middle step properties because
                        // when some of these changes we must recreate the eventwrappers
                        // and unregister old ones which will hold nothing anymore
                        binding.EventWrappers.Add(new EventWrapper<ViewModel, T>((ViewModel)temp, binding, bindingPath[i], true));
                    }
                    else
                    {
                        // we can handle only INotifyPropertyChanged objects
                        if (temp != null)
                        {
                            pathError = true;
                            break;
                        }
                    }
                }

                if (!pathError)
                {
                    // reorder: first wrapper as last since the Binder creates
                    // the final binding (and eventwrapper) as soon as we Bind, and we need the correct
                    // order since we are attaching the path before it afterwards
                    var ev = binding.EventWrappers[0];
                    binding.EventWrappers.RemoveAt(0);
                    binding.EventWrappers.Add(ev);

                    // save the binding to the viewmodel, not doing this will GC it and do nothing
                    self.AddBinding(destinationPropertyName, binding);
                }
                else
                {
					Debug.WriteLine("ERROR: Binding '" + bindingName + "' cannot be created because not all objects in the binding path '" + String.Join(".", bindingPath.ToArray()) + "' implement INotifyPropertyChanged." + Environment.NewLine + "   *** Failed on: '" + lastStep + "'");
                }
            }

        }
        #endregion
    }
    
    /// <summary>
    /// Class wrapping up the essential parts of a property.
    /// </summary>
    class PropertyItem
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public object Value { get; set; }
    }

}
