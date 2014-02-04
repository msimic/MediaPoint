using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Linq.Expressions;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;

namespace MediaPoint.Controls.Extensions
{
	public static class Animations
	{
		public static Storyboard AnimatePropertyTo<T, R>(this T element, Expression<Func<T, R>> p, R finalValue, double duration, bool autoReverse = false)
			where T : IAnimatable
		{
			if (typeof(R) == typeof(double))
			{
				return AnimatePropertyTo<T, R, DoubleAnimation>(element, p, finalValue, duration, autoReverse);
			}
			else if (typeof(R) == typeof(Color))
			{
				return AnimatePropertyTo<T, R, ColorAnimation>(element, p, finalValue, duration, autoReverse);
			}
			else if (typeof(R) == typeof(Point))
			{
				return AnimatePropertyTo<T, R, PointAnimation>(element, p, finalValue, duration, autoReverse);
			}

			throw new InvalidOperationException("Could not determine type of animation needed, use the generic signature that allows specifying the type of animation. The animation must have From and To dependency properties.");
		}

		public static Storyboard AnimatePropertyTo<T, R, AT>(this T element, Expression<Func<T, R>> p, R finalValue, double duration, bool autoReverse)
			where T : IAnimatable
			where AT : AnimationTimeline
		{
			
			AnimationTimeline animation = (AnimationTimeline)Activator.CreateInstance(typeof(AT));

			if (animation == null) return null;

			var prop = (p.Body as MemberExpression).Member.Name;
			var currentValue = p.Compile()(element);

			DependencyPropertyDescriptor dFrom = DependencyPropertyDescriptor.FromName("From", animation.GetType(), animation.GetType());
			animation.SetValue(dFrom.DependencyProperty, currentValue);

			DependencyPropertyDescriptor dTo = DependencyPropertyDescriptor.FromName("To", animation.GetType(), animation.GetType());
			animation.SetValue(dTo.DependencyProperty, finalValue);

			animation.Duration = new Duration(TimeSpan.FromSeconds(duration));
			animation.AutoReverse = autoReverse;

			DependencyPropertyDescriptor d = DependencyPropertyDescriptor.FromName(prop, typeof(T), typeof(T));
			
			element.BeginAnimation(d.DependencyProperty, animation);

			Storyboard s = new Storyboard();
			animation.SetValue(Storyboard.TargetProperty, element);
			s.BeginAnimation(d.DependencyProperty, null);
			s.BeginAnimation(d.DependencyProperty, animation);
			return s;
		}
	}
}
