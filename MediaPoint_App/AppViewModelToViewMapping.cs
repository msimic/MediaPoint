using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using MediaPoint.VM.ViewInterfaces;
using System.Windows.Data;
using MediaPoint.MVVM;
using System.Reflection;

namespace MediaPoint.App
{
	public class AppViewmodel2ViewMapping<VM, VIEW> : IViewModelToViewMapping
		where VIEW : DependencyObject
		where VM : ViewModel
	{
		private void SetBinding(VIEW view)
		{
			var dps = AppDialogService.GetDependencyProperties(view, false);
			foreach (var b in _propBindings)
			{
				string[] bdef = b.Split(',');
				var dp = dps.FirstOrDefault(d => d.Name == bdef[0]);
				if (dp != null)
					BindingOperations.SetBinding(view, dp, CreateBinding(bdef[1]));
			}
		}

		private static Binding CreateBinding(string input)
		{
			string[] values = input.Split('|');

			var binding = new Binding(values[0]);
			BindingMode mode = BindingMode.Default;
			if (values.Length > 1)
			{
				try
				{
					mode = (BindingMode)Enum.Parse(typeof(BindingMode), values[1], true);
				}
				catch
				{
					return null;
				}
			}
			binding.Mode = mode;
			return binding;
		}

		public VM ShowDialog(bool isModal, string title, double width = 0, double height = 0)
		{
			VIEW view = Activator.CreateInstance<VIEW>();
			Type type = typeof(VM);
			ConstructorInfo ctor = type.GetConstructors()[0];
			var parameters = ctor.GetParameters();
			object[] paramvalues = parameters.Select(p => p.DefaultValue.GetType() == typeof(DBNull) ? null : p.DefaultValue).ToArray();
			VM vm = (VM)ctor.Invoke(paramvalues);
			SetBinding(view);
			var wnd = new Window();
			wnd.Title = title;
			wnd.Content = view;
			wnd.DataContext = vm;
			if (width != 0) wnd.Width = width;
			if (height != 0) wnd.Height = height;
			if (isModal)
			{
				wnd.ShowDialog();
				return vm;
			}
			else
			{
				wnd.Show();
				return vm;
			}
		}

		public bool? ShowDialog(VM vm, bool isModal, string title, double width = 0, double height = 0)
		{
			VIEW view = Activator.CreateInstance<VIEW>();
			SetBinding(view);
			var wnd = new Window();
			wnd.Title = title;
			wnd.Content = view;
			wnd.DataContext = vm;
			if (width != 0) wnd.Width = width;
			if (height != 0) wnd.Height = height;
			if (isModal)
			{
				return wnd.ShowDialog();
			}
			else
			{
				wnd.Show();
				return true;
			}
		}

		string[] _propBindings;
		public AppViewmodel2ViewMapping(string[] propertyBindings)
		{
			_propBindings = propertyBindings;
		}

		public Type ViewModel
		{
			get { return typeof(VM); }
		}

		public Type View
		{
			get { return typeof(VIEW); }
		}

		ViewModel IViewModelToViewMapping.ShowDialog(bool isModal, string title, double width, double height)
		{
			return this.ShowDialog(isModal, title, width, height);
		}

		public bool? ShowDialog(ViewModel vm, bool isModal, string title, double width = 0, double height = 0)
		{
			return this.ShowDialog((VM)vm, isModal, title, width, height);
		}
	}
}
