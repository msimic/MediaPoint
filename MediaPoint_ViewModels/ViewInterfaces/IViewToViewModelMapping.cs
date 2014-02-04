using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPoint.MVVM;

namespace MediaPoint.VM.ViewInterfaces
{
	public interface IViewModelToViewMapping
	{
		Type ViewModel { get; }
		Type View { get; }
		ViewModel ShowDialog(bool isModal, string title, double width = 0, double height = 0);
		bool? ShowDialog(ViewModel vm, bool isModal, string title, double width = 0, double height = 0);
	}
}
