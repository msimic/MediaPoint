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
using System.Windows.Media;
using WPFSpark;
using System.ComponentModel;
using System.Windows.Markup;
using System.Windows.Controls.Primitives;

namespace WPFSpark
{
    /// <summary>
    /// Class which encapsulates the header and content
    /// for each Pivot item.
    /// </summary>
    public class PivotItem : ContentControl
    {
        #region Fields

        PivotPanel parent = null;

        #endregion

        #region Dependency Properties

        #region PivotHeader

        /// <summary>
        /// PivotHeader Dependency Property
        /// </summary>
        public static readonly DependencyProperty PivotHeaderProperty =
            DependencyProperty.Register("PivotHeader", typeof(FrameworkElement), typeof(PivotItem),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPivotHeaderChanged)));

        /// <summary>
        /// Gets or sets the PivotHeader property. This dependency property 
        /// indicates the header for the PivotItem.
        /// </summary>
        public FrameworkElement PivotHeader
        {
            get { return (FrameworkElement)GetValue(PivotHeaderProperty); }
            set { SetValue(PivotHeaderProperty, value); }
        }

        /// <summary>
        /// Handles changes to the PivotHeader property.
        /// </summary>
        /// <param name="d">PivotItem</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnPivotHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotItem item = (PivotItem)d;
            FrameworkElement oldPivotHeader = (FrameworkElement)e.OldValue;
            FrameworkElement newPivotHeader = item.PivotHeader;
            item.OnPivotHeaderChanged(oldPivotHeader, newPivotHeader);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the PivotHeader property.
        /// </summary>
        /// <param name="oldPivotHeader">Old Value</param>
        /// <param name="newPivotHeader">New Value</param>
        protected void OnPivotHeaderChanged(FrameworkElement oldPivotHeader, FrameworkElement newPivotHeader)
        {
            if (parent != null)
                parent.UpdatePivotItemHeader(this);
            IPivotHeader header = newPivotHeader as IPivotHeader;
            if (header != null)
                header.SetActive(false);
        }

        #endregion

        #region PivotContent

        /// <summary>
        /// PivotContent Dependency Property
        /// </summary>
        public static readonly DependencyProperty PivotContentProperty =
            DependencyProperty.Register("PivotContent", typeof(FrameworkElement), typeof(PivotItem),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPivotContentChanged)));

        /// <summary>
        /// Gets or sets the PivotContent property. This dependency property 
        /// indicates the content of the PivotItem.
        /// </summary>
        public FrameworkElement PivotContent
        {
            get { return (FrameworkElement)GetValue(PivotContentProperty); }
            set { SetValue(PivotContentProperty, value); }
        }

        /// <summary>
        /// Handles changes to the PivotContent property.
        /// </summary>
        /// <param name="d">PivotItem</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnPivotContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotItem item = (PivotItem)d;
            FrameworkElement oldPivotContent = (FrameworkElement)e.OldValue;
            FrameworkElement newPivotContent = item.PivotContent;
            item.OnPivotContentChanged(oldPivotContent, newPivotContent);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the PivotContent property.
        /// </summary>
        /// <param name="oldPivotContent">Old Value</param>
        /// <param name="newPivotContent">New Value</param>
        protected void OnPivotContentChanged(FrameworkElement oldPivotContent, FrameworkElement newPivotContent)
        {
            if (newPivotContent != null)
            {
                if (parent != null)
                    parent.UpdatePivotItemContent(this);
                newPivotContent.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #endregion

        #region APIs

        /// <summary>
        /// Sets the parent PivotPanel of the Pivot Item
        /// </summary>
        /// <param name="panel">PivotPanel</param>
        public void SetParent(PivotPanel panel)
        {
            parent = panel;
        }

        /// <summary>
        /// Activates/Deactivates the Pivot Header and Pivot Content
        /// based on the 'isActive' flag.
        /// </summary>
        /// <param name="isActive">Flag to indicate whether the Pivot Header and Pivot Content should be Activated or Decativated</param>
        public void SetActive(bool isActive)
        {
            if (PivotHeader != null)
            {
                IPivotHeader header = PivotHeader as IPivotHeader;
                if (header != null)
                    header.SetActive(isActive);
            }

            if (PivotContent != null)
            {
                IPivotContent content = PivotContent as IPivotContent;
                if (content != null)
                    content.SetActive(isActive);
                else
                    PivotContent.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Initializes the PivotItem
        /// </summary>
        public void Initialize()
        {
            // Set the header as inactive
            if (PivotHeader != null)
            {
                IPivotHeader header = PivotHeader as IPivotHeader;
                if (header != null)
                    header.SetActive(false);
            }

            // Make the PivotContent invisible
            if (PivotContent != null)
            {
                ((FrameworkElement)PivotContent).Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}
