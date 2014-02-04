#region File Header

// -------------------------------------------------------------------------------
// 
// This file is part of the WPFSpark project: http://wpfspark.codeplex.com/
//
// Author: Ratish Philip
// 
// WPFSpark v1.1
//
// -------------------------------------------------------------------------------

#endregion

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WPFSpark
{
    /// <summary>
    /// Implementation of the IPivotContent interface
    /// </summary>
    public class PivotContentControl : ContentControl, IPivotContent
    {
        #region Fields

        Storyboard fadeInSB;

        #endregion

        #region Dependency Properties

        #region AnimateContent

        /// <summary>
        /// AnimateContent Dependency Property
        /// </summary>
        public static readonly DependencyProperty AnimateContentProperty =
            DependencyProperty.Register("AnimateContent", typeof(bool), typeof(PivotContentControl),
                new FrameworkPropertyMetadata(true,
                    new PropertyChangedCallback(OnAnimateContentChanged)));

        /// <summary>
        /// Gets or sets the AnimateContent property. This dependency property 
        /// indicates whether the content should be animated upon activation.
        /// </summary>
        public bool AnimateContent
        {
            get { return (bool)GetValue(AnimateContentProperty); }
            set { SetValue(AnimateContentProperty, value); }
        }

        /// <summary>
        /// Handles changes to the AnimateContent property.
        /// </summary>
        /// <param name="d">PivotContentControl</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnAnimateContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotContentControl pcCtrl = (PivotContentControl)d;
            bool oldAnimateContent = (bool)e.OldValue;
            bool newAnimateContent = pcCtrl.AnimateContent;
            pcCtrl.OnAnimateContentChanged(oldAnimateContent, newAnimateContent);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the AnimateContent property.
        /// </summary>
        /// <param name="oldAnimateContent">Old Value</param>
        /// <param name="newAnimateContent">New Value</param>
        protected void OnAnimateContentChanged(bool oldAnimateContent, bool newAnimateContent)
        {

        }

        #endregion 

        #endregion

        #region Construction / Initialization

        public PivotContentControl()
        {
            ThicknessAnimation slideInAnimation = new ThicknessAnimation();
            slideInAnimation.From = new Thickness(200, 0, 0, 0);
            slideInAnimation.To = new Thickness(0, 0, 0, 0);
            slideInAnimation.Duration = new Duration(TimeSpan.FromSeconds(0.3));
            Storyboard.SetTargetProperty(slideInAnimation, new PropertyPath(FrameworkElement.MarginProperty));
            Storyboard.SetTarget(slideInAnimation, this);

            fadeInSB = new Storyboard();
            fadeInSB.Children.Add(slideInAnimation);
        }

        #endregion

        #region IPivotContent Members

        public void SetActive(bool isActive)
        {
            if (isActive)
            {
                this.Visibility = Visibility.Visible;
                if (AnimateContent)
                    fadeInSB.Begin();
            }
            else
            {
                this.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}
