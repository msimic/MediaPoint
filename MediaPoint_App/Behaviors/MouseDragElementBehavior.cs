using MediaPoint.Common.Helpers;
using Microsoft.Expression.Interactivity.Core;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Media;
namespace MediaPoint.App.Behaviors
{
    public class MouseDragElementBehavior : Behavior<FrameworkElement>
    {
        private bool settingPosition;
        private Point lastPoint;
        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(double), typeof(MouseDragElementBehavior), new PropertyMetadata(double.NaN, new PropertyChangedCallback(MouseDragElementBehavior.OnXChanged)));
        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(double), typeof(MouseDragElementBehavior), new PropertyMetadata(double.NaN, new PropertyChangedCallback(MouseDragElementBehavior.OnYChanged)));
        public static readonly DependencyProperty ConstrainToParentBoundsProperty = DependencyProperty.Register("ConstrainToParentBounds", typeof(bool), typeof(MouseDragElementBehavior), new PropertyMetadata(false, new PropertyChangedCallback(MouseDragElementBehavior.OnConstrainToParentBoundsChanged)));
        //public event MouseEventHandler DragBegun
        //{
        //    [MethodImpl(MethodImplOptions.Synchronized)]
        //    add
        //    {
        //        this.DragBegun = (MouseEventHandler)Delegate.Combine(this.DragBegun, value);
        //    }
        //    [MethodImpl(MethodImplOptions.Synchronized)]
        //    remove
        //    {
        //        this.DragBegun = (MouseEventHandler)Delegate.Remove(this.DragBegun, value);
        //    }
        //}
        //public event MouseEventHandler Dragging
        //{
        //    [MethodImpl(MethodImplOptions.Synchronized)]
        //    add
        //    {
        //        this.Dragging = (MouseEventHandler)Delegate.Combine(this.Dragging, value);
        //    }
        //    [MethodImpl(MethodImplOptions.Synchronized)]
        //    remove
        //    {
        //        this.Dragging = (MouseEventHandler)Delegate.Remove(this.Dragging, value);
        //    }
        //}
        //public event MouseEventHandler DragFinished
        //{
        //    [MethodImpl(MethodImplOptions.Synchronized)]
        //    add
        //    {
        //        this.DragFinished = (MouseEventHandler)Delegate.Combine(this.DragFinished, value);
        //    }
        //    [MethodImpl(MethodImplOptions.Synchronized)]
        //    remove
        //    {
        //        this.DragFinished = (MouseEventHandler)Delegate.Remove(this.DragFinished, value);
        //    }
        //}
        public double X
        {
            get
            {
                return (double)base.GetValue(MouseDragElementBehavior.XProperty);
            }
            set
            {
                base.SetValue(MouseDragElementBehavior.XProperty, value);
            }
        }
        public double Y
        {
            get
            {
                return (double)base.GetValue(MouseDragElementBehavior.YProperty);
            }
            set
            {
                base.SetValue(MouseDragElementBehavior.YProperty, value);
            }
        }
        public bool ConstrainToParentBounds
        {
            get
            {
                return (bool)base.GetValue(MouseDragElementBehavior.ConstrainToParentBoundsProperty);
            }
            set
            {
                base.SetValue(MouseDragElementBehavior.ConstrainToParentBoundsProperty, value);
            }
        }
        private Point ActualPosition
        {
            get
            {
                GeneralTransform transform = base.AssociatedObject.TransformToVisual(this.RootElement);
                Point transformOffset = MouseDragElementBehavior.GetTransformOffset(transform);
                return new Point(transformOffset.X, transformOffset.Y);
            }
        }
        internal static Rect GetLayoutRect(FrameworkElement element)
        {
            double num = element.ActualWidth;
            double num2 = element.ActualHeight;
            if (element is Image || element is MediaElement)
            {
                if (element.Parent.GetType() == typeof(Canvas))
                {
                    num = (double.IsNaN(element.Width) ? num : element.Width);
                    num2 = (double.IsNaN(element.Height) ? num2 : element.Height);
                }
                else
                {
                    num = element.RenderSize.Width;
                    num2 = element.RenderSize.Height;
                }
            }
            num = ((element.Visibility == Visibility.Collapsed) ? 0.0 : num);
            num2 = ((element.Visibility == Visibility.Collapsed) ? 0.0 : num2);
            Thickness margin = element.Margin;
            Rect layoutSlot = LayoutInformation.GetLayoutSlot(element);
            double x = 0.0;
            double y = 0.0;
            switch (element.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    x = layoutSlot.Left + margin.Left;
                    break;
                case HorizontalAlignment.Center:
                    x = (layoutSlot.Left + margin.Left + layoutSlot.Right - margin.Right) / 2.0 - num / 2.0;
                    break;
                case HorizontalAlignment.Right:
                    x = layoutSlot.Right - margin.Right - num;
                    break;
                case HorizontalAlignment.Stretch:
                    x = Math.Max(layoutSlot.Left + margin.Left, (layoutSlot.Left + margin.Left + layoutSlot.Right - margin.Right) / 2.0 - num / 2.0);
                    break;
            }
            switch (element.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    y = layoutSlot.Top + margin.Top;
                    break;
                case VerticalAlignment.Center:
                    y = (layoutSlot.Top + margin.Top + layoutSlot.Bottom - margin.Bottom) / 2.0 - num2 / 2.0;
                    break;
                case VerticalAlignment.Bottom:
                    y = layoutSlot.Bottom - margin.Bottom - num2;
                    break;
                case VerticalAlignment.Stretch:
                    y = Math.Max(layoutSlot.Top + margin.Top, (layoutSlot.Top + margin.Top + layoutSlot.Bottom - margin.Bottom) / 2.0 - num2 / 2.0);
                    break;
            }
            return new Rect(x, y, num, num2);
        }
        private Rect ElementBounds
        {
            get
            {
                Rect layoutRect = GetLayoutRect(base.AssociatedObject);
                return new Rect(new Point(0.0, 0.0), new Size(layoutRect.Width, layoutRect.Height));
            }
        }
        private FrameworkElement ParentElement
        {
            get
            {
                return base.AssociatedObject.Parent as FrameworkElement;
            }
        }
        private UIElement RootElement
        {
            get
            {
                DependencyObject dependencyObject = base.AssociatedObject;
                for (DependencyObject dependencyObject2 = dependencyObject; dependencyObject2 != null; dependencyObject2 = VisualTreeHelper.GetParent(dependencyObject))
                {
                    dependencyObject = dependencyObject2;
                }
                return dependencyObject as UIElement;
            }
        }
        private static void OnXChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            MouseDragElementBehavior mouseDragElementBehavior = (MouseDragElementBehavior)sender;
            mouseDragElementBehavior.UpdatePosition(new Point((double)args.NewValue, mouseDragElementBehavior.Y));
        }
        private static void OnYChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            MouseDragElementBehavior mouseDragElementBehavior = (MouseDragElementBehavior)sender;
            mouseDragElementBehavior.UpdatePosition(new Point(mouseDragElementBehavior.X, (double)args.NewValue));
        }
        private static void OnConstrainToParentBoundsChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            MouseDragElementBehavior mouseDragElementBehavior = (MouseDragElementBehavior)sender;
            mouseDragElementBehavior.UpdatePosition(new Point(mouseDragElementBehavior.X, mouseDragElementBehavior.Y));
        }
        private void UpdatePosition(Point point)
        {
            if (!this.settingPosition && base.AssociatedObject != null)
            {
                GeneralTransform transform = base.AssociatedObject.TransformToVisual(this.RootElement);
                Point transformOffset = MouseDragElementBehavior.GetTransformOffset(transform);
                double x = double.IsNaN(point.X) ? 0.0 : (point.X - transformOffset.X);
                double y = double.IsNaN(point.Y) ? 0.0 : (point.Y - transformOffset.Y);
                this.ApplyTranslation(x, y);
            }
        }
        private void ApplyTranslation(double x, double y)
        {
            if (this.ParentElement != null)
            {
                GeneralTransform transform = this.RootElement.TransformToVisual(this.ParentElement);
                Point point = MouseDragElementBehavior.TransformAsVector(transform, x, y);
                x = point.X;
                y = point.Y;
                if (this.ConstrainToParentBounds)
                {
                    FrameworkElement parentElement = this.ParentElement;
                    Rect rect = new Rect(0.0, 0.0, parentElement.ActualWidth, parentElement.ActualHeight);
                    GeneralTransform generalTransform = base.AssociatedObject.TransformToVisual(parentElement);
                    Rect rect2 = this.ElementBounds;
                    rect2 = generalTransform.TransformBounds(rect2);
                    Rect rect3 = rect2;
                    rect3.X += x;
                    rect3.Y += y;
                    if (!MouseDragElementBehavior.RectContainsRect(rect, rect3))
                    {
                        if (rect3.X < rect.Left)
                        {
                            double num = rect3.X - rect.Left;
                            x -= num;
                        }
                        else
                        {
                            if (rect3.Right > rect.Right)
                            {
                                double num2 = rect3.Right - rect.Right;
                                x -= num2;
                            }
                        }
                        if (rect3.Y < rect.Top)
                        {
                            double num3 = rect3.Y - rect.Top;
                            y -= num3;
                        }
                        else
                        {
                            if (rect3.Bottom > rect.Bottom)
                            {
                                double num4 = rect3.Bottom - rect.Bottom;
                                y -= num4;
                            }
                        }
                    }
                }
                this.ApplyTranslationTransform(x, y);
            }
        }
        private void ApplyTranslationTransform(double x, double y)
        {
            Transform renderTransform = base.AssociatedObject.RenderTransform;
            TransformGroup transformGroup = renderTransform as TransformGroup;
            MatrixTransform matrixTransform = renderTransform as MatrixTransform;
            TranslateTransform translateTransform = renderTransform as TranslateTransform;
            if (translateTransform == null)
            {
                if (transformGroup != null)
                {
                    if (transformGroup.Children.Count > 0)
                    {
                        translateTransform = (transformGroup.Children[transformGroup.Children.Count - 1] as TranslateTransform);
                    }
                    if (translateTransform == null)
                    {
                        translateTransform = new TranslateTransform();
                        transformGroup.Children.Add(translateTransform);
                    }
                }
                else
                {
                    if (matrixTransform != null)
                    {
                        Matrix matrix = matrixTransform.Matrix;
                        matrix.OffsetX += x;
                        matrix.OffsetY += y;
                        MatrixTransform matrixTransform2 = new MatrixTransform();
                        matrixTransform2.Matrix = matrix;
                        base.AssociatedObject.RenderTransform = matrixTransform2;
                        return;
                    }
                    TransformGroup transformGroup2 = new TransformGroup();
                    translateTransform = new TranslateTransform();
                    if (renderTransform != null)
                    {
                        transformGroup2.Children.Add(renderTransform);
                    }
                    transformGroup2.Children.Add(translateTransform);
                    base.AssociatedObject.RenderTransform = transformGroup2;
                }
            }
            translateTransform.X += x;
            translateTransform.Y += y;
        }
        private void UpdatePosition()
        {
            GeneralTransform transform = base.AssociatedObject.TransformToVisual(this.RootElement);
            Point transformOffset = MouseDragElementBehavior.GetTransformOffset(transform);
            this.X = transformOffset.X;
            this.Y = transformOffset.Y;
        }
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var w = VisualHelper.TryFindParent<Window>(AssociatedObject);
            if (w != Application.Current.MainWindow)
            {
                return;
            }
            this.lastPoint = e.GetPosition(RootElement); 
            base.AssociatedObject.CaptureMouse();
            base.AssociatedObject.MouseMove += new MouseEventHandler(this.OnMouseMove);

            //lastPoint.Offset(-e.GetPosition(AssociatedObject).X, -e.GetPosition(AssociatedObject).Y);
            e.Handled = true;
            base.AssociatedObject.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(this.OnMouseLeftButtonUp), false);
            //if (this.DragBegun != null)
            //{
            //    this.DragBegun(this, e);
            //}
        }
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            base.AssociatedObject.MouseMove -= new MouseEventHandler(this.OnMouseMove);
            base.AssociatedObject.ReleaseMouseCapture();
            e.Handled = true;
            base.AssociatedObject.RemoveHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(this.OnMouseLeftButtonUp));
            //if (this.DragFinished != null)
            //{
            //    this.DragFinished(this, e);
            //}
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(this.RootElement);
            double x = position.X - this.lastPoint.X;
            double y = position.Y - this.lastPoint.Y;
            this.lastPoint = position;
            //lastPoint.Offset(-e.GetPosition(AssociatedObject).X, -e.GetPosition(AssociatedObject).Y);
            
            if (this.ConstrainToParentBounds && !this.IsValidConstrainedMove(position))
            {
                return;
            }
            this.settingPosition = true;
            this.ApplyTranslation(x, y);
            this.UpdatePosition();
            this.settingPosition = false;
            //if (this.Dragging != null)
            //{
            //    this.Dragging(this, e);
            //}
        }
        private bool IsValidConstrainedMove(Point currentPosition)
        {
            return this.ElementBounds.Contains(this.RootElement.TransformToVisual(base.AssociatedObject).Transform(currentPosition));
        }
        private static bool RectContainsRect(Rect rect1, Rect rect2)
        {
            return !rect1.IsEmpty && !rect2.IsEmpty && (rect1.X <= rect2.X && rect1.Y <= rect2.Y && rect1.X + rect1.Width >= rect2.X + rect2.Width) && rect1.Y + rect1.Height >= rect2.Y + rect2.Height;
        }
        private static Point TransformAsVector(GeneralTransform transform, double x, double y)
        {
            Point point = transform.Transform(new Point(0.0, 0.0));
            Point point2 = transform.Transform(new Point(x, y));
            return new Point(point2.X - point.X, point2.Y - point.Y);
        }
        private static Point GetTransformOffset(GeneralTransform transform)
        {
            return transform.Transform(new Point(0.0, 0.0));
        }
        protected override void OnAttached()
        {
            base.AssociatedObject.AddHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(this.OnMouseLeftButtonDown), false);
        }
        protected override void OnDetaching()
        {
            base.AssociatedObject.RemoveHandler(UIElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler(this.OnMouseLeftButtonDown));
        }
    }
}
