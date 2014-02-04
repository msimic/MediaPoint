using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;
using System.ComponentModel;

namespace MediaPoint.Controls.Behaviors
{
	public class StylizedBehaviors
	{
		public static IList<DependencyProperty> GetDependencyProperties(DependencyObject obj, bool getAttached)
		{
			List<DependencyProperty> dps = new List<DependencyProperty>();

			foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj,
				new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) }))
			{
				DependencyPropertyDescriptor dpd =
					DependencyPropertyDescriptor.FromProperty(pd);

				if (getAttached)
				{
					if (dpd != null && dpd.IsAttached)
					{
						dps.Add(dpd.DependencyProperty);
					}
				}
				else
				{
					if (dpd != null && !dpd.IsAttached)
					{
						dps.Add(dpd.DependencyProperty);
					}
				}
			}

			return dps;
		}

		#region Fields (public)
		public static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached(
			@"Behaviors",
			typeof(StylizedBehaviorCollection),
			typeof(StylizedBehaviors),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));
		#endregion
		#region Static Methods (public)
		public static StylizedBehaviorCollection GetBehaviors(DependencyObject uie)
		{
			return (StylizedBehaviorCollection)uie.GetValue(BehaviorsProperty);
		}

		public static void SetBehaviors(DependencyObject uie, StylizedBehaviorCollection value)
		{
			uie.SetValue(BehaviorsProperty, value);
		}
		#endregion

		#region Static Methods (private)
		private static void OnPropertyChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs e)
		{
			bool designTime = System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject());
			if (designTime) return;

			var uie = dpo as UIElement;

			if (uie == null)
			{
				return;
			}

			BehaviorCollection itemBehaviors = Interaction.GetBehaviors(uie);

			var newBehaviors = e.NewValue as StylizedBehaviorCollection;
			var oldBehaviors = e.OldValue as StylizedBehaviorCollection;

			if (newBehaviors == oldBehaviors)
			{
				return;
			}

			if (oldBehaviors != null)
			{
				foreach (var behavior in oldBehaviors)
				{
					int index = itemBehaviors.IndexOf(behavior);

					if (index >= 0)
					{
						itemBehaviors.RemoveAt(index);
					}
				}
			}

			if (itemBehaviors != null) while (itemBehaviors.Count > 0)
			{
				itemBehaviors[0].Detach();
				itemBehaviors.RemoveAt(0);
			}

			if (newBehaviors != null)
			{
				foreach (var behavior in newBehaviors)
				{
					int index = itemBehaviors.IndexOf(behavior);

					if (index < 0)
					{
						var dps = GetDependencyProperties(behavior, false);
						var beh = (Behavior)Activator.CreateInstance(behavior.GetType());
						foreach (var dp in dps)
						{
							beh.SetValue(dp, behavior.GetValue(dp));
						}
						beh.Attach(dpo);
						itemBehaviors.Add(beh);
					}
				}
			}
		}
		#endregion
	}

	public class StylizedBehaviorCollection : FreezableCollection<Behavior>
	{
		#region Methods (protected)
		protected override Freezable CreateInstanceCore()
		{
			return new StylizedBehaviorCollection();
		}
		#endregion
	}
}
