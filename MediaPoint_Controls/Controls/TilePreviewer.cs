using System;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MediaPoint.Controls.Extensions;
using System.Windows.Media.Animation;

namespace MediaPoint.Controls
{
	public class TileControl : MultiSelector
	{
		public TileControl()
		{
			//DefaultStyleKeyProperty.OverrideMetadata(typeof(TileControl),
			//    new FrameworkPropertyMetadata(typeof(TileControl)));

			this.CanSelectMultipleItems = true;

			var factory = new FrameworkElementFactory(typeof(TilePreviewer));
			factory.SetValue(TilePreviewer.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
			factory.SetValue(TilePreviewer.VerticalAlignmentProperty, VerticalAlignment.Stretch);
			factory.SetValue(TilePreviewer.IsItemsHostProperty, true);
			var bnd = new Binding();
			bnd.Source = this;
			bnd.Path = new PropertyPath(TileControl.SelectedIndexProperty);
			bnd.Mode = BindingMode.TwoWay;
			factory.SetBinding(TilePreviewer.SelectedIndexProperty, bnd);
			ItemsPanel = new ItemsPanelTemplate(factory);

			DataTemplate dt = new DataTemplate();
			var fac = new FrameworkElementFactory(typeof(ContentPresenter));
			fac.SetValue(FrameworkElement.MarginProperty, new Thickness(0.0));
			var bnd2 = new Binding(".");
			bnd2.Mode = BindingMode.OneTime;
			fac.SetBinding(ContentPresenter.ContentProperty, bnd);
			dt.VisualTree = fac;
			ItemTemplate = dt;
			SelectedIndex = -1;
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			var contentitem = element as FrameworkElement;

			if (this.ItemContainerStyle != null && contentitem != null && contentitem.Style == null)
			{
				contentitem.SetValue(Control.StyleProperty, this.ItemContainerStyle);
			}

			//contentitem.MouseLeftButtonUp += contentitem_MouseLeftButtonUp;
			base.PrepareContainerForItemOverride(element, item);
		}

		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			FrameworkElement contentitem = element as FrameworkElement;
			//contentitem.MouseLeftButtonUp -= contentitem_MouseLeftButtonUp;
			base.ClearContainerForItemOverride(element, item);
		}

		// gets called only when clicking on border or textblock directly not somewhere in between?!
		void contentitem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var index = Items.IndexOf(ItemContainerGenerator.ItemFromContainer(sender as DependencyObject));

			if (Items.Count == 1)
			{
				SelectedIndex = 0;
			}
			else if (SelectedIndex == index)
			{
				// deselect
				SelectedIndex = -1;
			}
			else
			{
				// select
				SelectedIndex = index;
			}
		}

	}

	public class TilePreviewer : Canvas
	{
		static TilePreviewer()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TilePreviewer),
				new FrameworkPropertyMetadata(typeof(TilePreviewer)));
		}

		public int SelectedIndex
		{
			get { return (int)GetValue(SelectedIndexProperty); }
			set { SetValue(SelectedIndexProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedIndex.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedIndexProperty =
			DependencyProperty.Register("SelectedIndex", typeof(int), typeof(TilePreviewer), new UIPropertyMetadata(-1, SelectedIndexChanged));

		int _selIndex;

		private double getTransformHeight()
		{
			return (DesiredSize.Height / (rows<cols?cols:rows)) / DesiredSize.Height;
		}

		private double getTransformWidth()
		{
			return (DesiredSize.Width / cols) / DesiredSize.Width;
		}

		private double getTransformLeftFromIndex(int index)
		{
			return getTransformLeftFromColumn(getCol(index));
		}

		private double getTransformTopFromIndex(int index)
		{
			return getTransformTopFromRow(getRow(index));
		}

		private double getTransformLeftFromColumn(int column)
		{
			return getTransformWidth() * DesiredSize.Width * column;
		}

		private double getTransformTopFromRow(int row)
		{
			return getTransformHeight() * DesiredSize.Height * row + ((((double)(cols-rows)/2)) * getTransformHeight() * DesiredSize.Height);
		}

		Storyboard _last;
		Storyboard _lastNew;
		private static void SelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var tp = d as TilePreviewer;

			int oldIndex = tp._selIndex;
			tp._selIndex = (int)e.NewValue;			
			int newIndex = tp._selIndex;

			if (tp.Children.Count < oldIndex || tp.Children.Count < newIndex) return;

			if (oldIndex != -1)
			{
				var oldItem = tp.Children[oldIndex];
				if (oldItem.RenderTransform is TransformGroup)
				{
					foreach (var item in tp.Children)
					{
						UIElement tile = item as UIElement;
						if (tile == oldItem)
						{
							var scale = (oldItem.RenderTransform as TransformGroup).Children.First(c => c.GetType() == typeof(ScaleTransform)) as ScaleTransform;
							var translate = (oldItem.RenderTransform as TransformGroup).Children.First(c => c.GetType() == typeof(TranslateTransform)) as TranslateTransform;
							oldItem.SetValue(Panel.ZIndexProperty, tp.Children.Count);
							scale.AnimatePropertyTo(s => s.ScaleY, tp.getTransformHeight(), 0.5);
							tp._last = scale.AnimatePropertyTo(s => s.ScaleX, tp.getTransformWidth(), 0.5);
							translate.AnimatePropertyTo(s => s.X, tp.getTransformLeftFromIndex(oldIndex), 0.5);
							translate.AnimatePropertyTo(s => s.Y, tp.getTransformTopFromIndex(oldIndex), 0.5);
						}
						else
						{
							tile.AnimatePropertyTo(t => t.Opacity, 1.0, 0.5);
						}
					}
				}
			}

			if (newIndex != -1)
			{
				var newItem = tp.Children[newIndex];
				if (newItem.RenderTransform is TransformGroup)
				{
					foreach (var item in tp.Children)
					{
						UIElement tile = item as UIElement;
						if (tile == newItem)
						{
							var scale = (newItem.RenderTransform as TransformGroup).Children.First(c => c.GetType() == typeof(ScaleTransform)) as ScaleTransform;
							var translate = (newItem.RenderTransform as TransformGroup).Children.First(c => c.GetType() == typeof(TranslateTransform)) as TranslateTransform;
							newItem.SetValue(Panel.ZIndexProperty, tp.Children.Count);
							scale.AnimatePropertyTo(s => s.ScaleY, 1, 0.5);
							tp._lastNew = scale.AnimatePropertyTo(s => s.ScaleX, 1, 0.5);
							translate.AnimatePropertyTo(s => s.X, 0, 0.5);
							translate.AnimatePropertyTo(s => s.Y, 0, 0.5);
						}
						else
						{
							tile.AnimatePropertyTo(t => t.Opacity, 0.0, 0.5);
						}
					}
					tp.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				}
			}
			else
			{
				foreach (var item in tp.Children)
				{
					UIElement tile = item as UIElement;
					var scale = (tile.RenderTransform as TransformGroup).Children.First(c => c.GetType() == typeof(ScaleTransform)) as ScaleTransform;
					var translate = (tile.RenderTransform as TransformGroup).Children.First(c => c.GetType() == typeof(TranslateTransform)) as TranslateTransform;
					tile.SetValue(Panel.ZIndexProperty, 0);
					scale.AnimatePropertyTo(s => s.ScaleY, tp.getTransformHeight(), 0.5);
					tp._last = scale.AnimatePropertyTo(s => s.ScaleX, tp.getTransformWidth(), 0.5);
					translate.AnimatePropertyTo(s => s.X, tp.getTransformLeftFromIndex(oldIndex), 0.5);
					translate.AnimatePropertyTo(s => s.Y, tp.getTransformTopFromIndex(oldIndex), 0.5);
					tile.AnimatePropertyTo(t => t.Opacity, 1.0, 0.5);

				}
				tp.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			}
		}

		private int getRow(int index)
		{
			return (int)Math.Floor((double)index / cols);
		}

		private int getCol(int index)
		{
			return index % cols;
		}

		int cols = 1;
		int rows = 1;

		protected override Size MeasureOverride(Size availableSize)
		{
			Size resultSize = new Size(0, 0);

			cols = (int)Math.Ceiling(Math.Sqrt(Children.Count));
			rows = (int)Math.Ceiling((double)Children.Count / cols);

			foreach (UIElement child in Children)
			{
				child.Measure(availableSize);
				resultSize.Width = Math.Max(resultSize.Width, child.DesiredSize.Width);
				resultSize.Height = Math.Max(resultSize.Height, child.DesiredSize.Height);
			}

			resultSize.Width = double.IsPositiveInfinity(availableSize.Width) ?
				resultSize.Width : availableSize.Width;

			resultSize.Height = double.IsPositiveInfinity(availableSize.Height) ?
				resultSize.Height : availableSize.Height;

			return resultSize;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var sz = finalSize;
			var cellWidth = sz.Width / cols;
			var cellHeight = sz.Height / rows;

			int index = 0;
			
			foreach (UIElement child in Children)
			{
				var col = getCol(index);
				var row = getRow(index);

				double left = (col) * cellWidth;
				double top = (row) * cellHeight;

				//child.Arrange(new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight));			
				child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));			
			
				if (index != SelectedIndex)
				{
					TransformGroup tg = new TransformGroup();
					tg.Children.Add(new ScaleTransform(getTransformWidth(), getTransformHeight()));
					double x = getTransformLeftFromIndex(index);
					double y = getTransformTopFromIndex(index);
					var tt = new TranslateTransform(x, y);
					//tt.SetValue(UIElement.RenderTransformOriginProperty, new Point(0, 0));
					child.RenderTransformOrigin = new Point(0, 0);
					tg.Children.Add(tt);					
					child.RenderTransform = tg;
					child.SetValue(Panel.ZIndexProperty, 0);
				}
				else
				{
					TransformGroup tg = new TransformGroup();
					tg.Children.Add(new ScaleTransform(1, 1));
					tg.Children.Add(new TranslateTransform(0, 0));
					child.RenderTransformOrigin = new Point(0, 0);
					child.RenderTransform = tg;
					child.SetValue(Panel.ZIndexProperty, 1);
				}
				index++;
			}

			return finalSize;
		}
	}
}
