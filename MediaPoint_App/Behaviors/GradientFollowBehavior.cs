using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media;

namespace MediaPoint.App.Behaviors
{
    public enum GradientFollowDirection
    {
        None,
        Horizontal,
        Vertical,
        Both
    }

    public class GradientFollowBehavior : Behavior<FrameworkElement>
    {
        private FrameworkElement m_attachedObject;

        #region FollowDirection
        public static readonly DependencyProperty FollowDirectionProperty =
            DependencyProperty.Register("FollowDirection", typeof(GradientFollowDirection), typeof(GradientFollowBehavior),
                new FrameworkPropertyMetadata(GradientFollowDirection.Horizontal,
                    FrameworkPropertyMetadataOptions.None));

        public GradientFollowDirection FollowDirection
        {
            get { return (GradientFollowDirection)GetValue(FollowDirectionProperty); }
            set { SetValue(FollowDirectionProperty, value); }
        }
        #endregion
        
        protected override void OnAttached()
        {
            m_attachedObject = AssociatedObject;
            SetupEventHooks();
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            RemoveEventHooks();
            m_attachedObject = null;
            base.OnDetaching();
        }

        private void SetupEventHooks()
        {
            if(m_attachedObject != null)
                m_attachedObject.PreviewMouseMove += AttachedObject_PreviewMouseMove;
        }

        private void RemoveEventHooks()
        {
            if (m_attachedObject != null)
                m_attachedObject.PreviewMouseMove -= AttachedObject_PreviewMouseMove;
        }

        private void AttachedObject_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (FollowDirection == GradientFollowDirection.None)
                return;

            PropertyInfo info = FindFillProperty(m_attachedObject);

            if (info == null)
                return;

            var currentBrush = info.GetValue(m_attachedObject, null) as Brush;

            if(!(currentBrush is RadialGradientBrush)) 
                return;

            var gradient = currentBrush as RadialGradientBrush;

            var center = e.GetPosition(m_attachedObject);

            if (gradient.IsFrozen)
                gradient = gradient.Clone();

            if(FollowDirection == GradientFollowDirection.Horizontal)
                center.Y = gradient.Center.Y;

            if(FollowDirection == GradientFollowDirection.Vertical)
                center.X = gradient.Center.X;

            gradient.Center = center;
            gradient.GradientOrigin = center;

            info.SetValue(m_attachedObject, gradient, null);
        }

        /// <summary>
        /// Searches for a property on DependencyObject to set a Brush to
        /// </summary>
        /// <param name="obj">The DependencyObject to search</param>
        /// <returns></returns>
        private static PropertyInfo FindFillProperty(DependencyObject obj)
        {
            Type t = obj.GetType();

            PropertyInfo info = t.GetProperty("Background") ?? t.GetProperty("Fill");

            return info;
        }
    }
}
