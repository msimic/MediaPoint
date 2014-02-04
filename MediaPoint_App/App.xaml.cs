using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows;
using MediaPoint.Common.MediaFoundation;
using System.Diagnostics;
using MediaPoint.MVVM.Services;
using MediaPoint.VM.ViewInterfaces;
using MediaPoint.VM;
using MediaPoint.App.Themes;

namespace MediaPoint.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			Exit += new ExitEventHandler(App_Exit);
			//Thread preloader = new Thread(PreloadAssemblies);
			//preloader.IsBackground = true;
			//preloader.Priority = ThreadPriority.BelowNormal;
			//preloader.Start();
			//try
			//{
			//    StyleLoader.LoadStyles("MediaPoint", "default", Resources);
			var w = new Window1();
			var dlgsrv = new AppDialogService();
			var b = new System.Windows.Controls.Button();
			var sl = new StyleLoader(Resources);
			ServiceLocator.RegisterService<IStyleLoader>(sl);
			ServiceLocator.RegisterService<IMainView>(w);
			ServiceLocator.RegisterService<IDialogService>(dlgsrv);
			var vm = new Main { Themes = new ObservableCollection<string>(StyleLoader.GetAllStyles()) };
			w.DataContext = vm;
			//sl.LoadStyle("default");
			StyleLoader.CurrentStyleFolder = "default";
			vm.CurrentTheme = StyleLoader.CurrentStyleFolder;
			w.Show();
            
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 2)
            {
                w.StartupFile = args[1];
            }
            

			//}
			//catch (StyleLoader.ThemeException ex)
			//{
			//    MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
			//}			
		}

		void App_Exit(object sender, ExitEventArgs e)
		{
			if (Application.Current != this)
			{
				
			}
		}

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			//MessageBox.Show(e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
		}

		//static void PreloadAssemblies()
		//{
		//    Thread.Sleep(5000);
		//    int count = -1;
		//    Debug.WriteLine("Loading assemblies...");
		//    List<string> done = new List<string>(); // important...
		//    Queue<AssemblyName> queue = new Queue<AssemblyName>();
		//    queue.Enqueue(Assembly.GetEntryAssembly().GetName());
		//    while (queue.Count > 0)
		//    {
		//        AssemblyName an = queue.Dequeue();
		//        if (done.Contains(an.FullName)) continue;
		//        done.Add(an.FullName);
		//        try
		//        {
		//            Assembly loaded = Assembly.Load(an);
		//            Debug.WriteLine("Loaded " + loaded.FullName);
		//            count++;
		//            foreach (AssemblyName next in loaded.GetReferencedAssemblies())
		//            {
		//                queue.Enqueue(next);
		//            }
		//        }
		//        catch { } // not a problem
		//    }
		//    Debug.WriteLine("Assemblies loaded: " + count);
		//}

	}
}
