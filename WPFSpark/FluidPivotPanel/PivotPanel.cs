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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace WPFSpark
{
    /// <summary>
    /// The main Panel which contains the Pivot Items
    /// </summary>
    [ContentProperty("NotifiableChildren")]
    public class PivotPanel : Canvas, INotifiableParent
    {
        #region Fields

        private Grid rootGrid;
        private PivotHeaderPanel headerPanel;
        private List<PivotItem> pivotItems = null;
        private PivotItem currPivotItem = null;
        NotifiableUIElementCollection notifiableChildren = null;

        #endregion

        #region Dependency Properties

        #region ContentBackground

        /// <summary>
        /// ContentBackground Dependency Property
        /// </summary>
        public static readonly DependencyProperty ContentBackgroundProperty =
            DependencyProperty.Register("ContentBackground", typeof(Brush), typeof(PivotPanel),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnContentBackgroundChanged)));

        /// <summary>
        /// Gets or sets the ContentBackground property. This dependency property 
        /// indicates the background color of the Content.
        /// </summary>
        public Brush ContentBackground
        {
            get { return (Brush)GetValue(ContentBackgroundProperty); }
            set { SetValue(ContentBackgroundProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ContentBackground property.
        /// </summary>
        /// <param name="d">PivotPanel</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnContentBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotPanel panel = (PivotPanel)d;
            Brush oldContentBackground = (Brush)e.OldValue;
            Brush newContentBackground = panel.ContentBackground;
            panel.OnContentBackgroundChanged(oldContentBackground, newContentBackground);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the ContentBackground property.
        /// </summary>
        /// <param name="oldContentBackground">Old Value</param>
        /// <param name="newContentBackground">New Value</param>
        protected virtual void OnContentBackgroundChanged(Brush oldContentBackground, Brush newContentBackground)
        {

        }

        #endregion

        #region HeaderBackground

        /// <summary>
        /// HeaderBackground Dependency Property
        /// </summary>
        public static readonly DependencyProperty HeaderBackgroundProperty =
            DependencyProperty.Register("HeaderBackground", typeof(Brush), typeof(PivotPanel),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnHeaderBackgroundChanged)));

        /// <summary>
        /// Gets or sets the HeaderBackground property. This dependency property 
        /// indicates the background brush of the Header.
        /// </summary>
        public Brush HeaderBackground
        {
            get { return (Brush)GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        /// <summary>
        /// Handles changes to the HeaderBackground property.
        /// </summary>
        /// <param name="d">PivotPanel</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnHeaderBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotPanel panel = (PivotPanel)d;
            Brush oldHeaderBackground = (Brush)e.OldValue;
            Brush newHeaderBackground = panel.HeaderBackground;
            panel.OnHeaderBackgroundChanged(oldHeaderBackground, newHeaderBackground);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the HeaderBackground property.
        /// </summary>
        /// <param name="oldHeaderBackground">Old Value</param>
        /// <param name="newHeaderBackground">New Value</param>
        protected virtual void OnHeaderBackgroundChanged(Brush oldHeaderBackground, Brush newHeaderBackground)
        {

        }

        #endregion

        #region HeaderHeight

        /// <summary>
        /// HeaderHeight Dependency Property
        /// </summary>
        public static readonly DependencyProperty HeaderHeightProperty =
            DependencyProperty.Register("HeaderHeight", typeof(GridLength), typeof(PivotPanel),
                new FrameworkPropertyMetadata(new GridLength(0.1, GridUnitType.Star), new PropertyChangedCallback(OnHeaderHeightChanged)));

        /// <summary>
        /// Gets or sets the HeaderHeight property. This dependency property 
        /// indicates the Height of the header.
        /// </summary>
        public GridLength HeaderHeight
        {
            get { return (GridLength)GetValue(HeaderHeightProperty); }
            set { SetValue(HeaderHeightProperty, value); }
        }

        /// <summary>
        /// Handles changes to the HeaderHeight property.
        /// </summary>
        /// <param name="d">PivotPanel</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnHeaderHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotPanel panel = (PivotPanel)d;
            GridLength oldHeaderHeight = (GridLength)e.OldValue;
            GridLength newHeaderHeight = panel.HeaderHeight;
            panel.OnHeaderHeightChanged(oldHeaderHeight, newHeaderHeight);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the HeaderHeight property.
        /// </summary>
        /// <param name="oldHeaderHeight">Old Value</param>
        /// <param name="newHeaderHeight">New Value</param>
        protected virtual void OnHeaderHeightChanged(GridLength oldHeaderHeight, GridLength newHeaderHeight)
        {
            if ((rootGrid != null) && (rootGrid.RowDefinitions.Count > 0))
            {
                rootGrid.RowDefinitions[0].Height = newHeaderHeight;
            }
        }

        #endregion

        #region ItemsSource

        /// <summary>
        /// ItemsSource Dependency Property
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<PivotItem>), typeof(PivotPanel),
                new FrameworkPropertyMetadata(new PropertyChangedCallback(OnItemsSourceChanged)));

        /// <summary>
        /// Gets or sets the ItemsSource property. This dependency property 
        /// indicates the bindable collection.
        /// </summary>
        public ObservableCollection<PivotItem> ItemsSource
        {
            get { return (ObservableCollection<PivotItem>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ItemsSource property.
        /// </summary>
        /// <param name="d">PivotPanel</param>
        /// <param name="e">DependencyProperty changed event arguments</param>
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PivotPanel panel = (PivotPanel)d;
            ObservableCollection<PivotItem> oldItemsSource = (ObservableCollection<PivotItem>)e.OldValue;
            ObservableCollection<PivotItem> newItemsSource = panel.ItemsSource;
            panel.OnItemsSourceChanged(oldItemsSource, newItemsSource);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the ItemsSource property.
        /// </summary>
        /// <param name="oldItemsSource">Old Value</param>
        /// <param name="newItemsSource">New Value</param>
        protected virtual void OnItemsSourceChanged(ObservableCollection<PivotItem> oldItemsSource, ObservableCollection<PivotItem> newItemsSource)
        {
            this.ClearItemsSource();

            if (newItemsSource != null)
                AddItems(newItemsSource.ToList());
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Property used to set the Content Property for the FluidWrapPanel
        /// </summary>
        public NotifiableUIElementCollection NotifiableChildren
        {
            get
            {
                return notifiableChildren;
            }
        }

        #endregion

        #region Construction / Initialization

        public PivotPanel()
        {
            notifiableChildren = new NotifiableUIElementCollection(this, this);

            // Create the root grid that will hold the header panel and the contents
            rootGrid = new Grid();

            RowDefinition rd = new RowDefinition();
            rd.Height = HeaderHeight;
            rootGrid.RowDefinitions.Add(rd);

            rd = new RowDefinition();
            rd.Height = new GridLength(1, GridUnitType.Star);
            rootGrid.RowDefinitions.Add(rd);

            Binding backgroundBinding = new Binding();
            backgroundBinding.Source = this.Background;
            rootGrid.SetBinding(Grid.BackgroundProperty, backgroundBinding);

            rootGrid.Width = this.ActualWidth;
            rootGrid.Height = this.ActualHeight;

            rootGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
            rootGrid.VerticalAlignment = VerticalAlignment.Stretch;

            // Create the header panel
            headerPanel = new PivotHeaderPanel();
            headerPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            headerPanel.VerticalAlignment = VerticalAlignment.Stretch;
            headerPanel.HeaderSelected += new EventHandler(OnHeaderSelected);
            rootGrid.Children.Add(headerPanel);

            this.Children.Add(rootGrid);

            pivotItems = new List<PivotItem>();

            this.SizeChanged += (s, e) =>
                {
                    if (rootGrid != null)
                    {
                        rootGrid.Width = this.ActualWidth;
                        rootGrid.Height = this.ActualHeight;
                    }
                };
        }

        #endregion

        #region APIs

        /// <summary>
        /// Adds a PivotItem to the PivotPanel's Children collection
        /// </summary>
        /// <param name="item">PivotItem</param>
        public int AddChild(PivotItem item)
        {
            if (pivotItems == null)
                pivotItems = new List<PivotItem>();

            pivotItems.Add(item);

            item.SetParent(this);

            if (item.PivotHeader != null)
                headerPanel.AddChild(item.PivotHeader as UIElement);

            if (item.PivotContent != null)
            {
                Grid.SetRow(item.PivotContent as UIElement, 1);
                // Set the item to its initial state
                item.Initialize();
                rootGrid.Children.Add(item.PivotContent as UIElement);
            }

            return pivotItems.Count - 1;
        }

        /// <summary>
        /// Adds the newly assigned PivotHeader of the PivotItem to the PivotPanel
        /// </summary>
        /// <param name="item">PivotItem</param>
        internal void UpdatePivotItemHeader(PivotItem item)
        {
            if ((pivotItems.Contains(item)) && (item.PivotHeader != null) && (!headerPanel.Contains((UIElement)item.PivotHeader)))
            {
                headerPanel.AddChild(item.PivotHeader as UIElement);
                // Activate the First Pivot Item.
                ActivateFirstPivotItem();
            }
        }

        /// <summary>
        /// Adds the newly assigned PivotContent of the PivotItem to the PivotPanel
        /// </summary>
        /// <param name="item">PivotItem</param>
        internal void UpdatePivotItemContent(PivotItem item)
        {
            if ((pivotItems.Contains(item)) && (item.PivotContent != null) && (!rootGrid.Children.Contains((UIElement)item.PivotContent)))
            {
                Grid.SetRow(item.PivotContent as UIElement, 1);
                rootGrid.Children.Add(item.PivotContent as UIElement);
                // Activate the First Pivot Item.
                ActivateFirstPivotItem();
            }
        }

        /// <summary>
        /// Adds a list of PivotItems to the PivotPanel's Children collection
        /// </summary>
        /// <param name="items">List of PivotItems</param>
        public void AddItems(List<PivotItem> items)
        {
            if (items == null)
                return;

            foreach (PivotItem item in items)
            {
                AddChild(item);
            }

            ActivateFirstPivotItem();
        }

        /// <summary>
        /// Sets the DataContext for the PivotContent of each of the PivotItems.
        /// </summary>
        /// <param name="context">Data Context</param>
        public void SetDataContext(object context)
        {
            if ((pivotItems == null) || (pivotItems.Count == 0))
                return;

            foreach (PivotItem item in pivotItems)
            {
                item.PivotContent.DataContext = context;
            }
        }

        /// <summary>
        /// Resets the location of the header items so that the 
        /// first child that was added is moved to the beginning.
        /// </summary>
        public void Reset()
        {
            if (headerPanel != null)
                headerPanel.Reset();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the event raised when a header item is selected
        /// </summary>
        /// <param name="sender">Header item</param>
        /// <param name="e">Event Args</param>
        void OnHeaderSelected(object sender, EventArgs e)
        {
            FrameworkElement headerItem = sender as FrameworkElement;
            if (headerItem == null)
                return;

            // Find the PivotItem whose header was selected
            PivotItem pItem = pivotItems.Where(p => p.PivotHeader == headerItem).FirstOrDefault();

            if ((pItem != null) && (pItem != currPivotItem))
            {
                if (currPivotItem != null)
                {
                    currPivotItem.SetActive(false);
                }

                pItem.SetActive(true);
                currPivotItem = pItem;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Sets the First Pivot item as active
        /// </summary>
        private void ActivateFirstPivotItem()
        {
            // Set the first item as active
            if ((pivotItems != null) && (pivotItems.Count > 0))
            {
                pivotItems.First().SetActive(true);
                currPivotItem = pivotItems.First();
            }
        }

        /// <summary>
        /// Removes all the Pivot Items from the Children collection
        /// </summary>
        void ClearItemsSource()
        {
            if ((pivotItems == null) || (pivotItems.Count == 0))
                return;

            if (headerPanel != null)
                headerPanel.ClearHeader();

            if (rootGrid != null)
            {
                foreach (PivotItem item in pivotItems)
                {
                    rootGrid.Children.Remove(item.PivotContent);
                }
            }

            pivotItems.Clear();
        }

        #endregion

        #region INotifiableParent Members

        /// <summary>
        /// Adds the child to the Panel through XAML
        /// </summary>
        /// <param name="child">Child to be added</param>
        /// <returns>Index of the child in the collection</returns>
        public int AddChild(UIElement child)
        {
            PivotItem pItem = child as PivotItem;
            if (pItem != null)
            {
                return AddChild(pItem);
            }

            return -1;
        }

        #endregion
    }
}
