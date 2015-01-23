using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using MediaPoint.MVVM.Services;

namespace MediaPoint.VM.ViewInterfaces
{
	public enum PlayerCommand
	{
		Open,
		Play,
		Pause,
		Stop,
		Next,
		Previous,
		Dispose
	}

	public enum MainViewCommand
	{
		Close,
		Minimize,
		Maximize,
		Restore,
        IncreasePanScan,
        DecreasePanScan
	}

	public interface IMainView : IService
	{
        void NotifyDragged();
        DateTime LastDragTime();
		void Hide();
		void Show();
		void Invoke(Action action);
		void DelayedInvoke(Action action, int millisenconds = 100);
		bool ExecuteCommand(MainViewCommand command, object parameter = null);
	    Window GetWindow();
	    void UpdateTaskbarButtons();
        void RefreshUIElements();
	}

	public interface IPlayerView : IService
	{
		bool ExecuteCommand(PlayerCommand command, object parameter = null);
	}

}
