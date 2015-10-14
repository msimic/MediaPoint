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
using MediaPoint.VM.Model;
using SubtitleDownloader.Core;

namespace MediaPoint.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
    public partial class App : Application, ISubtitleDownloaderRegistrator
	{
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            ServiceLocator.RegisterService<ISubtitleDownloaderRegistrator>(this);

            InitializationSequence = new Dictionary<object, Action>();

            DispatcherUnhandledException += Application_DispatcherUnhandledException;
            
            // Call the OnStartup event on our base class
            base.OnStartup(e);

            var v = this.GetType().Assembly.GetName().Version;
            
            string preproduction = " ?";

            if (v.Build == 0)
            {
                preproduction = "";
            }
            else if (v.Build % 2 != 0)
            {
                preproduction = " αlpha" + (v.Build + 1) / 2 + " (rev." + v.Revision + ")";
            }
            else if (v.Build % 2 == 0)
            {
                preproduction = " βeta" + v.Build / 2 + " (rev." + v.Revision + ")";
            }

            preproduction += " (unstable)";

            this.Properties["Version"] = string.Format("v{0}.{1}{2}", v.Major, v.Minor, preproduction);
            this.Properties["VersionShort"] = string.Format("v{0}.{1}", v.Major, v.Minor);

            Exit += new ExitEventHandler(App_Exit);
            Thread preloader = new Thread(PreloadAssemblies);
            preloader.IsBackground = true;
            preloader.Priority = ThreadPriority.BelowNormal;
            preloader.Start();

            this.Resources = Application.LoadComponent(new Uri("App.xaml", UriKind.RelativeOrAbsolute)) as ResourceDictionary;

            var w = new Window1();
            ServiceLocator.RegisterService<IMainView>(w);
            var dlgsrv = new AppDialogService();
            ServiceLocator.RegisterService<IDialogService>(dlgsrv);
            ServiceLocator.RegisterService<IMainWindow>(w);

            var vm = new Main { Themes = new ObservableCollection<ThemeInfo>(StyleLoader.GetAllStyles()) };
            w.DataContext = vm;
            ServiceLocator.RegisterOverrideService<IKeyboardHandler>((IKeyboardHandler)vm);
            ServiceLocator.RegisterOverrideService<IActionExecutor>((IActionExecutor)vm);
            
            this.Resources.BeginInit();

            var sl = new StyleLoader(Resources);
            var startTheme = new ThemeInfo { Path = "default" };
            startTheme = sl.LoadStyle(startTheme);
            StyleLoader.CurrentStyleFolder = startTheme.Path;
            vm.CurrentTheme = vm.Themes.FirstOrDefault(t => t.Path == startTheme.Path);

            //this.Resources.MergedDictionaries.Add(Application.LoadComponent(new Uri("/Themes/default/style.xaml", UriKind.RelativeOrAbsolute)) as ResourceDictionary);
            this.Resources.EndInit();

            sl.PerformInit = true;

            var b = new System.Windows.Controls.Button();
            ServiceLocator.RegisterService<IStyleLoader>(sl);
            ServiceLocator.RegisterService<IFramePictureProvider>(w);
            ServiceLocator.RegisterService<IInputTeller>(w);
            ServiceLocator.RegisterService<IPlateProcessor>(vm);
            ServiceLocator.RegisterService<ISettings>(vm);
            
            //sl.LoadStyle("default");
            
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                (MainWindow as Window1).StartupFile = args[1];
            }

            InterceptKeys.Start((MainWindow as Window1));

            w.Show();

            Dispatcher.BeginInvoke((Action)(() =>
            {
                Initialized = true;

                foreach (var key in InitializationSequence.Keys)
                {
                    InitializationSequence[key]();
                }
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        public bool Initialized { get; set; }

        public Dictionary<object, Action> InitializationSequence { get; set; }


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


        public void RegisterDownloader(SubtitleDownloader.Core.ISubtitleDownloader downloader)
        {
            var member = typeof(SubtitleDownloaderFactory).GetField("DownloaderInstances", BindingFlags.Static | BindingFlags.NonPublic);
            var val = member.GetValue(null);
            var dict = val as Dictionary<string, ISubtitleDownloader>;
            dict[downloader.GetName()] = downloader;
        }

        public string Name
        {
            get { throw new NotImplementedException(); }
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
