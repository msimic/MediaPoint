using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using MediaPoint.MVVM.Helpers;

namespace MediaPoint.MVVM
{
    #region "Interfaces"

    /// <summary>
    /// Defines a covariant binding between two types
    /// </summary>
    /// <typeparam name="T1">Source type</typeparam>
    /// <typeparam name="T2">target type</typeparam>
    public interface IBinding<out T1, out T2>
    {
        /// <summary>
        /// The source fo this binding (final object in path that
        /// the binding attaches to and uses some of its property to provide the new value)
        /// </summary>
        MediaPoint.MVVM.Helpers.WeakReference<ViewModel> Source { get; set; }
        /// <summary>
        /// Constructor for a simple binding X.Y -> A.B which gives B as path
        /// </summary>
        /// <param name="path">Just the string of the property to bind to (B in this case)</param>
        string Path { get; }
        /// <summary>
        /// Disposes the binding and releases resources and event wrappers
        /// </summary>
        void Dispose();
        /// <summary>
        /// Happens when the property we bind to changes
        /// </summary>
        /// <param name="sender">The object we have bound to (penultimate object from the path)</param>
        /// <param name="e">Property data we have bound to</param>
        void SourcePropertyChanged(object sender, PropertyChangedEventArgs e);
        /// <summary>
        /// Happens whenever on object in middle of the path of the binding changes
        /// </summary>
        /// <param name="sender">The object that changed</param>
        /// <param name="e">PropertyChanged data</param>
        void PathObjectChanged(object sender, PropertyChangedEventArgs e);
    }

    /// <summary>
    /// Specifies a generic covariant weak event wrapper that does not hold strong references to objects it notifies
    /// </summary>
    /// <typeparam name="T1">Type 1 : covariant</typeparam>
    /// <typeparam name="T2">Type 2 : covariant</typeparam>
    public interface IEventWrapper<out T1, out T2>
    {
        /// <summary>
        /// Used only in event wrappers for in middle path objects ( for A.B.C.D this means A.B.C )
        /// </summary>
        string PropertyFilter { get; }
        /// <summary>
        /// Object we will listen for changes
        /// </summary>
        ViewModel EventSource { get; }
        /// <summary>
        /// Weak reference to the parent Binding
        /// </summary>
        WeakReference WRef { get; }
        /// <summary>
        /// Detaches this eventwrapper to be able to GC it later
        /// </summary>
        /// <param name="pathDetach">True if in middle path (just for debugging, doesnt matter which value is passed)</param>        
        void Detach(bool inPath = false);
    }

    #endregion

    #region "Classes"

    /// <summary>
    /// Class which wraps and event and makes it WEAK.
    /// This means that when the object which contains parent binding goes out of scope, this class won't hold onto it
    /// </summary>
    /// <typeparam name="SOURCE">Object type we will listen for changes</typeparam>
    /// <typeparam name="TARGET">Object type we will be updating</typeparam>
    public sealed class EventWrapper<SOURCE, TARGET> : IEventWrapper<SOURCE, TARGET>
        where SOURCE : ViewModel
        where TARGET : ViewModel
    {
        /// <summary>
        /// Used only in event wrappers for in middle path objects ( for A.B.C.D this means A.B.C )
        /// </summary>
        public string PropertyFilter { get; private set; }
        /// <summary>
        /// Object we will listen for changes
        /// </summary>
        public ViewModel EventSource { get; private set; }
        /// <summary>
        /// Weak reference to the parent Binding
        /// </summary>
        public WeakReference WRef { get; private set; }

        /// <summary>
        /// Creates an event wrapper which notifies the parent binding when a property changes
        /// (and filters the name in case it is a wrapper for a middle path object)
        /// </summary>
        /// <param name="eventSource">The final object in path which we listen to its properties</param>
        /// <param name="binding">Parent binding</param>
        /// <param name="propName">Only for a middle path object: property name we listen</param>
        /// <param name="middlePath">True if in middle of a binding path</param>
        public EventWrapper(ViewModel eventSource,
                            IBinding<SOURCE, TARGET> binding,
                            string propName = null,
                            bool middlePath = false)
        {
            if (eventSource == null) throw new ArgumentNullException("eventSource");
            this.EventSource = eventSource;
            this.WRef = new WeakReference(binding);

            // ? an event wrapper for an object in middle of the path (not the final one)
            if (middlePath)
            {
                PropertyFilter = propName;
                eventSource.PropertyChanged += PathPropertyChanged;
                Debug.WriteLine("Path attach: " + eventSource.GetType().Name + ":" + eventSource.GetHashCode() + "->" + propName);
            }
            else
            {
                eventSource.PropertyChanged += SourcePropertyChanged;
                Debug.WriteLine("Final attach: " + eventSource.GetType().Name);
            }

        }

        /// <summary>
        /// In a path we care only about a specific property change
        /// </summary>
        /// <param name="sender">Middle path object were some property changes</param>
        /// <param name="e">Property data</param>
        public void PathPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // in a path we care only about a specific property change
            if (e.PropertyName == PropertyFilter)
            {
                IBinding<SOURCE, TARGET> binding = (IBinding<SOURCE, TARGET>)WRef.Target;
                if (binding != null)
                    binding.PathObjectChanged(sender, e);
                else
                    Detach(true);
            }
        }

        /// <summary>
        /// Notifies the parent binding about a change in the final property we are attaching to
        /// </summary>
        /// <param name="sender">Final object we have bound to</param>
        /// <param name="e">Property data</param>
        [DebuggerStepThrough]
        public void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // notify binding - it takes care of filtering the property name
            IBinding<SOURCE, TARGET> binding = (IBinding<SOURCE, TARGET>)WRef.Target;
            if (binding != null)
                binding.SourcePropertyChanged(sender, e);
            else
                Detach();
        }

        /// <summary>
        /// Detaches this eventwrapper to be able to GC it later
        /// </summary>
        /// <param name="pathDetach">True if in middle path (just for debugging, doesnt matter which value is passed)</param>
        public void Detach(bool pathDetach = false)
        {
            if (PropertyFilter != null)
                EventSource.PropertyChanged -= SourcePropertyChanged;
            else
                EventSource.PropertyChanged -= PathPropertyChanged;
#if DEBUG
            if (pathDetach)
                Debug.WriteLine("Path detach: " + EventSource.GetType().Name + ":" + EventSource.GetHashCode() + " from " + PropertyFilter);
            else
                Debug.WriteLine("Final detach: " + EventSource.GetType().Name + ":" + EventSource.GetHashCode());
#endif
        }
    }

    /// <summary>
    /// On object that represents a binding between a source and a target property on two objects
    /// </summary>
    /// <typeparam name="SOURCE">Object we will listen for changes (INotifyPropertyChanged)</typeparam>
    /// <typeparam name="TARGET">Object we will be updating (INotifyPropertyChanged)</typeparam>
    public class Binder<SOURCE, TARGET> : IBinding<SOURCE, TARGET>
        where SOURCE : ViewModel
        where TARGET : ViewModel
    {
        private MediaPoint.MVVM.Helpers.WeakReference<ViewModel> root;
        private MediaPoint.MVVM.Helpers.WeakReference<ViewModel> source;
        private MediaPoint.MVVM.Helpers.WeakReference<TARGET> target;
        private string srcProperty;
        private string targetProperty;
        private List<string> fullPath;
        private List<IEventWrapper<ViewModel, ViewModel>> eventWrappers = new List<IEventWrapper<ViewModel, ViewModel>>();

        /// <summary>
        /// Extracts an object at specified position in a path of properties starting from the start object
        /// </summary>
        /// <param name="start">Starting object</param>
        /// <param name="path">Path of properties to follow</param>
        /// <param name="stopAt">Index to stop at but counted from the back side (So for 3 properties A.B.C, giving 3 will provide the value of A)</param>
        /// <returns>Object in path position</returns>
        public static object GetPathObject(object start, List<string> path, int stopAt = 0)
        {
            if (path.Count <= stopAt) return start;

            start = start.GetType().GetProperty(path[0]).GetValue(start, null);
            path.RemoveAt(0);
            if (start == null) return null;

            return GetPathObject(start, path, stopAt);
        }

        /// <summary>
        /// The source fo this binding (final object in path that
        /// the binding attaches to and uses some of its property to provide the new value)
        /// </summary>
        public MediaPoint.MVVM.Helpers.WeakReference<ViewModel> Source
        {
            get { return source; }
            set { source = value; }
        }

        /// <summary>
        /// Constructor for a simple binding X.Y -> A.B which gives B as path
        /// </summary>
        /// <param name="path">Just the string of the property to bind to (B in this case)</param>
        public Binder(List<string> path)
        {
            fullPath = new List<string>(path);
        }

        /// <summary>
        /// Constructor for a deep binding X.Y -> A.B.C...D which gives B.C...D as path
        /// </summary>
        /// <param name="root">Root object where the path starts from (A)</param>
        /// <param name="path">List of properties in path (B.C...D)</param>
        public Binder(ViewModel root, List<string> path)
            : this(path)
        {
            this.root = new MediaPoint.MVVM.Helpers.WeakReference<ViewModel>(root);
        }

        /// <summary>
        /// String representation of the binding
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (source == null && target == null)
            {
                return "Path: '" + String.Join(".", fullPath.ToArray()) + "' - NULL on both sides.";
            }
            else if (source == null)
            {
				return "Path: '" + String.Join(".", fullPath.ToArray()) + "', Object connection: NULL -> '" + target.Target.GetType().Name + "." + srcProperty + "'";
            }
            else if (target == null)
            {
				return "Path: '" + String.Join(".", fullPath.ToArray()) + "', Object connection: '" + source.Target.GetType().Name + "." + srcProperty + " -> NULL";
            }
            else if (target.IsAlive)
            {
				return "Path: '" + String.Join(".", fullPath.ToArray()) + "', Object connection: '" + source.Target.GetType().Name + "." + srcProperty + " -> " + target.Target.GetType().Name + "." + targetProperty + "'";
            }
            else if (source.IsAlive)
            {
				return "Path: '" + String.Join(".", fullPath.ToArray()) + "', Object connection: '" + source.Target.GetType().Name + "." + srcProperty + " -> Unbound";
            }
            else
            {
				return "Path: '" + String.Join(".", fullPath.ToArray()) + "' : both sides unbound.";
            }
        }

        /// <summary>
        /// Event wrappers exposed for the updating logic when some part of the path changes
        /// </summary>
        public List<IEventWrapper<ViewModel, ViewModel>> EventWrappers
        {
            get { return eventWrappers; }
        }

        /// <summary>
        /// Binds this binding (actually connects objects)
        /// </summary>
        /// <param name="source">The object that we will listen to for changes</param>
        /// <param name="target">The object we will be updating</param>
        /// <param name="srcProperty">The property on the source we will listen to</param>
        /// <param name="targetProperty">The property on target we will be updating</param>
        public void Bind(ViewModel source, TARGET target, string srcProperty, string targetProperty)
        {
            this.source = source == null ? null : new MediaPoint.MVVM.Helpers.WeakReference<ViewModel>(source);
            this.target = new MediaPoint.MVVM.Helpers.WeakReference<TARGET>(target);
            this.srcProperty = srcProperty;
            this.targetProperty = targetProperty;

            Debug.Assert(this.srcProperty != null);
            Debug.Assert(this.targetProperty != null);
            Debug.Assert(this.target != null);

            // the ending eventwrapper to the target object and the right property
            if (source != null)
                eventWrappers.Add(new EventWrapper<ViewModel, ViewModel>((ViewModel)source, this));

        }

        /// <summary>
        /// Happens whenever on object in middle of the path of the binding changes
        /// </summary>
        /// <param name="sender">The object that changed</param>
        /// <param name="e">PropertyChanged data</param>
        public void PathObjectChanged(object sender, PropertyChangedEventArgs e)
        {
            bool sourceSet = false; // we set the source to the penultimate object in path and only once

            // eventwrapper position for this change
            var index = eventWrappers.IndexOf(eventWrappers.First(ew => ew.PropertyFilter == e.PropertyName && ew.EventSource == sender));

            // remove all after it
            while (eventWrappers.Count > index)
            {
                var ev = eventWrappers[eventWrappers.Count - 1];
                ev.Detach(ev.PropertyFilter != null);
                eventWrappers.RemoveAt(eventWrappers.Count - 1);
            }

            // first occurence in path up to the end
            for (int i = fullPath.FindIndex(m => m == e.PropertyName); i < fullPath.Count; i++)
            {
                var tmpPath = new List<string>(fullPath);
                string propertyName = tmpPath[i];
                var objAtPathPos = (ViewModel)GetPathObject(root.Target, tmpPath, tmpPath.Count - i);
                if (objAtPathPos == null) break;

                if (i == fullPath.Count - 1)
                {
                    // the last one (not a pathChange eventwrapper but final one)
                    eventWrappers.Add(new EventWrapper<ViewModel, ViewModel>(objAtPathPos, this));
                }
                else
                {
                    // deep bindings change the source of this binding when something in the hierarchy changes
                    if (!sourceSet)
                    {
                        this.Source = new MediaPoint.MVVM.Helpers.WeakReference<ViewModel>((ViewModel)GetPathObject(root.Target, new List<string>(fullPath), 1));
                        sourceSet = true;
                    }
                    // middle path eventwrapper
                    eventWrappers.Add(new EventWrapper<ViewModel, ViewModel>(objAtPathPos, this, propertyName, true));
                }
            }
        }

        /// <summary>
        /// Happens when the property we bind to changes
        /// </summary>
        /// <param name="sender">The object we have bound to (penultimate object from the path)</param>
        /// <param name="e">Property data we have bound to</param>
        [DebuggerStepThrough]
        public void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            if (e.PropertyName == srcProperty)
            {
                if (target.IsAlive && source.IsAlive)
                {
                    Debug.WriteLine(sender.GetType().Name + ":" + sender.GetHashCode() + " Changed: " + target.Target.GetType().Name + "->" + e.PropertyName);

					try
					{
						object val = sender.GetType().GetProperty(srcProperty).GetValue(sender, null);
						if (typeof(TARGET).GetProperty(targetProperty).GetValue(target.Target, null) != val)
							typeof(TARGET).GetProperty(targetProperty).SetValue(target.Target, val, null);
					}
					catch (Exception ex) 
					{
						Debug.WriteLine("Binding setting value failed: " + ex.Message);
					}
                }
            }
        }

        /// <summary>
        /// Clears all event wrappers and disposes the binding
        /// </summary>
        public void Dispose()
        {
            foreach (var ew in eventWrappers)
            {
                ew.Detach(ew.PropertyFilter != null);
            }

            // its not possible to access managed properties from a finalizer, they might be discarded already
            // Debug.WriteLine("Disposed binding: " + this.GetHashCode() + " (" + Path + ")");
        }

        /// <summary>
        /// The full path of this binding
        /// </summary>
        public string Path
        {
            get { return this.ToString(); }
        }
    }

    #endregion

}
