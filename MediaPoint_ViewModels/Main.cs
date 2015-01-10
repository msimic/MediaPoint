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
using System.Reactive.Linq;
using MediaPoint.Interfaces;
using System.Windows;
using MediaPoint.Helpers;
using MediaPoint.Common.TaskbarNotification.Interop;
using System.Windows.Media.Imaging;
using MediaPoint.VM.Services.Model;

namespace MediaPoint.VM
{
    public class Main : ViewModel, IKeyboardHandler, IPlateProcessor, ISettings
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
            Plates = new ObservableCollection<Plate>();
            SubtitleMinScore = 0.55;

            Observable.Interval(TimeSpan.FromSeconds(1.5)).Subscribe(i =>
            {
                if (ShowPlate == true)
                {
                    ShowPlate = false;
                }
            });

            ServiceLocator.RegisterOverrideService<IKeyboardHandler>((IKeyboardHandler)this);
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
            Playlist = new Playlist(Player);
		    SubEncoding = Encodings[1];
            InitShortcuts();
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

        private void InitShortcuts()
        {
            InitActions();

            KeyboardShortcuts = new ObservableCollection<KeyboardShortcut>();

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Framestep",
                Key = Key.Decimal,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Play/Pause",
                Key = Key.Space,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Subs Delay +",
                Key = Key.Right,
                Control = true,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Subs Delay -",
                Key = Key.Left,
                Control = true,
                Shift = false,
                Alt = false
            });


            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Previous Track",
                Key = Key.PageUp,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Next Track",
                Key = Key.PageDown,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Seek Back 1%",
                Key = Key.Left,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Seek Forward 1%",
                Key = Key.Right,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Increase Subtitle Size",
                Key = Key.Up,
                Control = false,
                Shift = true,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Decrease Subtitle Size",
                Key = Key.Down,
                Control = false,
                Shift = true,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Increase Volume",
                Key = Key.Up,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Decrease Volume",
                Key = Key.Down,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Go Fullscreen",
                Key = Key.F,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Exit Fullscreen",
                Key = Key.Escape,
                Control = false,
                Shift = false,
                Alt = false
            });

            KeyboardShortcuts.Add(new KeyboardShortcut
            {
                ActionId = "Save Video Screenshot",
                Key = Key.PrintScreen,
                Control = false,
                Shift = false,
                Alt = false
            });

            foreach (var sc in KeyboardShortcuts)
            {
                var ac = PlayerActions.FirstOrDefault(pa => pa.ActionId == sc.ActionId);
                if (ac != null)
                {
                    ac.Shortcut = sc;
                }
            }
        }

        private void InitActions()
        {
            PlayerActions = new ObservableCollection<PlayerAction>();

            var frameStep = new PlayerAction
            {
                ActionId = "Framestep",
                Action = new Action(() =>
                {
                    if (Player.IsPlaying == false && Player.IsPaused == false)
                    {
                        return;
                    }

                    Player.FrameStep();
                })
            };
            PlayerActions.Add(frameStep);

            var playPause = new PlayerAction
            {
                ActionId = "Play/Pause",
                Action = new Action(() =>
                {
                    if (Player.IsPaused || Player.Source == null)
                        Player.Play();
                    else if (Player.Source != null)
                        Player.Pause();
                })
            };
            PlayerActions.Add(playPause);

            var subDelayRight = new PlayerAction
            {
                ActionId = "Subs Delay +",
                Action = new Action(() =>
                {
                    SubtitleDelay += 200;
                })
            };
            PlayerActions.Add(subDelayRight);

            var subDelayLeft = new PlayerAction
            {
                ActionId = "Subs Delay -",
                Action = new Action(() =>
                {
                    SubtitleDelay -= 200;
                })
            };
            PlayerActions.Add(subDelayLeft);

            var seekLeft = new PlayerAction
            {
                ActionId = "Seek Back 1%",
                Action = new Action(() =>
                {
                    Player.MediaPosition = Math.Max(0, Player.MediaPosition - (Player.MediaDuration / 100));
                    ShowOsdMessage("Seeking: " + Player.Position);
                })
            };
            PlayerActions.Add(seekLeft);

            var seekRigth = new PlayerAction
            {
                ActionId = "Seek Forward 1%",
                Action = new Action(() =>
                {
                    Player.MediaPosition = Math.Min(Player.MediaDuration, Player.MediaPosition + (Player.MediaDuration / 100));
                    ShowOsdMessage("Seeking: " + Player.Position);
                })
            };
            PlayerActions.Add(seekRigth);

            var incSub = new PlayerAction
            {
                ActionId = "Increase Subtitle Size",
                Action = new Action(() =>
                {
                    if (SubtitleSize < 150) SubtitleSize += 2;
                })
            };
            PlayerActions.Add(incSub);

            var decSub = new PlayerAction
            {
                ActionId = "Decrease Subtitle Size",
                Action = new Action(() =>
                {
                    if (SubtitleSize > 6) SubtitleSize -= 2;
                })
            };
            PlayerActions.Add(decSub);

            var incVol = new PlayerAction
            {
                ActionId = "Increase Volume",
                Action = new Action(() =>
                {
                    Player.Volume = Math.Min(1, Player.Volume + .1);
                })
            };
            PlayerActions.Add(incVol);

            var decVol = new PlayerAction
            {
                ActionId = "Decrease Volume",
                Action = new Action(() =>
                {
                    Player.Volume = Math.Max(0, Player.Volume - .1);
                })
            };
            PlayerActions.Add(decVol);

            var nextTrack = new PlayerAction
            {
                ActionId = "Next Track",
                Action = new Action(() =>
                {
                    if (Playlist.NextCommand.CanExecute(null)) Playlist.NextCommand.Execute(null);
                })
            };
            PlayerActions.Add(nextTrack);

            var prevTrack = new PlayerAction
            {
                ActionId = "Previous Track",
                Action = new Action(() =>
                {
                    if (Playlist.PreviousCommand.CanExecute(null)) Playlist.PreviousCommand.Execute(null);
                })
            };
            PlayerActions.Add(prevTrack);

            var fs = new PlayerAction
            {
                ActionId = "Go Fullscreen",
                Action = new Action(() =>
                {
                    _view.ExecuteCommand(MainViewCommand.Maximize);
                })
            };
            PlayerActions.Add(fs);

            var exFs = new PlayerAction
            {
                ActionId = "Exit Fullscreen",
                Action = new Action(() =>
                {
                    _view.ExecuteCommand(MainViewCommand.Restore);
                })
            };
            PlayerActions.Add(exFs);

            var ss = new PlayerAction
            {
                ActionId = "Save Video Screenshot",
                Action = new Action(() =>
                {
                    var bs = ServiceLocator.GetService<IFramePictureProvider>().GetBitmapOfVideoElement();
                    if (bs != null)
                    {
                        Microsoft.Win32.SaveFileDialog sf = new Microsoft.Win32.SaveFileDialog();
                        sf.AddExtension = true;
                        sf.DefaultExt = ".png";
                        sf.Filter = "Portable Network Graphics (*.png)|*.png|All Files|*.*";
                        sf.FilterIndex = 0;
                        sf.FileName = Path.GetFileNameWithoutExtension(Player.SourceFileName) + ".png";
                        if (true == sf.ShowDialog(ServiceLocator.GetService<IMainView>().GetWindow()))
                        {
                            SaveImageToFile(bs, sf.FileName);
                        }
                    }
                })
            };
            PlayerActions.Add(ss);
            
            
        }

        public static void SaveImageToFile(BitmapSource image, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }

		#endregion

		#region "Properties"

        public ObservableCollection<KeyboardShortcut> KeyboardShortcuts
        {
            get { return GetValue(() => KeyboardShortcuts); }
            set { SetValue(() => KeyboardShortcuts, value); }
        }

        public List<string> PlayerActionNames
        {
            get
            {
                return PlayerActions.Select(pa => pa.ActionId).ToList();
            }
        }

        public ObservableCollection<PlayerAction> PlayerActions
        {
            get { return GetValue(() => PlayerActions); }
            set { SetValue(() => PlayerActions, value); }
        }

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

        public bool ShowPlaylist
        {
            get { return GetValue(() => ShowPlaylist); }
            set { SetValue(() => ShowPlaylist, value); }
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

        public Playlist Playlist
        {
            get { return GetValue(() => Playlist); }
            set { SetValue(() => Playlist, value); }
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
                if (value == null) return;
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
            set
            {
                if (SetValue(() => SubtitleSize, value))
                {
                    ShowOsdMessage("Subtitle size: " + value);
                }
            }
        }

        public double SubtitleMinScore
        {
            get { return GetValue(() => SubtitleMinScore); }
            set
            {
                SetValue(() => SubtitleMinScore, value);
            }
        }

        public int SubtitleDelay
        {
            get { return GetValue(() => SubtitleDelay); }
            set
            {
                if (SetValue(() => SubtitleDelay, value))
                {
                    ShowOsdMessage("Subtitle delay: " + value);
                }
            }
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

        public FontCharSet[] Encodings
		{
			get { return GetValue(() => Encodings); }
			set { SetValue(() => Encodings, value); }
		}

        public string OSDMessage
        {
            get { return GetValue(() => OSDMessage); }
            set { SetValue(() => OSDMessage, value); }
        }

		#endregion

		#region Commands

		public void SetSkin(string skin)
		{
			try
			{
				if (Player != null)
				{
                    Player.ForceStop();
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

        public ICommand ShowPlaylistCommand
        {
            get
            {
                return new Command(o =>
                {
                    if (!string.IsNullOrEmpty(o as string) && (string)o == "hide")
                        ShowPlaylist = false;
                    else
                        ShowPlaylist = !ShowPlaylist;
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
                    try
                    {
                        var f = new FontObject(font);
                        if (f.GetStyledFontGeometryUsingCache() != null)
                        {
                            okFonts.Add(f);
                        }
                    }
                    catch { }
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

        public void ShowOsdMessage(string message)
        {
            OSDMessage = message;
            Observable.Return(1).Delay(TimeSpan.FromSeconds(2)).Subscribe(i =>
            {
                if (OSDMessage == message) OSDMessage = null;
            });
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


        public bool HandleKey(Key key, bool isControl, bool isAlt, bool isShift, bool isExternal)
        {
            var shortcut = KeyboardShortcuts.FirstOrDefault(ks => ks.Key == key &&
                ks.Control == isControl &&
                ks.Alt == isAlt &&
                ks.Shift == isShift &&
                ks.External == isExternal);

            if (shortcut != null)
            {
                return shortcut.Execute(PlayerActions);
            }

            return false;
        }

        public string Name
        {
            get { return "MainVM"; }
        }

        DateTime _lastPlateTime = DateTime.Now;

        public double PlateLeft
        {
            get { return GetValue(() => PlateLeft); }
            set { SetValue(() => PlateLeft, value); }
        }

        public double PlateTop
        {
            get { return GetValue(() => PlateTop); }
            set { SetValue(() => PlateTop, value); }
        }

        public double PlateWidth
        {
            get { return GetValue(() => PlateWidth); }
            set { SetValue(() => PlateWidth, value); }
        }

        public double PlateHeight
        {
            get { return GetValue(() => PlateHeight); }
            set { SetValue(() => PlateHeight, value); }
        }

        public double PlateAngle
        {
            get { return GetValue(() => PlateAngle); }
            set { SetValue(() => PlateAngle, value); }
        }

        public bool ShowPlate
        {
            get { return GetValue(() => ShowPlate); }
            set { SetValue(() => ShowPlate, value); }
        }

        Plate IsFuzzyMatch(string text, Rect rect)
        {
            if (Plates.Count == 0) return null;

            for (int i = Plates.Count - 1; i >= Math.Max(0, Plates.Count - 4); i--)
            {
                if ((DateTime.Now - Plates[i].time) > TimeSpan.FromSeconds(15)) continue;

                bool textMatch = Levenshtein.Compare(Plates[i].Text ?? "", text ?? "") <= 2;

                if (textMatch)
                {
                    return Plates[i];
                }

                if ((DateTime.Now - Plates[i].time) < TimeSpan.FromSeconds(1.5)) continue;
                
                Rect r1 = Plates[i].AsRect();
                bool rectmatch = r1.IntersectsWith(rect);

                if (rectmatch)
                {
                    return Plates[i];
                }
            }

            return null;
        }

        public class Plate : ViewModel
        {
            public int Left;
            public int Top;
            public int Width;
            public int Height;
            public double Angle;
            public string Text
            {
                get { return GetValue(() => Text); }
                set { SetValue(() => Text, value); }
            }
            public DateTime time;
            public int Confidence
            {
                get { return GetValue(() => Confidence); }
                set { SetValue(() => Confidence, value); }
            }
            public Rect AsRect()
            {
                return new Rect(Left, Top, Width, Height);
            }
        }

        public Plate SelectedPlate
        {
            get { return GetValue(() => SelectedPlate); }
            set { SetValue(() => SelectedPlate, value); }
        }

        public ObservableCollection<Plate> Plates
        {
            get { return GetValue(() => Plates); }
            set { SetValue(() => Plates, value); }
        }

        public void ProcessPlate(string text, int left, int top, int right, int bottom, double angle, int confidence)
        {
            if (confidence < 800) return;

            var chars = text.ToCharArray().ToList();

            if (chars[1] == 'I' && chars[2] == 'J')
            {
                chars[1] = 'U';
                chars.RemoveAt(2);
            }

            for (int i = 0; i < 1; i++)
            {
                if (chars[i] == '0')
                    chars[i] = 'O';
                if (chars[i] == '1')
                    chars[i] = 'I';
            }
            for (int i = chars.Count - 1; i >= chars.Count - 2; i--)
            {
                if (chars[i] == '0')
                    chars[i] = 'O';
                if (chars[i] == '1')
                    chars[i] = 'I';
            }

            text = new string(chars.ToArray());
            var rect = new Rect(left, top, right - left, bottom - top);

            Plate match;

            if ((match = IsFuzzyMatch(text, rect)) != null)
            {
                UpdatePlate(match, rect, angle, text, confidence);
                return;
            }

            if (match == null)
                SetPlate(text, confidence, rect, angle);
        }

        private void SetPlate(string text, int confidence, Rect rect, double angle)
        {
            Plate p = new Plate();
            p.Text = text;
            p.time = DateTime.Now;

            Plates.Add(p);

            UpdatePlate(p, rect, angle, text, confidence);

            ShowOsdMessage("Plate: " + text + " with " + confidence / 10 + "% confidence");
        }

        private void UpdatePlate(Plate plate, Rect rect, double angle, string text, int confidence)
        {
            _lastPlateTime = DateTime.Now;

            plate.Angle = angle;
            plate.Left = (int)rect.Left;
            plate.Top = (int)rect.Top;
            plate.Width = (int)rect.Width;
            plate.Height = (int)rect.Height;
            if (plate.Confidence > 0 && plate.Confidence < confidence && plate.Text != text)
            {
                ShowOsdMessage("Plate updated: " + text + " with " + confidence / 10 + "% confidence");
                plate.Confidence = confidence;
                plate.Text = text;
            }

            if (plate.Confidence == 0) plate.Confidence = confidence;

            SelectedPlate = plate;

            PlateLeft = rect.Left - 5;
            PlateTop = rect.Top - 5;
            PlateWidth = rect.Width + 10;
            PlateHeight = rect.Height + 10;
            PlateAngle = angle;

            ShowPlate = true;
        }
    }
}
