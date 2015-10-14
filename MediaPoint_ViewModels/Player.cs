using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using MediaPoint.Common.Helpers;
using MediaPoint.Common.MediaFoundation;
using MediaPoint.Common.Subtitles;
using MediaPoint.MVVM;
using System.Collections.ObjectModel;
using System.Windows.Input;
using MediaPoint.Subtitles;
using MediaPoint.Subtitles.Logic;
using MediaPoint.VM.Config;
using MediaPoint.VM.ViewInterfaces;
using MediaPoint.MVVM.Services;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Net;

namespace MediaPoint.VM
{
	public class Player : ViewModel
	{
		#region Members
		IDialogService _dlg;
		BackgroundWorker _bWork = new BackgroundWorker();
	    private volatile bool _subtitleIsDownloading;
		#endregion

		#region Ctor

		public Player(Main main)
		{
            OnlineSubtitleChoices = new ObservableCollection<SubtitleMatch>();
		    Main = main;
            MediaPosition = 0;
		    MediaDuration = 0;
			Status = "Stopped";
			SourceFileName = "<nothing loaded>";
			Volume = 1;
			Rate = 1;
			IsDeeperColor = true;
			SubtitleStreams = new ObservableCollection<SubtitleItem>();
			IsStopped = true;
			_dlg = ServiceLocator.GetService<IDialogService>();				
		}

		#endregion

		#region Properties

        public Main Main
        {
            get { return GetValue(() => Main); }
            set { SetValue(() => Main, value); }
        }

		public IPlayerView View
		{
			get { return GetValue<IPlayerView>(() => View); }
			set
			{
			    SetValue<IPlayerView>(() => View, value);
                if (View != null)
                {
                    ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
                }
			}
		}

        public string MediaInfo
        {
            get { return GetValue(() => MediaInfo); }
            set { SetValue(() => MediaInfo, value); }
        }

		public string Status
		{
			get { return GetValue<string>(() => Status); }
			set { SetValue<string>(() => Status, value); }
		}

		public bool HasVideo
		{
			get { return GetValue<bool>(() => HasVideo); }
			set
            {
                if (SetValue<bool>(() => HasVideo, value) && Source != null)
                {
                    if (value)
                    {
                        Main.ShowVisualizations = false;
                        Main.ShowEqualizer = false;
                    }
                    else
                    {
                        Main.ShowVisualizations = Source != null && true;
                        Main.ShowEqualizer = Source != null && true;
                    }
                }
            }
		}

		public string SourceFileName
		{
			get { return GetValue<string>(() => SourceFileName); }
			set {
                SetValue<string>(() => SourceFileName, value);
                SubtitleDefaultSearchText = value;
            }
		}

        public bool IsSubtitleSearchInProgress
        {
            get { return GetValue(() => IsSubtitleSearchInProgress); }
            set
            {
                SetValue(() => IsSubtitleSearchInProgress, value);
                if (HasVideo == false) return; // silent
                if (IsSubtitleSearchInProgress)
                {
                    Main.ShowOsdMessage("Subtitle search is in progress.");
                }
                else
                {
                    Main.ShowOsdMessage("Subtitle search finished.");
                }
            }
        }

        public string SubtitleDefaultSearchText
        {
            get { return GetValue(() => SubtitleDefaultSearchText); }
            set { SetValue(() => SubtitleDefaultSearchText, value); }
        }

		public Uri Source
		{
			get { return GetValue<Uri>(() => Source); }
			set { SetValue<Uri>(() => Source, value);
				if (value != null)
				{
                    Main.Playlist.SetPlaying(value);
                    if (value.IsFile)
                    {
                        SourceFileName = Path.GetFileNameWithoutExtension(value.LocalPath);
                        Main.ShowOsdMessage(string.Format("Opening '{0}'", Path.GetFileName(SourceFileName)));
                    }
                    else
                    {
                        SourceFileName = Uri.UnescapeDataString(value.AbsoluteUri);
                        Main.ShowOsdMessage(string.Format("Opening '{0}'", SourceFileName));
                    }
				}
				else
				{
                    Main.Playlist.SetPlaying(null);
					SourceFileName = "<nothing loaded>";
				}
			}
		}

		public bool IsPlaying
		{
			get { return !IsPaused && !IsStopped; }
		}

		public bool IsPaused
		{
			get { return GetValue<bool>(() => IsPaused); }
			set {
                if (SetValue<bool>(() => IsPaused, value))
                {
                    OnPropertyChanged(() => IsPlaying);
                    ServiceLocator.GetService<IMainView>().RefreshUIElements();
                }
			}
		}

		public bool IsDeeperColor
		{
			get { return GetValue<bool>(() => IsDeeperColor); }
			set { SetValue<bool>(() => IsDeeperColor, value); }
		}

        public long TrackbarMediaPosition
        {
            get { return GetValue(() => TrackbarMediaPosition); }
            set
            {
                SetValue(() => TrackbarMediaPosition, value);
            }
        }

        public bool IsTrackbarBeingMoved
        {
            get { return GetValue(() => IsTrackbarBeingMoved); }
            set
            {
                SetValue(() => IsTrackbarBeingMoved, value);
            }
        }

		public long MediaPosition
		{
			get { return GetValue(() => MediaPosition); }
			set
            {
                if (IsTrackbarBeingMoved)
                {
                    if (Math.Abs(MediaPosition - value) < MediaDuration / 1000)
                    {
                        return;
                    }
                }

                SetValue(() => MediaPosition, value);
                    
                UpdatePositionInUI(value);
                if (MediaPosition > MediaDuration && MediaDuration > 0 && IsPlaying)
                {
                    ForceStop();
                }
			}
		}

        private void UpdatePositionInUI(long value)
        {
            Position = TimeSpan.FromSeconds(value / 10000000).ToString();
            Remaining = TimeSpan.FromSeconds(Math.Abs(MediaDuration - value) / 10000000).ToString();
            ServiceLocator.GetService<IMainView>().Invoke((Action)(() =>
            {
                if (MediaDuration < value * 100)
                {
                    TaskbarManager.Instance.SetProgressValue(0, 100, ServiceLocator.GetService<IMainView>().GetWindow());
                }
                else
                {
                    TaskbarManager.Instance.SetProgressValue(value == 0 ? 0 : (int)(value * 100 / MediaDuration), 100, ServiceLocator.GetService<IMainView>().GetWindow());
                }
            }));
        }

	    public double Volume
		{
			get { return GetValue<double>(() => Volume); }
			set
			{
                if (SetValue(() => Volume, value))
                {
                    ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
                    Main.ShowOsdMessage(string.Format("Volume {0}%", (int)Math.Round(value * 100)));
                }
			}
		}

		public long MediaDuration
		{
			get { return GetValue(() => MediaDuration); }
			set
			{
				if (SetValue(() => MediaDuration, value))
				{
					Duration = TimeSpan.FromSeconds(MediaDuration/10000000).ToString();
				}
			}
		}

		public string Position
		{
			get { return GetValue(() => Position); }
			set { SetValue(() => Position, value); }
		}

        public string Remaining
        {
            get { return GetValue(() => Remaining); }
            set { SetValue(() => Remaining, value); }
        }

		public string Duration
		{
			get { return GetValue(() => Duration); }
			set { SetValue(() => Duration, value); }
		}

		public bool Loop
		{
			get { return GetValue(() => Loop); }
			set { SetValue(() => Loop, value); }
		}

		public double Rate
		{
			get { return GetValue(() => Rate); }
			set
            {
                if (SetValue(() => Rate, value))
                {
                    Main.ShowOsdMessage(string.Format("Playback rate changed to '{0}'", value));
                }
            }
		}

		public ObservableCollection<SubtitleItem> SubtitleStreams
		{
			get { return GetValue(() => SubtitleStreams); }
			set { SetValue(() => SubtitleStreams, value); }
		}

        public SubtitleMatch SelectedOnlineSubtitle
        {
            get { return GetValue(() => SelectedOnlineSubtitle); }
            set { SetValue(() => SelectedOnlineSubtitle, value); }
        }

        public ObservableCollection<SubtitleMatch> OnlineSubtitleChoices
        {
            get { return GetValue(() => OnlineSubtitleChoices); }
            set { SetValue(() => OnlineSubtitleChoices, value); }
        }

        public bool ShowOnlineSubtitles
        {
            get { return GetValue(() => ShowOnlineSubtitles); }
            set {
                SetValue(() => ShowOnlineSubtitles, value);

                if (value == true && OnlineSubtitleChoices.Count == 0 && Source != null && HasVideo)
                {
                    RefreshOnlineSubs();
                }
            }
        }

        public void ClearSelectedSubtitle() 
        {
            SetValue(() => SelectedSubtitle, null);
        }

		public SubtitleItem SelectedSubtitle
		{
			get { return GetValue(() => SelectedSubtitle); }
			set
            {
                if (value == null) return; // listboxes bound would clear this when clearing itemssource, so if need to clear in code use ClearSelectedSubtitle()

                if (value.Type == SubtitleItem.SubtitleType.None)
                {
                    ClearSelectedSubtitle();
                    Main.ShowOsdMessage("Subtitle unset");
                    return;
                }

                SetValue(() => SelectedSubtitle, value);
                Main.ShowOsdMessage(string.Format("Setting subtitle '{0}'", value.DisplayName));
            }
		}

        public SubtitleItem DownloadedSubtitle
        {
            get { return GetValue(() => DownloadedSubtitle); }
            set { SetValue(() => DownloadedSubtitle, value); }
        }

        public ObservableCollection<string> EmbeddedSubtitleStreams
        {
            get { return GetValue(() => EmbeddedSubtitleStreams); }
            set
            {
                if (SetValue(() => EmbeddedSubtitleStreams, value) && value != null)
                {
                    ReinsertEmbeddedSubtitlesIntoSubtitleStreams(value);
                }
            }
        }

        private void ReinsertEmbeddedSubtitlesIntoSubtitleStreams(ObservableCollection<string> value)
        {
            for (int i = SubtitleStreams.Count - 1; i >= 0; i--)
            {
                if (SubtitleStreams[i].Type == SubtitleItem.SubtitleType.Embedded)
                {
                    SubtitleStreams.RemoveAt(i);
                }
            }
            for (int i = 0; i < value.Count; i++)
            {
                if (value[i].ToLowerInvariant() == "s: no subtitles") continue;
                string name = "Embedded: " + value[i].Substring(2);
                SubtitleStreams.Insert(i + 1, new SubtitleItem(SubtitleItem.SubtitleType.Embedded, SubtitleItem.SubtitleSubType.None, value[i], name));
            }
        }

		public ObservableCollection<string> AudioStreams
		{
			get { return GetValue(() => AudioStreams); }
			set { SetValue(() => AudioStreams, value); }
		}

		public ObservableCollection<string> VideoStreams
		{
			get { return GetValue(() => VideoStreams); }
			set { SetValue(() => VideoStreams, value); }
		}

		public bool IsStopped
		{
			get { return GetValue<bool>(() => IsStopped); }
			set { SetValue<bool>(() => IsStopped, value);
				OnPropertyChanged(() => IsPlaying);
                if (value)
                {
                    IMDb = null;
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress, ServiceLocator.GetService<IMainView>().GetWindow());
                }
			}
		}

        public IMDb IMDb
        {
            get { return GetValue(() => IMDb); }
            set
            {
                if (SetValue(() => IMDb, value))
                {
                    if (value == null)
                        ShowIMdb = false;
                    else
                    {
                        Main.ShowOsdMessage(string.Format("Movie matched to '{0} ({1})'", value.Title, value.Year));
                        ShowIMdb = true;
                        //Observable.Return(1).Delay(TimeSpan.FromSeconds(10)).Subscribe(i => { if (!_invalidateImdbHiding) ShowIMdb = false; });
                    }
                }
            }
        }

        public bool ShowIMdb
        {
            get { return GetValue(() => ShowIMdb); }
            set { SetValue(() => ShowIMdb, value); }
        }

		#endregion

		#region Commands

        public ICommand ShowImdbCommand
		{
			get
			{
				return new Command(o =>
				{

					if (ShowIMdb)
					{
					    SetValue(() => ShowIMdb, false);
					}
					else
					{
                        SetValue(() => ShowIMdb, true);
					}
				}, can =>
				{
					return true;
				});
			}
		}

        public ICommand LoadSelectedOnlineSubtitleCommand
        {
            get
            {
                return new Command(o =>
                {
                    LoadSelectedOnlineSubtitle();
                }, can =>
                {
                    return SelectedOnlineSubtitle != null;
                });
            }
        }

        public ICommand ShowOnlineSubtitlesCommand
        {
            get
            {
                return new Command(o =>
                {
                    if (!string.IsNullOrEmpty(o as string) && (string)o == "hide")
                        ShowOnlineSubtitles = false;
                    else
                        ShowOnlineSubtitles = !ShowOnlineSubtitles;
                }, can =>
                {
                    return true;
                });
            }
        }

        public ICommand NeedSubtitlesCommand
        {
            get
            {
                return new Command(o =>
                {
#if !ALPR
                    if (HasVideo == false) return;

                    if (o is string && (string)o == "online")
                    {
                        // this means we clicked on the show online subtitles button so lets refresh the results
                        RefreshOnlineSubs();
                    }
                    else
                    {
                        // this happens when the player started without subtitles
                        SetBestSubtitles();
                    }
#endif
                }, can =>
                {
                    return Source != null;
                });
            }
        }

		public ICommand StopCommand
		{
			get
			{
				return new Command(o =>
				{
					Stop();
				}, can =>
				{
					return Source != null;
				});
			}
		}

        public ICommand PlayCommand
		{
			get
			{
				return new Command(o =>
				{
					Play();
				}, can =>
				{
					return true;
				});
			}
		}

        public ICommand DecreaseRateCommand
        {
            get
            {
                return new Command(o =>
                {
                    Rate = Rate / 2;
                    if (((int)(Rate * 100)) / 100 == 1 && Rate != 1)
                    {
                        Rate = 1;
                    }
                }, can =>
                {
                    return IsPlaying && Rate > (1.0/8);
                });
            }
        }

		public ICommand IncreaseRateCommand
		{
			get
			{
				return new Command(o =>
				{
					Rate = Rate * 2;
                    if (((int)(Rate * 100)) / 100 == 1 && Rate != 1)
                    {
                        Rate = 1;
                    }
				}, can =>
				{
                    return IsPlaying && Rate < 8;
				});
			}
		}

		public ICommand OpenCommand
		{
			get
			{
				return new Command(o =>
				{
					Uri uri = null;

                    if (o is Uri)
                    {
                        uri = o as Uri;
                    }
                    else if ((string)o == "true" && Source != null)
                    {
                        uri = new Uri((string)Source.LocalPath);
                    }
                    else if ((string)o == null || (string)o == "true")
                    {
                        uri = _dlg.ShowOpenUriDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), SupportedFiles.OpenFileDialogFilter);
                        if (uri == null) return;
                    }
                    else
                    {
                        uri = new Uri((string)o);
                    }

                    ForceStop();
					Open(uri);

				}, can =>
				{
					return true;
				});
			}
		}	

		#endregion

		#region Methods

        public void ForceStop()
        {
            ClearSelectedSubtitle();
            IsPaused = true;
            MediaPosition = 0;
            IsPaused = false;
            IsStopped = true;
            Source = null;
            if (View != null) View.ExecuteCommand(PlayerCommand.Stop);
            Main.ShowOsdMessage("Stopped");
            HasVideo = false;
        }

		public void Stop()
		{
            ClearSelectedSubtitle();
			if (View != null) View.ExecuteCommand(PlayerCommand.Stop, null);
			MediaPosition = 0;
			IsPaused = true;
			IsStopped = true;
			Status = "Stopped";
            ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
            Main.ShowOsdMessage("Stopped");
            Source = null;
            HasVideo = false;
		}

        public void LoadMediaInfo()
        {
            var src = Source;
            if (src == null || src.IsFile == false) return;

            BackgroundWorker b = new BackgroundWorker();

            b.DoWork += (sender, args) =>
            {
                var info = new MediaInfo();
                info.Open(src.LocalPath);
                info.Option("Complete", "0");
                args.Result = info.Inform();
                info.Close();
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                MediaInfo = args.Result as string;
            };
            b.RunWorkerAsync();
        }

		public void Play(bool force = false)
		{
            if (Source == null)
            {
                OpenCommand.Execute(null);
                return;
            }

            var track = Main.Playlist.TrackForUri(Source);
            Main.Playlist.CurrentTrack = track;

			if (!IsPaused && !force)
			{
				Pause();
			}
			else
			{
				Rate = 1;
				View.ExecuteCommand(PlayerCommand.Play);
				IsPaused = false;
				IsStopped = false;
				Status = "Playing";
                Main.ShowOsdMessage("Playing");
                
                ServiceLocator.GetService<IMainView>().DelayedInvoke(() => {
                    //Main.Equalizer.Update();
                }, 2000);
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, ServiceLocator.GetService<IMainView>().GetWindow());
			}

            ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
		}

        public void OnMediaEnded()
        {
            ForceStop();

            var next = Main.Playlist.GetNextTrack();

            if (next != null)
            {
                OpenCommand.Execute(next);
            }

            Main.ShowOsdMessage("Media ended");
        }

		public void Pause()
		{
            Main.ShowOsdMessage("Paused");
			View.ExecuteCommand(PlayerCommand.Pause, null);
			IsPaused = true;
			Status = "Paused";
            ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
            Main.ShowOsdMessage("Paused");
		}

        public void LoadSelectedOnlineSubtitle()
        {
            ShowOnlineSubtitles = false;
            BackgroundWorker b = new BackgroundWorker();
            b.DoWork += (sender, args) =>
            {
                DateTime start = DateTime.Now;
                Monitor.Enter(_subtitleSearchLocker);

                try
                {
                    args.Result = SubtitleUtil.DownloadSubtitle(SelectedOnlineSubtitle, Source.LocalPath);
                }
                catch (WebException)
                {
                    Main.ShowOsdMessage("Failed to download subtitle: Internet connection unavailable");
                }
                catch (Exception)
                {
                    Main.ShowOsdMessage("Failed to download subtitle from '" + SelectedOnlineSubtitle.Service + "'");
                }
                finally
                {
                    Monitor.Exit(_subtitleSearchLocker);
                    if (DateTime.Now - start > TimeSpan.FromSeconds(15))
                    {
                        args.Cancel = true;
                    }
                }
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                if (!args.Cancelled && args.Error == null && args.Result is string && Source != null)
                {
                    FillSubs(Source);
                    string resultSub = (string)args.Result;
                    if (SubtitleStreams.Any(s => s.Path.ToLowerInvariant() == resultSub.ToLowerInvariant()) == false)
                    {
                        SubtitleStreams.Add(new SubtitleItem(SubtitleItem.SubtitleType.File, SubtitleItem.SubtitleSubType.Srt, resultSub, "Subtitle"));
                    }
                    var loadSub = (DownloadedSubtitle = SubtitleStreams.FirstOrDefault(s => s.Path.ToLowerInvariant() == (resultSub).ToLowerInvariant()));
                    ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = loadSub; }, 200);
                }
            };
            b.RunWorkerAsync();
        }

		public bool Open(Uri uri = null)
		{
			if (uri == null)
				return false;

#if ALPR
            Main.Plates.Clear();
#endif

            Main.Playlist.AddTrackIfNotExisting(uri);

            if (Main.Playlist.Tracks.Count > 1)
            {
                Main.ShowPlaylist = true;
            }

            ClearSelectedSubtitle();
            MediaPosition = 0;
			IsStopped = true;
			IsPaused = true;
			IsStopped = false;		
			Source = null;
			SubtitleItem sub = null;

#if !ALPR
            if (uri.IsFile)
            {
                if (null == (sub = FillSubs(uri)) && Main.AutoLoadSubtitles)
                {
                    // no subs found
                    DownloadSubtitleForUriAndQueryIMDB(uri);
                }
                else
                {
                    // we have subs
                    QueryIMDBForUri(uri);
                }
            }
#endif
            Main.ShowVisualizations = false;
            Main.ShowEqualizer = false;
            HasVideo = true;

            Source = uri;
            LoadMediaInfo();
            IsPaused = false;
            IsStopped = false;
            Play(true);

            if (sub != null)
            {
                // queue subtitle set
                // in case we didnt find any the command NeedSubtitlesCommand will be fired by the player when he opens the video
#if !ALPR
                ServiceLocator.GetService<IMainView>().DelayedInvoke(() => SelectedSubtitle = sub, 200);
#endif
            }

            if (Source != null)
            {
                if (Source.IsFile)
                {
                    var sourceFileName = Path.GetFileNameWithoutExtension(Source.LocalPath);
                    Main.ShowOsdMessage(string.Format("Opening '{0}'", Path.GetFileName(sourceFileName)));
                }
                else
                {
                    var sourceFileName = Uri.UnescapeDataString(Source.AbsoluteUri);
                    Main.ShowOsdMessage(string.Format("Opening '{0}'", sourceFileName));
                }
            }
            else
            {
                Main.ShowOsdMessage("Nothing loaded");
            }

			return true;
		}

        private void QueryIMDBForUri(Uri uri)
        {
            BackgroundWorker b = new BackgroundWorker();

            b.DoWork += (sender, args) =>
            {
                DateTime start = DateTime.Now;
                string strTitle, strYear, strTitleAndYear; int season, episode;
                SubtitleDownloader.Core.Subtitle bestGuessSubtitle = null;
                try
                {
                    args.Result = SubtitleUtil.GetIMDbFromFilename(uri.LocalPath, Path.GetDirectoryName(uri.LocalPath), out strTitle, out strTitleAndYear, out strYear, out season, out episode, MessageCallback, out bestGuessSubtitle);
                }
                catch
                {
                    args.Cancel = true;
                }
                if (DateTime.Now - start > TimeSpan.FromSeconds(15))
                {
                    args.Cancel = true;
                }
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Cancelled || args.Error != null)
                {
                    IMDb = null;
                    return;
                }
                object r = null;
                try
                {
                    r = args.Result;
                }
                catch { }
                if (r != null)
                {
                    IMDb = (IMDb)r;
                }
                else
                {
                    IMDb = null;
                }
            };
            b.RunWorkerAsync();
        }

        void MessageCallback(string message)
        {
            Main.ShowOsdMessage(message);
        }

        private object _subtitleSearchLocker = new object();

        private void DownloadSubtitleForUriAndQueryIMDB(Uri uri)
        {           
            // try online
            BackgroundWorker b = new BackgroundWorker();
            _subtitleIsDownloading = true;
            IsSubtitleSearchInProgress = true;
            b.DoWork += (sender, args) =>
            {
                DateTime start = DateTime.Now;
                
                Monitor.Enter(_subtitleSearchLocker);

                IMDb imdb;
                List<SubtitleMatch> otherChoices;
                try
                {
                    string s = SubtitleUtil.DownloadSubtitle(uri.LocalPath, Main.SubtitleLanguages.Select(l => l.Id).ToArray(),
                                                                Main.SubtitleServices.Select(l => l.Id).ToArray(), out imdb, out otherChoices, MessageCallback);
                    if (!string.IsNullOrEmpty(s) && imdb.status)
                    {
                        args.Result = new object[] { s, imdb, otherChoices };
                    }
                    else
                    {
                        args.Result = imdb;
                    }
                }
                catch (WebException)
                {
                    Main.ShowOsdMessage("Internet connection unavailable.");
                    args.Result = null;
                }
                finally
                {
                    Monitor.Exit(_subtitleSearchLocker);
                    if (DateTime.Now - start > TimeSpan.FromSeconds(15))
                    {
                        args.Cancel = true;
                    }
                }
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                try
                {
                    if (args.Cancelled || args.Error != null || HasVideo == false) return;
                    var result = (object)null;
                    try
                    {
                        result = args.Result;
                    }
                    catch { /* bad request etc */ }

                    if (result != null)
                    {
                        if (result is IMDb)
                        {
                            // we got no subtitle but have IMDb
                            IMDb = result as IMDb;
                        }
                        else
                        {
                            // both subtitle and IMDB
                            string resultSub = (string)((object[])result)[0];
                            IMDb imdb = (IMDb)((object[])result)[1];
                            var param2 = ((object[])result)[2];
                            SetupDownloadedSubtitleAndIMDbInfo(uri, resultSub, imdb, param2);
                        }
                    }
                    else
                    {
                        // we got nothing
                        IMDb = null;
                    }
                }
                finally
                {
                    IsSubtitleSearchInProgress = false;
                    _subtitleIsDownloading = false;
                }
            };
            b.RunWorkerAsync();
        }

        private void SetupDownloadedSubtitleAndIMDbInfo(Uri uri, string resultSub, IMDb imdb, object param2)
        {
            OnlineSubtitleChoices.Clear();
            if (HasVideo == false) return;
            IMDb = imdb;
            
            if (param2 is List<SubtitleMatch>)
            {
                foreach (var st in (List<SubtitleMatch>)param2)
                {
                    OnlineSubtitleChoices.Add(st);
                }
                Main.ShowOsdMessage(string.Format("{0} subtitles found.", OnlineSubtitleChoices.Count));
            }
            FillSubs(uri);

            if (SubtitleStreams.Any(s => s.Path.ToLowerInvariant() == resultSub.ToLowerInvariant()) == false)
            {
                SubtitleStreams.Add(new SubtitleItem(SubtitleItem.SubtitleType.File, SubtitleItem.SubtitleSubType.Srt, resultSub, "Subtitle"));
            }

            var loadSub = (DownloadedSubtitle = SubtitleStreams.FirstOrDefault(s => s.Path.ToLowerInvariant() == resultSub.ToLowerInvariant()));
            ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = loadSub; }, 200);
        }

        private void RefreshOnlineSubs()
        {

            IsSubtitleSearchInProgress = true;
            BackgroundWorker b = new BackgroundWorker();
            b.DoWork += (sender, args) =>
            {
                DateTime start = DateTime.Now;
                
                IMDb imdb;
                List<SubtitleMatch> otherChoices;
                try
                {
                    SubtitleUtil.FindSubtitleForFilename(SubtitleDefaultSearchText, SubtitleDefaultSearchText, Main.SubtitleLanguages.Select(l => l.Id).ToArray(), Main.SubtitleServices.Select(l => l.Id).ToArray(), out imdb, out otherChoices, MessageCallback, true, false);
                    args.Result = otherChoices;
                }
                catch (WebException)
                {
                    Main.ShowOsdMessage("Internet connection unavailable.");
                    args.Result = null;
                }
                finally
                {
                    if (DateTime.Now - start > TimeSpan.FromSeconds(15))
                    {
                        args.Cancel = true;
                    }
                }
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                try
                {
                    if (args.Cancelled || args.Error != null) return;

                    SetupDownloadedSubtitleAndIMDbInfo(Source, SelectedSubtitle != null ? SelectedSubtitle.Path : "", IMDb, args.Result);
                }
                catch (Exception)
                {
                    Main.ShowOsdMessage("Querying subtitle providers failed. Please try again.");
                }
                finally
                {
                    IsSubtitleSearchInProgress = false;
                }
            };
            b.RunWorkerAsync();
        }

        private void SetBestSubtitles()
        {
            if (!Monitor.TryEnter(_subtitleSearchLocker, 0))
            {
                // no point in doing this if we are already downloading subtitles, which will do the same thing effectively
                return;
            }

            SubtitleItem bestSubMatch = null;

            try
            {

                if (!_subtitleIsDownloading && Source != null &&
                    SubtitleStreams.Any(s => s.Type == SubtitleItem.SubtitleType.File && File.Exists(s.Path)))
                {
                    // if nothing is downloading and we have file subs in the folder
                    string lcode = Main.SubtitleLanguages.Count > 0 ? Main.SubtitleLanguages[0].Id : "";

                    for (int i = 0; i < Main.SubtitleLanguages.Count; i++)
                    {
                        // TODO this should care also for EMBEDDED subs
                        bestSubMatch = SubtitleStreams.Where(s => s.Type == SubtitleItem.SubtitleType.File && File.Exists(s.Path)).OrderBy(f => Path.GetFileNameWithoutExtension(f.Path).ToLowerInvariant().StartsWith(Path.GetFileNameWithoutExtension(Source.LocalPath.ToLowerInvariant())) && (lcode == "" || Path.GetFileNameWithoutExtension(f.Path).EndsWith(lcode)) ? 0 : 1).FirstOrDefault();
                        if (bestSubMatch != null) break;
                    }

                    ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = bestSubMatch; }, 200);
                    return;
                }
            }
            finally
            {
                Monitor.Exit(_subtitleSearchLocker);
            }
            if (bestSubMatch == null) DownloadSubtitleForUriAndQueryIMDB(Source);
        }

		private SubtitleItem FillSubs(Uri video)
		{
			List<SubtitleItem> subs = new List<SubtitleItem>();
			SubtitleStreams.Clear();
		    var scMgr = new Subtitles.Subtitles();

            SubtitleStreams.Add(new SubtitleItem(SubtitleItem.SubtitleType.None, SubtitleItem.SubtitleSubType.None, "", "<No Subtitles>"));
			// embedded
            //long wouldLikeToLoadEmbedded = -1;
            //bool loadedEmbeddedSub = (video.LocalPath.ToLowerInvariant().EndsWith("mkv") ||
            //                          video.LocalPath.ToLowerInvariant().EndsWith("mp4")) &&
            //                         (wouldLikeToLoadEmbedded = scMgr.ListEmbeddedSubtitles(video.LocalPath, out subs)) >= 0;


            //foreach (var embeddedSubtitleStream in subs)
            //{
            //    SubtitleStreams.Add(embeddedSubtitleStream);
            //}

            //subs.Clear();

            // TODO would like to load embedded
            if (EmbeddedSubtitleStreams != null && EmbeddedSubtitleStreams.Count > 0) ReinsertEmbeddedSubtitlesIntoSubtitleStreams(EmbeddedSubtitleStreams);

			// files
            string wouldLikeToLoadFile = scMgr.LoadSubtitles(video.LocalPath, out subs, null);

			foreach (var fileSub in subs)
			{
				SubtitleStreams.Add(fileSub);
			}

            if (!string.IsNullOrEmpty(wouldLikeToLoadFile))
			{
				return SubtitleStreams.First(f => f.Path == wouldLikeToLoadFile);
			}
            
            //if (wouldLikeToLoadEmbedded >= 0 && loadedEmbeddedSub)
            //{
            //    return SubtitleStreams.First(e => e.Path == wouldLikeToLoadEmbedded.ToString());
            //}

			return null;
		}

		#endregion


        public void FrameStep()
        {
            Pause();
            MediaPosition+=416666;
            Play(true);
            ServiceLocator.GetService<IMainView>().DelayedInvoke((Action)(() => { Pause(); Main.ShowOsdMessage("Framesteping at " + Position); }), 40);

        }
    }
}
