using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPoint.VM.ViewInterfaces;
using System.Windows;
using System.ComponentModel;
using MediaPoint.MVVM;
using System.IO;

namespace MediaPoint.App
{
	public class AppDialogService : IDialogService
	{
		List<IViewModelToViewMapping> mappings = new List<IViewModelToViewMapping>();

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

		public VM ShowDialog<VM>(bool modal, string title, double width = 0, double height = 0) where VM : ViewModel
		{
			var mapping = mappings.FirstOrDefault(m => m.ViewModel == typeof(VM));
			if (mapping != null)
			{
				return (VM)mapping.ShowDialog(modal, title, width, height);
			}
			else
			{
				return null;
			}
		}

		public bool? ShowDialog<VM>(VM vm, bool modal, string title, double width = 0, double height = 0) where VM : ViewModel
		{
			var mapping = mappings.FirstOrDefault(m => m.ViewModel == typeof(VM));
			if (mapping != null)
			{
				return mapping.ShowDialog(vm, modal, title, width, height);
			}
			else
			{
				return false;
			}
		}

		public Uri ShowOpenUriDialog(string startpath, string filter)
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
			dlg.InitialDirectory = Path.GetDirectoryName(startpath);
			dlg.Filter = filter;
			dlg.CheckPathExists = true;
			dlg.CheckFileExists = true;
			if (dlg.ShowDialog() == true)
			{
				return new Uri(dlg.FileName);
			}
			else
			{
				return null;
			}
		}

		public eDialogResult ShowMessageBox(string text, string title, eMessageBoxType type, eMessageBoxIcon icon)
		{
			MessageBoxButton button = MessageBoxButton.OK;
			MessageBoxImage micon = MessageBoxImage.None;

			switch(type) {
				case eMessageBoxType.Ok:
					button = MessageBoxButton.OK;
					break;
				case eMessageBoxType.YesNo:
					button = MessageBoxButton.YesNo;
					break;
				case eMessageBoxType.OkCancel:
					button = MessageBoxButton.OKCancel;
					break;
				case eMessageBoxType.YesNoCancel:
					button = MessageBoxButton.YesNoCancel;
					break;
				default:
					button = MessageBoxButton.OK;
					break;
			}
			switch (icon)
			{
				case eMessageBoxIcon.Error:
					micon = MessageBoxImage.Error;
					break;
				case eMessageBoxIcon.Info:
					micon = MessageBoxImage.Information;
					break;
				case eMessageBoxIcon.Warning:
					micon = MessageBoxImage.Warning;
					break;
				case eMessageBoxIcon.Question:
					micon = MessageBoxImage.Question;
					break;
				default:
					micon = MessageBoxImage.None;
					break;
			}

			var ret = MessageBox.Show(text, title, button, micon);

			switch (ret)
			{
				case MessageBoxResult.Yes:
					return eDialogResult.Yes;
				case MessageBoxResult.OK:
					return eDialogResult.OK;
				case MessageBoxResult.No:
					return eDialogResult.No;
				case MessageBoxResult.Cancel:
					return eDialogResult.Cancel;
				case MessageBoxResult.None:
					return eDialogResult.Nothing;
				default:
					return eDialogResult.Nothing;
			}
		}

		public string Name
		{
			get { return "DialogService"; }
		}
	}
}
