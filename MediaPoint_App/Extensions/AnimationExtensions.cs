using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Media;

namespace MediaPoint.App.Extensions
{
	public static class AnimationExtensions
	{
		public static void AnimatePropertyTo<T, R>(this T element, Expression<Func<T, R>> p, R finalValue, double duration, bool autoReverse = false)
			where T : IAnimatable
		{

			if (typeof(R) == typeof(double))
			{
				AnimatePropertyTo<T, R, DoubleAnimation>(element, p, finalValue, duration, autoReverse);
				return;
			}
			else if (typeof(R) == typeof(Color))
			{
				AnimatePropertyTo<T, R, ColorAnimation>(element, p, finalValue, duration, autoReverse);
				return;
			}
			else if (typeof(R) == typeof(Point))
			{
				AnimatePropertyTo<T, R, PointAnimation>(element, p, finalValue, duration, autoReverse);
				return;
			}

			throw new InvalidOperationException("Could not determine type of animation needed, use the generic signature that allows specifying the type of animation. The animation must have From and To dependency properties.");
		}

		public static void AnimatePropertyTo<T, R, AT>(this T element, Expression<Func<T, R>> p, R finalValue, double duration, bool autoReverse)
			where T : IAnimatable
			where AT : AnimationTimeline
		{
			AnimationTimeline animation = (AnimationTimeline)Activator.CreateInstance(typeof(AT));

			if (animation == null) return;

			var prop = (p.Body as MemberExpression).Member.Name;
			var currentValue = p.Compile()(element);

			DependencyPropertyDescriptor dFrom = DependencyPropertyDescriptor.FromName("From", animation.GetType(), animation.GetType());
			animation.SetValue(dFrom.DependencyProperty, currentValue);

			DependencyPropertyDescriptor dTo = DependencyPropertyDescriptor.FromName("To", animation.GetType(), animation.GetType());
			animation.SetValue(dTo.DependencyProperty, finalValue);

			animation.Duration = new Duration(TimeSpan.FromSeconds(duration));
			animation.AutoReverse = autoReverse;

			DependencyPropertyDescriptor d = DependencyPropertyDescriptor.FromName(prop, typeof(T), typeof(T));
			element.BeginAnimation(d.DependencyProperty, null);
			element.BeginAnimation(d.DependencyProperty, animation);
		}
	}
}
