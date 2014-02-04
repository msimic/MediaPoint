using System;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;

namespace MediaPoint.MVVM
{
	public class Command : ICommand
	{
		public string Name { get; set; }

		private readonly Action<object> m_Execute;
		private readonly Predicate<object> m_CanExecute;

		protected Command()
		{
		}

		public Command(String Name)
		{
			this.Name = Name;
		}

		public Command(String Name, Action<object> Execute)
		{
			this.Name = Name;
			m_Execute = Execute;
		}

		public Command(Action<object> Execute, Predicate<object> CanExecute)
		{
			this.Name = "<Noname>";
			m_Execute = Execute;
			m_CanExecute = CanExecute;
		}

		public Command(String Name, Action<object> Execute, Predicate<object> CanExecute)
		{
			this.Name = Name;
			m_Execute = Execute;
			m_CanExecute = CanExecute;
		}

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public virtual bool CanExecute(object parameter)
		{
			if (m_CanExecute != null)
				return m_CanExecute(parameter);
			else
				return true;
		}

		public virtual void Execute(object parameter)
		{
			if (m_Execute != null)
				m_Execute(parameter);
			else
				Debug.WriteLine("Command \"" + this.Name + "\" not implemented.");
		}
	}
}