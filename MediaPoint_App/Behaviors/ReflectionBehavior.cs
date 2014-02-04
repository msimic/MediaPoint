using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace MediaPoint.App.Behaviors
{
    public class ReflectionBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            ReplaceControlWithProxyGrid();

            AddReflectionEffect();
        }

        private Border _reflectionContainer;

        private void ReplaceControlWithProxyGrid()
        {
            DependencyObject associatedObjectParent = AssociatedObject.Parent;

            if (associatedObjectParent is Panel)
            {
                Grid gridProxy = new Grid();
                _reflectionContainer = new Border();

                Panel parentPanel = (Panel)associatedObjectParent;
                int indexOfAssociatedObject = parentPanel.Children.IndexOf(AssociatedObject);
                parentPanel.Children.RemoveAt(indexOfAssociatedObject);
                gridProxy.Children.Add(AssociatedObject);
                gridProxy.Children.Add(_reflectionContainer);
                parentPanel.Children.Insert(indexOfAssociatedObject, gridProxy);

                MoveGridProperties(AssociatedObject, gridProxy);
            }
            else if (associatedObjectParent is ContentControl)
            {
                Grid gridProxy = new Grid();
                _reflectionContainer = new Border();

                ContentControl parentContentControl = (ContentControl)associatedObjectParent;
                parentContentControl.Content = null;
                gridProxy.Children.Add(AssociatedObject);
                gridProxy.Children.Add(_reflectionContainer);
                parentContentControl.Content = gridProxy;

                MoveGridProperties(AssociatedObject, gridProxy);
            }
            else
            {
                throw new NotImplementedException(string.Format("The ReflectionBehavior doesn't support {0} as a parent for the element to reflect", associatedObjectParent.GetType().ToString()));
            }
        }

        private void MoveGridProperties(DependencyObject sourceObject, DependencyObject targetObject)
        {
            // move grid attached properties from control to proxy
            MoveProperty(sourceObject, targetObject, Grid.ColumnProperty);
            MoveProperty(sourceObject, targetObject, Grid.ColumnSpanProperty);
            MoveProperty(sourceObject, targetObject, Grid.RowProperty);
            MoveProperty(sourceObject, targetObject, Grid.RowSpanProperty);
            MoveProperty(sourceObject, targetObject, Grid.IsSharedSizeScopeProperty);
        }

        /// <summary>
        /// Moves dependency property value from a source to target, if exists.
        /// </summary>
        /// <param name="sourceObject">The source object.</param>
        /// <param name="targetObject">The target object.</param>
        /// <param name="property">The property to copy.</param>
        private static void MoveProperty(DependencyObject sourceObject, DependencyObject targetObject, DependencyProperty property)
        {
            // get value from source object
            object propertyValue = sourceObject.ReadLocalValue(property);

            // if property value exists
            if (propertyValue != DependencyProperty.UnsetValue)
            {
                // copy value on target object
                targetObject.SetValue(property, propertyValue);

                // remove value from source object
                sourceObject.ClearValue(property);
            }
        }

        private void AddReflectionEffect()
        {
            // set reflection container height and width 
            _reflectionContainer.SetBinding(FrameworkElement.HeightProperty, new Binding("ActualHeight") { Source = AssociatedObject });
            _reflectionContainer.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth") { Source = AssociatedObject });

            // set reflection transparency effect
            LinearGradientBrush opacityBrush = new LinearGradientBrush()
            {
                StartPoint = new Point(1, 0),
            };
            opacityBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(128, 0, 0, 0), Offset = 0 });
            opacityBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0, 0, 0, 0), Offset = 0.8 });
            opacityBrush.GradientStops.Add(new GradientStop() { Color = Color.FromArgb(0, 0, 0, 0), Offset = 1 });
            _reflectionContainer.OpacityMask = opacityBrush;

            // set reflection effect
            VisualBrush visualBrush = new VisualBrush();
            visualBrush.Visual = AssociatedObject;
            visualBrush.AutoLayoutContent = false;
            visualBrush.Stretch = Stretch.None;
            TransformGroup transformGroup = new TransformGroup();
            ScaleTransform scaleTransform = new ScaleTransform(1, -1, 0, 1);
            TranslateTransform translateTransform = new TranslateTransform(0, -1);
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);
            visualBrush.RelativeTransform = transformGroup;
            _reflectionContainer.Background = visualBrush;

            // move reflection effect to the bottom of the control
            TranslateTransform renderTranslateTransform = new TranslateTransform();
            BindingOperations.SetBinding(renderTranslateTransform, TranslateTransform.YProperty, new Binding("ActualHeight") { Source = AssociatedObject });
            _reflectionContainer.RenderTransform = renderTranslateTransform;
        }

    }
}
