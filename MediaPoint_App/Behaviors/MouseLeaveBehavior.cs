using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;
using System.ComponentModel;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using MediaPoint.Common.Helpers;

namespace MediaPoint.App.Behaviors
{
    public class MonitorMouseLeaveBehavior : Behavior<FrameworkElement>
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(UInt16 virtualKeyCode);

        private enum VK
        {
            LBUTTON = 0x01
        }

        private bool _tracking;
        private const int _interval = 1;
        private Timer _checkPosTimer = new Timer(_interval);
        private Dictionary<FrameworkElement, RoutedEventHandlerInfo[]> _leaveHandlersForElement = new Dictionary<FrameworkElement, RoutedEventHandlerInfo[]>();
        private Window _window;
        private Dictionary<FrameworkElement, Rect> _boundsByElement = new Dictionary<FrameworkElement, Rect>();
        private Dictionary<FrameworkElement, bool> _wasInside = new Dictionary<FrameworkElement, bool>();
        private List<FrameworkElement> _elements = new List<FrameworkElement>();


        /// <summary>
        /// If true, all subcontrols are monitored for the mouseleave event when left mousebutton is down.
        /// True by default.
        /// </summary>
        public bool MonitorSubControls { get { return (bool)GetValue(MonitorSubControlsProperty); } set { SetValue(MonitorSubControlsProperty, value); } }
        public static readonly DependencyProperty MonitorSubControlsProperty = DependencyProperty.Register("MonitorSubControls", typeof(bool), typeof(MonitorMouseLeaveBehavior), new PropertyMetadata(true, OnMonitorSubControlsChanged));

        private static void OnMonitorSubControlsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonitorMouseLeaveBehavior beh = (MonitorMouseLeaveBehavior)d;
            beh.AddOrRemoveLogicalChildren((bool)e.NewValue);
        }

        /// <summary>
        /// Initial actions
        /// </summary>
        protected override void OnAttached()
        {
            _window = this.AssociatedObject is Window ? (Window)this.AssociatedObject : Window.GetWindow(this.AssociatedObject); // get window
            _window.SourceInitialized += (s, e) =>
            {
                this.AddOrRemoveLogicalChildren(this.MonitorSubControls); // get all monitored elements
                this.AttachHandlers(true); // attach mousedown and sizechanged handlers
                this.GetAllBounds(); // determine bounds of all elements
                _checkPosTimer.Elapsed += (s1, e1) => Dispatcher.BeginInvoke((Action)(() => { CheckPosition(); }));
            };
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            this.AttachHandlers(false);
            base.OnDetaching();
        }

        /// <summary>
        /// Starts or stops monitoring of the AssociatedObject's logical children.
        /// </summary>
        /// <param name="add"></param>
        private void AddOrRemoveLogicalChildren(bool add)
        {
            if (_window != null && _window.IsInitialized)
            {
                AddOrRemoveSizeChangedHandlers(false);
                _elements.Clear();
                if (add)
                    _elements.AddRange(VisualHelper.FindLogicalChildren<FrameworkElement>(this.AssociatedObject));
                _elements.Add(this.AssociatedObject);
                AddOrRemoveSizeChangedHandlers(true);
            }
        }

        /// <summary>
        /// Attaches/detaches size changed handlers to the monitored elements
        /// </summary>
        /// <param name="add"></param>
        private void AddOrRemoveSizeChangedHandlers(bool add)
        {
            foreach (var element in _elements)
            {
                element.SizeChanged -= element_SizeChanged;
                if (add) element.SizeChanged += element_SizeChanged;
            }
        }

        /// <summary>
        /// Adjusts the stored bounds to the changed size
        /// </summary>
        void element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            if (fe != null)
                GetBounds(fe);
        }

        /// <summary>
        /// Attaches/Detaches MouseLeftButtonDown and SizeChanged handlers 
        /// </summary>
        /// <param name="attach">true: attach, false: detach</param>
        private void AttachHandlers(bool attach)
        {
            AddOrRemoveSizeChangedHandlers(attach);

            if (attach)
                _window.PreviewMouseLeftButtonDown += window_PreviewMouseLeftButtonDown;
            else // detach
                _window.PreviewMouseLeftButtonDown -= window_PreviewMouseLeftButtonDown;
        }

        /// <summary>
        /// Gets the bounds for all monitored elements
        /// </summary>
        private void GetAllBounds()
        {
            _boundsByElement.Clear();
            foreach (var element in _elements)
                GetBounds(element);
        }

        /// <summary>
        /// Gets the bounds of the control, which are used to check if the mouse position
        /// is located within. Note that this only covers rectangular control shapes.
        /// </summary>
        private void GetBounds(FrameworkElement element)
        {
            Point p1 = new Point(0, 0);
            Point p2 = new Point(element.ActualWidth, element.ActualHeight);
            p1 = element.TransformToVisual(_window).Transform(p1);
            p2 = element.TransformToVisual(_window).Transform(p2);

            if (element == _window) // window bounds need to account for the border
            {
                var titleHeight = SystemParameters.WindowCaptionHeight + 2 * SystemParameters.ResizeFrameHorizontalBorderHeight; //  not sure about that one
                var verticalBorderWidth = SystemParameters.ResizeFrameVerticalBorderWidth;
                p1.Offset(-verticalBorderWidth, -titleHeight);
                p2.Offset(-verticalBorderWidth, -titleHeight);
            }

            Rect bounds = new Rect(p1, p2);

            if (_boundsByElement.ContainsKey(element))
                _boundsByElement[element] = bounds;
            else
                _boundsByElement.Add(element, bounds);
        }

        /// <summary>
        /// For all monitored elements, detach the MouseLeave event handlers and store them locally,
        /// to be executed manually.
        /// </summary>
        private void RerouteLeaveHandlers()
        {
            foreach (var element in _elements)
            {
                if (!_leaveHandlersForElement.ContainsKey(element))
                {
                    var handlers = ReflectionHelper.GetRoutedEventHandlers(element, UIElement.MouseLeaveEvent);
                    if (handlers != null)
                    {
                        _leaveHandlersForElement.Add(element, handlers);
                        foreach (var handler in handlers)
                            element.MouseLeave -= (MouseEventHandler)handler.Handler; // detach handlers
                    }
                }
            }
        }

        /// <summary>
        /// Reattach all leave handlers that were detached in window_PreviewMouseLeftButtonDown.
        /// </summary>
        private void ReattachLeaveHandlers()
        {
            foreach (var kvp in _leaveHandlersForElement)
            {
                FrameworkElement fe = kvp.Key;
                foreach (var handler in kvp.Value)
                {
                    if (handler.Handler is MouseEventHandler)
                        fe.MouseLeave += (MouseEventHandler)handler.Handler;
                }
            }

            _leaveHandlersForElement.Clear();
        }

        /// <summary>
        /// Checks if the mouse position is inside the bounds of the elements
        /// If there is a transition from inside to outside, the leave event handlers are executed
        /// </summary>
        private void DetermineIsInside()
        {
            Point p = _window.PointFromScreen(GetMousePosition());
            foreach (var element in _elements)
            {
                if (_boundsByElement.ContainsKey(element))
                {
                    bool isInside = _boundsByElement[element].Contains(p);
                    bool wasInside = _wasInside.ContainsKey(element) && _wasInside[element];

                    if (wasInside && !isInside)
                        ExecuteLeaveHandlers(element);

                    if (_wasInside.ContainsKey(element))
                        _wasInside[element] = isInside;
                    else
                        _wasInside.Add(element, isInside);
                }
            }
        }

        /// <summary>
        /// Gets the mouse position relative to the screen
        /// </summary>
        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        /// <summary>
        /// Gets the mouse button state. MouseEventArgs.LeftButton is notoriously unreliable.
        /// </summary>
        private bool IsMouseLeftButtonPressed()
        {
            short leftMouseKeyState = GetAsyncKeyState((ushort)VK.LBUTTON);
            bool ispressed = leftMouseKeyState < 0;

            return ispressed;
        }

        /// <summary>
        /// Executes the leave handlers that were attached to the controls.
        /// They have been detached previously by this behavior (see window_PreviewMouseLeftButtonDown), to prevent double execution.
        /// After mouseup, they are reattached (see CheckPosition)
        /// </summary>
        private void ExecuteLeaveHandlers(FrameworkElement fe)
        {
            MouseDevice mouseDev = InputManager.Current.PrimaryMouseDevice;
            MouseEventArgs mouseEvent = new MouseEventArgs(mouseDev, 0) { RoutedEvent = Control.MouseLeaveEvent };

            if (_leaveHandlersForElement.ContainsKey(fe))
            {
                foreach (var handler in _leaveHandlersForElement[fe])
                {
                    if (handler.Handler is MouseEventHandler)
                        ((MouseEventHandler)handler.Handler).Invoke(fe, mouseEvent);
                }
            }
        }

        /// <summary>
        /// Sets the mouse capture (events outside the window are still directed to it),
        /// and tells the behavior to watch out for a missed leave event
        /// </summary>
        private void window_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("--- left mousebutton down ---"); // todo remove

            this.RerouteLeaveHandlers();
            _tracking = true;
            _checkPosTimer.Start();
        }

        /// <summary>
        /// Uses the _tracking field as well as left mouse button state to determine if either 
        /// leave event handlers should be executed, or monitoring should be stopped.
        /// </summary>
        private void CheckPosition()
        {
            if (_tracking)
            {
                if (IsMouseLeftButtonPressed())
                {
                    this.DetermineIsInside();
                }
                else
                {
                    _wasInside.Clear();
                    _tracking = false;
                    _checkPosTimer.Stop();
                    System.Diagnostics.Debug.WriteLine("--- left mousebutton up ---"); // todo remove

                    // invoking ReattachLeaveHandlers() immediately would rethrow MouseLeave for top grid/window 
                    // if both a) mouse is outside window and b) mouse moves. Wait with reattach until mouse is inside window again and moves.
                    _window.MouseMove += ReattachHandler;
                }
            }
        }

        /// <summary>
        /// Handles the first _window.MouseMove event after left mouse button was released,
        /// and reattaches the MouseLeaveHandlers. Detaches itself to be executed only once.
        /// </summary>
        private void ReattachHandler(object sender, MouseEventArgs e)
        {
            ReattachLeaveHandlers();
            _window.MouseMove -= ReattachHandler; // only once
        }
    }
}