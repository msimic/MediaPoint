using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using MediaPoint.Common.DirectShow.MediaPlayers;
using MediaPoint.Common.Interfaces;
using MediaPoint.Common.ScreenSaver;
using MediaPoint.MVVM;
using System.Windows.Input;
using MediaPoint.Subtitles.Logic;
using MediaPoint.VM.Model;
using MediaPoint.VM.ViewInterfaces;
using MediaPoint.MVVM.Services;
using System.Threading;
using MediaPoint.Common.Helpers;

namespace MediaPoint.VM
{
	public class Main : ViewModel
	{
        internal static class NativeMethods
        {
            // Import SetThreadExecutionState Win32 API and necessary flags
            [DllImport("kernel32.dll")]
            public static extern uint SetThreadExecutionState(uint esFlags);
            public const uint ES_CONTINUOUS = 0x80000000;
            public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        }

		#region Members
		private readonly IMainView _view;
		private bool _exiting;
		private IDialogService _dlg;
		private IEnumerable<MediaPoint.Subtitles.Logic.Paragraph> _currentSubs = new Paragraph[0];
        private readonly uint _previousExecutionState;
		#endregion

		#region Ctor
		public Main()
		{
            Equalizer = new Equalizer();
		    SubtitleColor = Colors.White;
		    AutoLoadSubtitles = true;
            SubtitleLanguages = new List<ITag>() {new Tag() {Id = "eng", Name="English"}};
            AllSubtitleServices = new List<ITag>() { new Tag() { Id = "Podnapisi", Name = "Podnapisi" }, new Tag() { Id = "OpenSubtitles", Name = "OpenSubtitles" } };
            SubtitleServices = new List<ITag>() { new Tag() { Id = "Podnapisi", Name = "Podnapisi" }, new Tag() { Id = "OpenSubtitles", Name = "OpenSubtitles" } };
            AllLanguages = new List<ITag> { new Tag("bos", "Bosnian"),
                                            new Tag("slv", "Slovenian"),
                                            new Tag("hrv", "Croatian"),
                                            new Tag("srp", "Serbian"),
                                            new Tag("eng", "English"),
                                            new Tag("spa", "Spanish"),
                                            new Tag("fre", "French"),
                                            new Tag("gre", "Greek"),
                                            new Tag("ger", "German"),
                                            new Tag("rus", "Russian"),
                                            new Tag("chi", "Chinese"),
                                            new Tag("por", "Portuguese"),
                                            new Tag("dut", "Dutch"),
                                            new Tag("ita", "Italian"),
                                            new Tag("rum", "Romanian"),
                                            new Tag("cze", "Czech"),
                                            new Tag("ara", "Arabic"),
                                            new Tag("pol", "Polish"),
                                            new Tag("tur", "Turkish"),
                                            new Tag("swe", "Swedish"),
                                            new Tag("fin", "Finnish"),
                                            new Tag("hun", "Hungarian"),
                                            new Tag("dan", "Danish"),
                                            new Tag("heb", "Hebrew"),
                                            new Tag("est", "Estonian"),
                                            new Tag("slo", "Slovak"),
                                            new Tag("ind", "Indonesian"),
                                            new Tag("per", "Persian"),
                                            new Tag("bul", "Bulgarian"),
                                            new Tag("jpn", "Japanese"),
                                            new Tag("alb", "Albanian"),
                                            new Tag("bel", "Belarusian"),
                                            new Tag("hin", "Hindi"),
                                            new Tag("gle", "Irish"),
                                            new Tag("ice", "Icelandic"),
                                            new Tag("cat", "Catalan"),
                                            new Tag("kor", "Korean"),
                                            new Tag("lav", "Latvian"),
                                            new Tag("lit", "Lithuanian"),
                                            new Tag("mac", "Macedonian"),
                                            new Tag("nor", "Norwegian"),
                                            new Tag("tha", "Thai"),
                                            new Tag("ukr", "Ukrainian"),
                                            new Tag("vie", "Vietnamese")};

            var ISO839_1 = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            var l = LanguageISOTranslator.ISO839_1[ISO839_1];
            if (l != null)
            {
                var l2 = AllLanguages.FirstOrDefault(o => o.Id == l.ISO639_2B);
                if (l2 != null)
                {
                    SubtitleLanguages.Insert(0, l2);
                }
            }

            _previousExecutionState = NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
			_view = ServiceLocator.GetService<IMainView>();
			_dlg = ServiceLocator.GetService<IDialogService>();
			var allEnc = new List<EncodingInfo>(Encoding.GetEncodings());
		    var enc = allEnc[allEnc.FindIndex(e => e.CodePage == Encoding.Default.CodePage)];
            allEnc.Remove(enc);
			allEnc.Insert(0, enc);
			var fonts = new List<FontFamily>(Fonts.SystemFontFamilies);
			if (!fonts.Any(f => f.ToString().ToLowerInvariant().Contains("buxton sketch")))
			{
				fonts.Add(new FontFamily(new Uri("pack://application:,,,/MediaPoint;component/Resources/BuxtonSketch.ttf", UriKind.RelativeOrAbsolute), "Buxton Sketch"));
			}

		    string fontCacheFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		    fontCacheFile = Path.Combine(fontCacheFile, Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location));
            fontCacheFile = Path.Combine(fontCacheFile, "font.cache");
            PreBuildFonts(fonts.ToArray(), fontCacheFile);
		    SubtitleSize = 20;
            Encodings = Enum.GetValues(typeof(FontCharSet)).Cast<FontCharSet>().ToArray();
			Player = new Player(this);
		    SubEncoding = Encodings[1];

            var updateThread = new Thread((() =>
                                {
                                    while (!_exiting)
                                    {
                                        try
                                        {
                                            // update time
                                            Time = string.Format("{0:00}:{1:00}", DateTime.Now.Hour, DateTime.Now.Minute);
                                            // periodically ping screensaver to not happen during playback
                                            ScreenSaver.SetScreenSaverActive(ScreenSaver.GetScreenSaverActive());
                                            Thread.Sleep(5000);
                                        }
                                        catch
                                        {
                                            Thread.Sleep(10000);
                                        }
                                    }
                                })) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
            updateThread.Start();
		}
		#endregion

		#region "Properties"

        public bool ShowVisualizations
        {
            get { return GetValue(() => ShowVisualizations); }
            set { SetValue(() => ShowVisualizations, value); }
        }

        public bool ShowEqualizer
        {
            get { return GetValue(() => ShowEqualizer); }
            set { SetValue(() => ShowEqualizer, value); }
        }

        public Equalizer Equalizer
        {
            get { return GetValue(() => Equalizer); }
            set { SetValue(() => Equalizer, value); }
        }

        public FontCharSet SubEncoding
        {
            get { return GetValue(() => SubEncoding); }
            set { SetValue(() => SubEncoding, value); }
        }

        public ObservableCollection<string> AudioRenderers
        {
            get { return GetValue(() => AudioRenderers); }
            set { SetValue(() => AudioRenderers, value); }
        }

        public string AudioRenderer
        {
            get { return GetValue(() => AudioRenderer); }
            set { SetValue(() => AudioRenderer, value); }
        }

		public ObservableCollection<string>	Themes
		{
			get { return GetValue(() => Themes); }
			set { SetValue(() => Themes, value); }
		}

		public Player Player
		{
			get { return GetValue(() => Player); }
			set { SetValue(() => Player, value); }
		}

		public bool IsHidden
		{
			get { return GetValue<bool>(() => IsHidden); }
			set { SetValue<bool>(() => IsHidden, value); }
		}

		public bool IsOptionsVisible
		{
			get { return GetValue(() => IsOptionsVisible); }
			set
			{
				if (SetValue(() => IsOptionsVisible, value))
				{
					//_dlg.ShowMessageBox("Should show options", "", eMessageBoxType.Ok, eMessageBoxIcon.Info);
				}
			}
		}

        public bool AutoLoadSubtitles
        {
            get { return GetValue(() => AutoLoadSubtitles); }
            set { SetValue(() => AutoLoadSubtitles, value); }
        }

        public string Time
        {
            get { return GetValue(() => Time); }
            set { SetValue(() => Time, value); }
        }

		public bool IsMaximized
		{
			get { return GetValue(() => IsMaximized); }
			set { SetValue(() => IsMaximized, value); }
		}

		public string CurrentTheme
		{
			get { return GetValue(() => CurrentTheme); }
			set {
				if (SetValue(() => CurrentTheme, value))
				{
					SetSkin(value);
				}
			}
		}

        public List<ITag> AllLanguages
        {
            get { return GetValue(() => AllLanguages); }
            set { SetValue(() => AllLanguages, value); }
        }

        public List<ITag> SubtitleLanguages
        {
            get { return GetValue(() => SubtitleLanguages); }
            set { SetValue(() => SubtitleLanguages, value); }
        }

        public List<ITag> AllSubtitleServices
        {
            get { return GetValue(() => AllSubtitleServices); }
            set { SetValue(() => AllSubtitleServices, value); }
        }

        public List<ITag> SubtitleServices
        {
            get { return GetValue(() => SubtitleServices); }
            set { SetValue(() => SubtitleServices, value); }
        }

        public int SubtitleSize
        {
            get { return GetValue(() => SubtitleSize); }
            set { SetValue(() => SubtitleSize, value); }
        }

		public FontObject SubtitleFont
		{
			get { return GetValue(() => SubtitleFont); }
			set { SetValue(() => SubtitleFont, value); }
		}

        public Color SubtitleColor
		{
            get { return GetValue(() => SubtitleColor); }
            set { SetValue(() => SubtitleColor, value); }
		}

		public FontObject[] AllFonts
		{
			get { return GetValue(() => AllFonts); }
			set { SetValue(() => AllFonts, value); }
		}

		public IEnumerable<Paragraph> CurrentSubtitle
		{
			get { return GetValue(() => CurrentSubtitle); }
			set { SetValue(() => CurrentSubtitle, value); }
		}

        public FontCharSet[] Encodings
		{
			get { return GetValue(() => Encodings); }
			set { SetValue(() => Encodings, value); }
		}

		#endregion

		#region Commands

		public void SetSkin(string skin)
		{
			Uri src = null;
			long pos = 0;
			bool isPlaying = false;
			try
			{
				if (Player != null)
				{
					src = Player.Source;
					pos = Player.MediaPosition;
					isPlaying = Player.IsPlaying;
				}
				_view.Hide();
				IsOptionsVisible = false;
				var sl = ServiceLocator.GetService<IStyleLoader>();
				sl.LoadStyle(skin);
			}
			catch (Exception ex)
			{
				_dlg.ShowMessageBox(ex.Message + Environment.NewLine + ex.StackTrace, "Exception", eMessageBoxType.Ok, eMessageBoxIcon.Warning);
			}		
			finally
			{
				_view.Show();
				if (Player != null && src != null)
				{
					Player.Source = null;
					Player.WorkQueue.Enqueue(() =>
					                         	{
					                         		if (isPlaying)
					                         		{
					                         			_view.Invoke(() =>
					                         			             	{
					                         			             		Player.Open(src);
					                         			             		Player.MediaPosition = pos;
					                         			             		Player.View.ExecuteCommand(PlayerCommand.Play);
					                         			             	});
					                         		}
					                         	});
				}
			}
		}

		private ICommand _openCommand; 
		public ICommand OpenCommand
		{
			get
			{
				if (_openCommand == null) _openCommand = new Command(o =>
				{
					Player.OpenCommand.Execute(o);

				}, can => true);

				return _openCommand;
			}
		}


        public ICommand ShowVisualizationsCommand
        {
            get
            {
                return new Command(o =>
                {
                    if (!string.IsNullOrEmpty(o as string) && (string)o == "hide")
                        ShowVisualizations = false;
                    else
                        ShowVisualizations = !ShowVisualizations;
                }, can =>
                {
                    return true;
                });
            }
        }

        public ICommand ShowEqualizerCommand
        {
            get
            {
                return new Command(o =>
                {
                    if (!string.IsNullOrEmpty(o as string) && (string)o == "hide")
                        ShowEqualizer = false;
                    else
                        ShowEqualizer = !ShowEqualizer;
                }, can =>
                {
                    return true;
                });
            }
        }

		public ICommand MinimizeCommand
		{
			get
			{
				return new Command(o =>
				                   	{
				                   		IsHidden = true;
				                   		_view.ExecuteCommand(MainViewCommand.Minimize);
				                   	}, can => true);
			}
		}

		public ICommand AboutCommand
		{
			get
			{
				return new Command(o =>
				                   	{
				                   		_dlg.ShowMessageBox(@"Cannot loose time in writing something
meaningful :)

MediaPoint is made by Mars

Enjoy all the Bugs :D", "About MediaPoint", eMessageBoxType.Ok, eMessageBoxIcon.Info);
				                   	}, can => true);
			}
		}

		public ICommand RestoreCommand
		{
			get
			{
				return new Command(o =>
				{
					IsHidden = false;
					_view.ExecuteCommand(MainViewCommand.Restore);
				}, can => true);
			}
		}

        public ICommand SetAudioRendererCommand
        {
            get
            {
                if (GetValue(() => SetAudioRendererCommand) == null)
                {
                    SetValue(() => SetAudioRendererCommand, new Command(o =>
                                                              {
                                                                  AudioRenderer = (string) o;
                                                              }, can => true));
                }
                return GetValue(() => SetAudioRendererCommand);
            }
            set { SetValue(() => SetAudioRendererCommand, value); }
        }

		public ICommand WindowStateCommand
		{
			get
			{
				return new Command(o =>
				{
					if (!IsMaximized)
					{
						_view.ExecuteCommand(MainViewCommand.Maximize);
						IsMaximized = true;
						IsHidden = false;
					}
					else
					{
						_view.ExecuteCommand(MainViewCommand.Restore);
						IsMaximized = false;
						IsHidden = false;
					}
				}, can => true);
			}
		}

		public ICommand OptionsCommand
		{
			get
			{
				return new Command(o => ToggleOptions(), can => true);
			}
		}

		public ICommand ExitCommand
		{
			get
			{
				return new Command(o => Exit(), can => true);
			}
		}	

		#endregion

		#region Methods
		
        private void PreBuildFonts(FontFamily[] fonts, string cacheFilePath)
        {
            var b = new BackgroundWorker();

            b.DoWork += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("Preloading fonts");
                if (!Directory.Exists(Path.GetDirectoryName(cacheFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFilePath));
                }

                if (File.Exists(cacheFilePath))
                {
                    try
                    {
                        using (var fs = new FileStream(cacheFilePath, FileMode.Open))
                            FontObject.ReadDictionaryFromStream(fs);
                    }
                    catch
                    {
                        File.Delete(cacheFilePath);
                    }
                }
            
                var okFonts = new List<FontObject>();
                foreach (FontFamily font in fonts)
                {
                    var f = new FontObject(font);
                    if (f.GetStyledFontGeometryUsingCache() != null)
                    {
                        okFonts.Add(f);
                    }
                }

                if (!File.Exists(cacheFilePath) || okFonts.Count != FontObject.GeometryCount)
                {
                    using (var fs = new FileStream(cacheFilePath, FileMode.Create))
                        FontObject.SaveDictionaryToStream(fs);
                }

                args.Result = okFonts.ToArray();

            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                AllFonts = (FontObject[]) args.Result;
                System.Diagnostics.Debug.WriteLine("Fonts preloaded");
                SubtitleFont = AllFonts.First(f => f.Font.ToString().ToLowerInvariant().Contains("impact"));
            };
            b.RunWorkerAsync();
        }

		public void ToggleOptions()
		{
			IsOptionsVisible = !IsOptionsVisible;
		}

		private void Exit()
		{
			_exiting = true;
			_view.ExecuteCommand(MainViewCommand.Close);
		    NativeMethods.SetThreadExecutionState(_previousExecutionState);
		}

		#endregion

	}
}
