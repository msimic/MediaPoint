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
using System.Windows.Input;
using System.Windows.Media;

namespace WPFSpark
{
    /// <summary>
    /// Class which implements the IPivotHeader interface
    /// and represents the header item in text form.
    /// </summary>
    public class PivotHeaderControl : ContentControl, IPivotHeader
    {
        #region Dependency Properties

        #region ActiveForeground

        /// <summary>
        /// ActiveForeground Dependency Property
        /// </summary>
        public static readonly DependencyProperty ActiveForegroundProperty =
            DependencyProperty.Register("ActiveForeground", typeof(Brush), typeof(PivotHeaderControl),
                new FrameworkPropertyMetadata(Brushes.Black, new PropertyChangedCallback(OnActiveForegroundChanged)));

        /// <summary>
        /// Gets or sets the ActiveForeground property. This dependency property 
        /// indicates the foreground color of the Header Item when it is active.
        /// </summary>
        public Brush ActiveForeground
        {
            get { return (Brush)GetValue(ActiveForegroundProperty); }
            set { SetValue(ActiveForegroundProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ActiveForeground property.
        /// </summary>
        /// <param name="d">PivotHeaderControl</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnActiveForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotHeaderControl header = (PivotHeaderControl)d;
            Brush oldActiveForeground = (Brush)e.OldValue;
            Brush newActiveForeground = header.ActiveForeground;
            header.OnActiveForegroundChanged(oldActiveForeground, newActiveForeground);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the ActiveForeground property.
        /// </summary>
        /// <param name="oldActiveForeground">Old Value</param>
        /// <param name="newActiveForeground">New Value</param>
        protected virtual void OnActiveForegroundChanged(Brush oldActiveForeground, Brush newActiveForeground)
        {
            if (IsActive)
            {
                this.Foreground = newActiveForeground;
            }
        }

        #endregion

        #region InactiveForeground

        /// <summary>
        /// InactiveForeground Dependency Property
        /// </summary>
        public static readonly DependencyProperty InactiveForegroundProperty =
            DependencyProperty.Register("InactiveForeground", typeof(Brush), typeof(PivotHeaderControl),
                new FrameworkPropertyMetadata(Brushes.DarkGray, new PropertyChangedCallback(OnInactiveForegroundChanged)));

        /// <summary>
        /// Gets or sets the InactiveForeground property. This dependency property 
        /// indicates the foreground color when the Header Item is inactive.
        /// </summary>
        public Brush InactiveForeground
        {
            get { return (Brush)GetValue(InactiveForegroundProperty); }
            set { SetValue(InactiveForegroundProperty, value); }
        }

        /// <summary>
        /// Handles changes to the InactiveForeground property.
        /// </summary>
        /// <param name="d">PivotHeaderControl</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnInactiveForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotHeaderControl header = (PivotHeaderControl)d;
            Brush oldInactiveForeground = (Brush)e.OldValue;
            Brush newInactiveForeground = header.InactiveForeground;
            header.OnInactiveForegroundChanged(oldInactiveForeground, newInactiveForeground);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the InactiveForeground property.
        /// </summary>
        /// <param name="oldInactiveForeground">Old Value</param>
        /// <param name="newInactiveForeground">New Value</param>
        protected virtual void OnInactiveForegroundChanged(Brush oldInactiveForeground, Brush newInactiveForeground)
        {
            if (!IsActive)
            {
                this.Foreground = newInactiveForeground;
            }
        }

        #endregion

        #region IsActive

        /// <summary>
        /// IsActive Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(PivotHeaderControl),
                new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnIsActiveChanged)));

        /// <summary>
        /// Gets or sets the IsActive property. This dependency property 
        /// indicates whether the Header Item is currently active.
        /// </summary>
        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsActive property.
        /// </summary>
        /// <param name="d">PivotHeaderControl</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotHeaderControl header = (PivotHeaderControl)d;
            bool oldIsActive = (bool)e.OldValue;
            bool newIsActive = header.IsActive;
            header.OnIsActiveChanged(oldIsActive, newIsActive);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the IsActive property.
        /// </summary>
        /// <param name="oldIsActive">Old Value</param>
        /// <param name="newIsActive">New Value</param>
        protected virtual void OnIsActiveChanged(bool oldIsActive, bool newIsActive)
        {
            this.Foreground = newIsActive ? ActiveForeground : InactiveForeground;
            this.SetValue(TextBlock.ForegroundProperty, this.Foreground);
        }

        #endregion

        #endregion

        #region Construction / Initialization

        /// <summary>
        /// Ctor
        /// </summary>
        public PivotHeaderControl()
        {
            // By default, the header will be inactive
            IsActive = false;
            this.Foreground = InactiveForeground;
            // This control will raise the HeaderSelected event on Mouse Left Button down
            this.MouseLeftButtonDown +=new MouseButtonEventHandler(OnMouseDown);
        }

        #endregion

        #region IPivotHeader Members

        /// <summary>
        /// Activates/Deactivates the Pivot Header based on the 'isActive' flag.
        /// </summary>
        /// <param name="isActive">Flag to indicate whether the Pivot Header and Pivot Content should be Activated or Deactivated</param>
        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }

        public event EventHandler HeaderSelected;

        #endregion

        #region EventHandlers

        /// <summary>
        /// Handler for the mouse down event
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event Args</param>
        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (HeaderSelected != null)
            {
                HeaderSelected(this, new EventArgs());
            }
        }

        #endregion
    }
}
