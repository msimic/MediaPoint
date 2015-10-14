using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interactivity;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MediaPoint.Controls.Extensions;
using System.Windows.Controls;
using MediaPoint.MVVM.Services;
using MediaPoint.VM.ViewInterfaces;
using MediaPoint.App.AttachedProperties;

namespace MediaPoint.App.Behaviors
{

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
            AssociatedObject.AddHandler(FrameworkElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(AssociatedObject_MouseLeftButtonDown), true);
            AssociatedObject.AddHandler(FrameworkElement.PreviewMouseMoveEvent, new MouseEventHandler(AssociatedObject_MouseMove), true);
            AssociatedObject.AddHandler(FrameworkElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(AssociatedObject_MouseLeftButtonUp), true);
            AssociatedObject.PreviewMouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            //AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            base.OnAttached();
        }

		/// <summary>
		/// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
		/// </summary>
		/// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
		protected override void OnDetaching()
		{
			if (AssociatedObject == null) return;
            //AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            //AssociatedObject.PreviewMouseMove -= new MouseEventHandler(AssociatedObject_MouseMove);
            //AssociatedObject.PreviewMouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            ////AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.RemoveHandler(FrameworkElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(AssociatedObject_MouseLeftButtonDown));
            AssociatedObject.RemoveHandler(FrameworkElement.PreviewMouseMoveEvent, new MouseEventHandler(AssociatedObject_MouseMove));
            AssociatedObject.RemoveHandler(FrameworkElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(AssociatedObject_MouseLeftButtonUp));
            
			base.OnDetaching();

		}

		#endregion

		#region Event Handlers

		bool _isMouseDown = false;
		bool _wasDragging = false;
        private Point startPoint;

		void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (AssociatedObject == null) return;
			_isMouseDown = true;
            _wasDragging = false;
            startPoint = e.GetPosition(AssociatedObject);
		}

		void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
		{
            if (startPoint.X == 0 && startPoint.Y == 0) return;

			var wnd = AssociatedObject.TryFindParent<Window>();
			if (IsDraggable && wnd != null)
			{
                //if (e.LeftButton == MouseButtonState.Pressed)
                //{
                //    //e.Handled = true;
                //    _wasDragging = true;
                //    _isMouseDown = false;
                //    wnd.DragMove();
                //}
                var currentPoint = e.GetPosition(AssociatedObject);
                if (!_wasDragging && e.LeftButton == MouseButtonState.Pressed &&
                    Mouse.Captured == null &&
                    (Math.Abs(currentPoint.X - startPoint.X) >
                        SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPoint.Y - startPoint.Y) >
                        SystemParameters.MinimumVerticalDragDistance))
                {
                    var element = e.OriginalSource as FrameworkElement;
                    var isInput = element.GetValue(ThreatAsInputControl.TreatProperty);
                    if (isInput != DependencyProperty.UnsetValue &&
                        (bool)isInput != false)
                    {
                        _wasDragging = true;
                        // Prevent Click from firing
                        //AssociatedObject.CaptureMouse();
                        wnd.DragMove();
                    }

                }
			}
		}

		void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
            var wnd = AssociatedObject.TryFindParent<Window>();
            if (wnd != null)
            {
                wnd.ReleaseMouseCapture();
            }
            if (_wasDragging)
            {
                ServiceLocator.GetService<IMainView>().NotifyDragged();
                AssociatedObject.ReleaseMouseCapture();
                _wasDragging = false;
            }
            
            _isMouseDown = false;
		}
		#endregion

	}   // class

}   // namespace
