using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interactivity;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;

namespace MediaPoint.Controls.Behaviors
{

	public static class Extensions
	{
		/// <summary>
		/// Finds a parent of a given item on the visual tree.
		/// </summary>
		/// <typeparam name="T">The type of the queried item.</typeparam>
		/// <param name="child">A direct or indirect child of the
		/// queried item.</param>
		/// <param name="mustBe">parent must be of this type</param>>
		/// <returns>The first parent item that matches the submitted
		/// type parameter. If not matching item can be found, a null
		/// reference is being returned.</returns>
		public static T TryFindParent<T>(this DependencyObject child, Type mustBe = null)
			where T : DependencyObject
		{
			//get parent item
			DependencyObject parentObject = GetParentObject(child);

			//we've reached the end of the tree
			if (parentObject == null) return null;

			//check if the parent matches the type we're looking for
			T parent = parentObject as T;
			if (parent != null && (mustBe == null || mustBe.IsAssignableFrom(parent.GetType())))
			{
				return parent;
			}
			else
			{
				//use recursion to proceed with next level
				return TryFindParent<T>(parentObject, mustBe);
			}
		}

		/// <summary>
		/// This method is an alternative to WPF's
		/// <see cref="VisualTreeHelper.GetParent"/> method, which also
		/// supports content elements. Keep in mind that for content element,
		/// this method falls back to the logical tree of the element!
		/// </summary>
		/// <param name="child">The item to be processed.</param>
		/// <returns>The submitted item's parent, if available. Otherwise
		/// null.</returns>
		public static DependencyObject GetParentObject(this DependencyObject child)
		{
			if (child == null) return null;

			//handle content elements separately
			ContentElement contentElement = child as ContentElement;
			if (contentElement != null)
			{
				DependencyObject parent = ContentOperations.GetParent(contentElement);
				if (parent != null) return parent;

				FrameworkContentElement fce = contentElement as FrameworkContentElement;
				return fce != null ? fce.Parent : null;
			}

			//also try searching for parent in framework elements (such as DockPanel, etc)
			FrameworkElement frameworkElement = child as FrameworkElement;
			if (frameworkElement != null)
			{
				DependencyObject parent = frameworkElement.Parent;
				if (parent != null) return parent;
			}

			//if it's not a ContentElement/FrameworkElement, rely on VisualTreeHelper
			return VisualTreeHelper.GetParent(child);
		}
	}

	/// <summary>
	/// A behavior that adds dragging
	/// </summary>
	public sealed class DraggableBehavior : Behavior<FrameworkElement>
	{

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="T:DraggableBehavior"/> class.
		/// </summary>
		public DraggableBehavior()
		{
		}

		#endregion

		#region Properties

		#region IsDraggable

		/// <summary>
		/// returns or sets if the window is draggable
		/// </summary>
		public bool IsDraggable
		{
			get
			{
				return (bool)GetValue(IsDraggableProperty);
			}
			set
			{
				SetValue(IsDraggableProperty, value);
			}
		}

		/// <summary>
		/// Dependency property for the <see cref="P:IsDraggableProperty"/> property.
		/// </summary>
		private static readonly DependencyProperty IsDraggableProperty = DependencyProperty.RegisterAttached("IsDraggable", typeof(bool), typeof(DraggableBehavior), new PropertyMetadata(default(bool)));

		/// <summary>
		/// Gets a value indicating whether or not the specified window is currently draggable.
		/// </summary>
		public static bool GetIsDraggable(Window window)
		{
			return (bool)window.GetValue(IsDraggableProperty);
		}

		/// <summary>
		/// Sets a value indicating whether or not the specified window is currently draggable.
		/// </summary>
		/// <param name="window">The window.</param>
		/// <param name="value">The value.</param>
		public static void SetIsDraggable(Window window, bool value)
		{
			window.SetValue(IsDraggableProperty, value);
		}

		#endregion

		#endregion

		#region Methods

		/// <summary>
		/// Called after the behavior is attached to an AssociatedObject.
		/// </summary>
		/// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
		protected override void OnAttached()
		{
			if (AssociatedObject == null) return;
			base.OnAttached();
			AssociatedObject.MouseLeftButtonDown += new MouseButtonEventHandler(AssociatedObject_MouseLeftButtonDown);
			AssociatedObject.MouseMove += new MouseEventHandler(AssociatedObject_MouseMove);
			AssociatedObject.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(AssociatedObject_MouseLeftButtonUp);

		}

		/// <summary>
		/// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
		/// </summary>
		/// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
		protected override void OnDetaching()
		{
			if (AssociatedObject == null) return;
			AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
			AssociatedObject.MouseMove -= new MouseEventHandler(AssociatedObject_MouseMove);
			AssociatedObject.PreviewMouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
			base.OnDetaching();

		}

		#endregion

		#region Event Handlers

		bool _isMouseDown = false;
		bool _wasDragging = false;

		void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (AssociatedObject == null) return;

			_isMouseDown = true;
		}

		void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
		{
			var wnd = AssociatedObject.TryFindParent<Window>();
			if (IsDraggable && wnd != null && _isMouseDown)
			{
				e.Handled = true;
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					_wasDragging = true;
					wnd.DragMove();
				}
			}
		}

		void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (AssociatedObject == null) return;

			if (_wasDragging)
			{
				e.Handled = true;
				_wasDragging = false;
			}
			_isMouseDown = false;
		}
		#endregion

	}   // class

}   // namespace
