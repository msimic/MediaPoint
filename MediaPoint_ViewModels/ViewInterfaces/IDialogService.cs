using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPoint.MVVM;
using MediaPoint.MVVM.Services;

namespace MediaPoint.VM.ViewInterfaces
{
	public enum eDialogResult {
		OK,
		Cancel,
		Yes,
		No,
		Abort,
		Retry,
		Continue,
		Nothing
	}

	public enum eMessageBoxType {
		OkCancel,
		YesNo,
		Ok,
		YesNoCancel
	}

	public enum eMessageBoxIcon {
		Info,
		Warning,
		Error,
		None,
		Question
	}

	public interface IDialogService : IService
	{
		VM ShowDialog<VM>(bool modal, string title, double width = 0, double height = 0) where VM : ViewModel;
		bool? ShowDialog<VM>(VM vm, bool modal, string title, double width = 0, double height = 0) where VM : ViewModel;
		eDialogResult ShowMessageBox(string text, string title, eMessageBoxType type, eMessageBoxIcon icon);
		Uri ShowOpenUriDialog(string startpath, string filter);
	}
}
