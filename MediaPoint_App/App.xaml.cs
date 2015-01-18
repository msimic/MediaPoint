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
using Microsoft.VisualBasic.ApplicationServices;
using System.Linq;
using MediaPoint.Interfaces;
using MediaPoint.VM.Services.Model;
using MediaPoint.Common.Services;

namespace MediaPoint.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            // Call the OnStartup event on our base class
            base.OnStartup(e);

            Exit += new ExitEventHandler(App_Exit);
            Thread preloader = new Thread(PreloadAssemblies);
            preloader.IsBackground = true;
            preloader.Priority = ThreadPriority.BelowNormal;
            preloader.Start();
            
            this.Resources = Application.LoadComponent(new Uri("App.xaml", UriKind.RelativeOrAbsolute)) as ResourceDictionary;
            this.Resources.BeginInit();
            this.Resources.MergedDictionaries.Add(Application.LoadComponent(new Uri("/Themes/default/style.xaml", UriKind.RelativeOrAbsolute)) as ResourceDictionary);
            this.Resources.EndInit();
            DispatcherUnhandledException += Application_DispatcherUnhandledException;
            var w = new Window1();
            var dlgsrv = new AppDialogService();
            var b = new System.Windows.Controls.Button();
            var sl = new StyleLoader(Resources);
            ServiceLocator.RegisterService<IStyleLoader>(sl);
            ServiceLocator.RegisterService<IMainView>(w);
            ServiceLocator.RegisterService<IFramePictureProvider>(w);
            ServiceLocator.RegisterService<IInputTeller>(w);
            ServiceLocator.RegisterService<IDialogService>(dlgsrv);
            var vm = new Main { Themes = new ObservableCollection<string>(StyleLoader.GetAllStyles()) };
            ServiceLocator.RegisterService<IPlateProcessor>(vm);
            ServiceLocator.RegisterService<ISettings>(vm);
            ServiceLocator.RegisterService<IMainWindow>(w);
            
            w.DataContext = vm;
            //sl.LoadStyle("default");
            StyleLoader.CurrentStyleFolder = "default";
            vm.CurrentTheme = StyleLoader.CurrentStyleFolder;

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                (MainWindow as Window1).StartupFile = args[1];
            }

            InterceptKeys.Start((MainWindow as Window1));

            w.Show();

        }

        public void Activate(string[] args)
        {
            if (args.Length > 0)
            {
                (MainWindow as Window1).StartupFile = args[0];
            }
            // Reactivate the main window
            MainWindow.Activate();
        }
        
		void App_Exit(object sender, ExitEventArgs e)
		{
			if (Application.Current != this)
			{
				
			}
		}

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
            var ex = e.Exception;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace, "Serious problem occured - App may crash!", MessageBoxButton.OK, MessageBoxImage.Error);
            //this.Shutdown(1);
            e.Handled = true;
		}

        public class SingleInstanceManager : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
        {
            private App _application;
            private System.Collections.ObjectModel.ReadOnlyCollection<string> _commandLine;

            public SingleInstanceManager()
            {
                IsSingleInstance = true;
            }

            protected override bool OnUnhandledException(Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs e)
            {
                var ex = e.Exception;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                MessageBox.Show(Application.Current.MainWindow, ex.Message + Environment.NewLine + ex.StackTrace, "Serious problem occured - App may crash!", MessageBoxButton.OK, MessageBoxImage.Error);
                e.ExitApplication = false;
                return base.OnUnhandledException(e);
            }

            protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
            {
                // First time _application is launched
                _commandLine = eventArgs.CommandLine;
                _application = new App();
                try
                {
                    _application.Run();
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                    }
                    MessageBox.Show(Application.Current.MainWindow, ex.Message + Environment.NewLine + ex.StackTrace, "Serious problem occured - App may crash!", MessageBoxButton.OK, MessageBoxImage.Error);
                    ((ServiceLocator.GetService<IMainWindow>() as Window1).DataContext as Main).ExitCommand.Execute(null);
                    return false;
                }
                ((ServiceLocator.GetService<IMainWindow>() as Window1).DataContext as Main).ExitCommand.Execute(null);
                return false;
            }

            protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
            {
                // Subsequent launches
                base.OnStartupNextInstance(eventArgs);
                _commandLine = eventArgs.CommandLine;
                _application.Activate(_commandLine.ToArray());
            }
        }

        static void PreloadAssemblies()
        {
            Thread.Sleep(5000);
            int count = -1;
            Debug.WriteLine("Loading assemblies...");
            List<string> done = new List<string>(); // important...
            Queue<AssemblyName> queue = new Queue<AssemblyName>();
            queue.Enqueue(Assembly.GetEntryAssembly().GetName());
            int level = 0;
            while (queue.Count > 0)
            {
                if (level > 3) break;
                AssemblyName an = queue.Dequeue();
                if (done.Contains(an.FullName)) continue;
                done.Add(an.FullName);
                try
                {
                    Assembly loaded = Assembly.Load(an);
                    Debug.WriteLine("Loaded " + loaded.FullName);
                    level++;
                    count++;
                    foreach (AssemblyName next in loaded.GetReferencedAssemblies())
                    {
                        if (!queue.Contains(next))
                        {
                            queue.Enqueue(next);
                        }
                    }
                }
                catch { } // not a problem
            }
            Debug.WriteLine("Assemblies loaded: " + count);
        }

	}

    public class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
            MediaPoint.App.App.SingleInstanceManager manager = new MediaPoint.App.App.SingleInstanceManager();
            manager.Run(args);
        }
    }
}
