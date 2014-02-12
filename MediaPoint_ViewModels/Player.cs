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
	    private bool _invalidateImdbHiding;
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
			SourceFileName = "<no file loaded>";
			WorkQueue = new Queue<Action>();
			Volume = 0.7;
			Rate = 1;
			IsDeeperColor = true;
			SubtitleStreams = new ObservableCollection<SubtitleItem>();
			_bWork.DoWork += (o, e) =>
			{
                var mw = ServiceLocator.GetService<IMainView>();	
				while (!e.Cancel) {
                    Thread.Sleep(500);
                    if (WorkQueue.Count > 0 && View != null)
					{
                        if (mw == null) mw = ServiceLocator.GetService<IMainView>();
						try
						{
							mw.Invoke(() => WorkQueue.Dequeue());
						}
						catch (Exception ex)
						{
							Debug.WriteLine("WorkQueue exception: " + ex.Message + " " + ex.StackTrace);
						}
					}
				}
			};
			_bWork.RunWorkerAsync();
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
			set { SetValue<bool>(() => HasVideo, value); }
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
            set { SetValue(() => IsSubtitleSearchInProgress, value); }
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
					SourceFileName = Path.GetFileNameWithoutExtension(value.LocalPath);
				}
				else
				{
					SourceFileName = "<no file loaded>";
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
			set { SetValue<bool>(() => IsPaused, value);
				OnPropertyChanged(() => IsPlaying);
			}
		}

		public bool IsDeeperColor
		{
			get { return GetValue<bool>(() => IsDeeperColor); }
			set { SetValue<bool>(() => IsDeeperColor, value); }
		}

		public long MediaPosition
		{
			get { return GetValue<long>(() => MediaPosition); }
			set {
				if (SetValue<long>(() => MediaPosition, value))
				{
					Position = TimeSpan.FromSeconds(MediaPosition / 10000000).ToString();
				}
				if (MediaPosition > MediaDuration && MediaDuration > 0 && IsPlaying)
				{
                    ForceStop();
				}
                TaskbarManager.Instance.SetProgressValue(MediaDuration == 0 ? 0 : (int)(MediaPosition*100/MediaDuration), 100, ServiceLocator.GetService<IMainView>().GetWindow());
			}
		}

	    public double Volume
		{
			get { return GetValue<double>(() => Volume); }
			set
			{
			    SetValue<double>(() => Volume, value);
                ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
			}
		}

		public long MediaDuration
		{
			get { return GetValue<long>(() => MediaDuration); }
			set
			{
				if (SetValue<long>(() => MediaDuration, value))
				{
					Duration = TimeSpan.FromSeconds(MediaDuration/10000000).ToString();
				}
			}
		}

		public string Position
		{
			get { return GetValue<string>(() => Position); }
			set { SetValue<string>(() => Position, value); }
		}

		public string Duration
		{
			get { return GetValue<string>(() => Duration); }
			set { SetValue<string>(() => Duration, value); }
		}

		public bool Loop
		{
			get { return GetValue<bool>(() => Loop); }
			set { SetValue<bool>(() => Loop, value); }
		}

		public double Rate
		{
			get { return GetValue<double>(() => Rate); }
			set { SetValue<double>(() => Rate, value); }
		}

		public ObservableCollection<SubtitleItem> SubtitleStreams
		{
			get { return GetValue<ObservableCollection<SubtitleItem>>(() => SubtitleStreams); }
			set { SetValue<ObservableCollection<SubtitleItem>>(() => SubtitleStreams, value); }
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

                if (value == true && OnlineSubtitleChoices.Count == 0 && Source != null)
                {
                    RefreshOnlineSubs();
                }
            }
        }

        public void ClearSelectedSubtitle() { SetValue(() => SelectedSubtitle, null); }
		public SubtitleItem SelectedSubtitle
		{
			get { return GetValue(() => SelectedSubtitle); }
			set
            {
                if (value == null) return; // listboxes bound would clear this when clearing itemssource, so if need to clear in code use ClearSelectedSubtitle()

                SetValue(() => SelectedSubtitle, value); 
            }
		}

        public SubtitleItem DownloadedSubtitle
        {
            get { return GetValue(() => DownloadedSubtitle); }
            set { SetValue(() => DownloadedSubtitle, value); }
        }

		public ObservableCollection<string> AudioStreams
		{
			get { return GetValue(() => AudioStreams); }
			set { SetValue(() => AudioStreams, value); }
		}

        public string ErrorMessage
        {
            get { return GetValue(() => ErrorMessage); }
            set { SetValue(() => ErrorMessage, value); }
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

    	public Queue<Action> WorkQueue
		{
			get { return GetValue(() => WorkQueue); }
			set { SetValue(() => WorkQueue, value); }
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
                        ShowIMdb = true;
                        Observable.Return(1).Delay(TimeSpan.FromSeconds(10)).Subscribe(i =>
                                                                                           {
                                                                                               if (!_invalidateImdbHiding) ShowIMdb = false;
                                                                                           });
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
                    _invalidateImdbHiding = true;

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
                    if (o is string && (string)o == "online")
                    {
                        RefreshOnlineSubs();
                    }
                    else
                    {
                        SetBestSubtitles();
                    }
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
					return true;
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

        public ICommand PreviousCommand
        {
            get
            {
                return new Command(o =>
                {
                    //todo
                }, can =>
                {
                    return true;
                });
            }
        }

        public ICommand NextCommand
        {
            get
            {
                return new Command(o =>
                {
                    //todo
                }, can =>
                {
                    return true;
                });
            }
        }

		public ICommand OpenCommand
		{
			get
			{
				return new Command(o =>
				{
					Uri uri;

                    if ((string)o == "true" && Source != null)
                        o = Source.LocalPath;

                    if ((string)o == null || (string)o == "true")
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
					//Play(true);

				}, can =>
				{
					return true;
				});
			}
		}	

		#endregion

		#region Methods

        private void ForceStop()
        {
            ClearSelectedSubtitle();
            IsPaused = true;
            MediaPosition = 0;
            IsPaused = false;
            IsStopped = true;
            Source = null;
            View.ExecuteCommand(PlayerCommand.Stop);
        }

		public void Stop()
		{
            ClearSelectedSubtitle();
			View.ExecuteCommand(PlayerCommand.Stop, null);
			MediaPosition = 0;
			IsPaused = true;
			IsStopped = true;
			Status = "Stopped";
            ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
		}

        public void LoadMediaInfo()
        {
            var src = Source;
            if (src == null) return;

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
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal, ServiceLocator.GetService<IMainView>().GetWindow());
			}

            ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
		}


		public void Pause()
		{
			View.ExecuteCommand(PlayerCommand.Pause, null);
			IsPaused = true;
			Status = "Paused";
            ServiceLocator.GetService<IMainView>().UpdateTaskbarButtons();
		}

		public void Enqueue(Action action)
		{
			WorkQueue.Enqueue(action);
		}

        public void LoadSelectedOnlineSubtitle()
        {
            ShowOnlineSubtitles = false;
            BackgroundWorker b = new BackgroundWorker();
            b.DoWork += (sender, args) =>
            {
                try
                {
                    args.Result = SubtitleUtil.DownloadSubtitle(SelectedOnlineSubtitle, Source.LocalPath);
                }
                catch (WebException)
                {
                    ErrorMessage = "Nemaš net";
                }
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Result is string && Source != null) {
                    FillSubs(Source);
                    var loadSub = (DownloadedSubtitle = SubtitleStreams.FirstOrDefault(s => s.Path.ToLowerInvariant() == ((string)args.Result).ToLowerInvariant()));
                    ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = loadSub; }, 200);
                }
            };
            b.RunWorkerAsync();
        }

		public bool Open(Uri uri = null)
		{
		    _invalidateImdbHiding = false;
			if (uri == null)
				return false;

            ClearSelectedSubtitle();
            MediaPosition = 0;
			IsStopped = true;
			IsPaused = true;
			IsStopped = false;		
			Source = null;
			SubtitleItem sub;
		    
            if (null == (sub = FillSubs(uri)) && Main.AutoLoadSubtitles)
            {
                // try online
                BackgroundWorker b = new BackgroundWorker();
                _subtitleIsDownloading = true;
                b.DoWork += (sender, args) =>
                                {
                                    IsSubtitleSearchInProgress = true;
                                    IMDb imdb;
                                    List<SubtitleMatch> otherChoices;
                                    try
                                    {                                    
                                        string s = SubtitleUtil.DownloadSubtitle(uri.LocalPath, Main.SubtitleLanguages.Select(l => l.Id).ToArray(),
                                                                                 Main.SubtitleServices.Select(l => l.Id).ToArray(), out imdb, out otherChoices);
                                        if (!string.IsNullOrEmpty(s) && imdb.status)
                                        {
                                            args.Result = new object[] {s, imdb, otherChoices};
                                        }
                                        else
                                        {
                                            args.Result = imdb;
                                        }
                                    }
                                    catch (WebException)
                                    {
                                        ErrorMessage = "Nemaš net";
                                        args.Result = null;
                                    }
                                };
                b.RunWorkerCompleted += (sender, args) =>
                                            {
                                                try
                                                {
                                                    if (args.Result != null)
                                                    {
                                                        if (args.Result is IMDb)
                                                        {
                                                            IMDb = args.Result as IMDb;
                                                        }
                                                        else
                                                        {
                                                            string resultSub = (string) ((object[]) args.Result)[0];
                                                            IMDb imdb = (IMDb) ((object[]) args.Result)[1];
                                                            IMDb = imdb;
                                                            OnlineSubtitleChoices.Clear();
                                                            if (((object[]) args.Result)[2] is List<SubtitleMatch>)
                                                                foreach (var st in (List<SubtitleMatch>) ((object[]) args.Result)[2])
                                                                {
                                                                    OnlineSubtitleChoices.Add(st);
                                                                }
                                                            FillSubs(uri);
                                                            var loadSub = (DownloadedSubtitle = SubtitleStreams.FirstOrDefault( s => s.Path.ToLowerInvariant() == resultSub.ToLowerInvariant()));
                                                            ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = loadSub; }, 200);
                                                        }
                                                    }
                                                    else
                                                    {
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
            else
            {
                BackgroundWorker b = new BackgroundWorker();

                b.DoWork += (sender, args) =>
                {
                    string strTitle, strYear, strTitleAndYear;
                    args.Result = SubtitleUtil.GetIMDbFromFilename(uri.LocalPath, out strTitle, out strTitleAndYear, out strYear);
                };
                b.RunWorkerCompleted += (sender, args) =>
                {
                    if (args.Result != null)
                    {
                        IMDb = (IMDb)args.Result;
                    }
                    else
                    {
                        IMDb = null;
                    }
                };
                b.RunWorkerAsync();
            }

            Source = uri;
            LoadMediaInfo();
            IsPaused = false;
            IsStopped = false;
            Play(true);

            if (sub != null)
            {
                ServiceLocator.GetService<IMainView>().DelayedInvoke(() => SelectedSubtitle = sub, 200);
            }

			return true;
		}

        private void RefreshOnlineSubs()
        {
            BackgroundWorker b = new BackgroundWorker();
            b.DoWork += (sender, args) =>
            {
                IsSubtitleSearchInProgress = true;
                IMDb imdb;
                List<SubtitleMatch> otherChoices;
                try
                {
                    SubtitleUtil.FindSubtitleForFilename(SubtitleDefaultSearchText, Main.SubtitleLanguages.Select(l => l.Id).ToArray(), Main.SubtitleServices.Select(l => l.Id).ToArray(), out imdb, out otherChoices, true, false);
                    args.Result = otherChoices;
                }
                catch (WebException)
                {
                    ErrorMessage = "Nemas net";
                    args.Result = null;
                }
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                try
                {
                    OnlineSubtitleChoices.Clear();
                    if (args.Result is List<SubtitleMatch>)
                        foreach (var st in (List<SubtitleMatch>)args.Result)
                        {
                            OnlineSubtitleChoices.Add(st);
                        }
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
            return;
            if (!_subtitleIsDownloading &&
                SubtitleStreams.Any(s => s.Type == SubtitleItem.SubtitleType.File && File.Exists(s.Path)))
            {
                string lcode = Main.SubtitleLanguages.Count > 0 ? Main.SubtitleLanguages[0].Id : "";
                
                var sorted =
                    SubtitleStreams.Where(s => s.Type == SubtitleItem.SubtitleType.File && File.Exists(s.Path)).OrderBy(
                        f => lcode == "" || Path.GetFileNameWithoutExtension(f.Path).EndsWith(lcode) ? 0 : 1).ToArray();

                var ss = sorted.First();

                // no need to background work
                ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = ss; }, 200);
                return;
            }

            BackgroundWorker b = new BackgroundWorker();
            b.DoWork += (sender, args) =>
            {
                while (_subtitleIsDownloading)
                {
                    Thread.Sleep(100);
                }

                var sub = DownloadedSubtitle;

                if (sub == null || !File.Exists(sub.Path))
                {
                    _subtitleIsDownloading = true;
                    try
                    {
                        IMDb imdb;
                        List<SubtitleMatch> others;
                        string s = SubtitleUtil.DownloadSubtitle(Source.LocalPath,
                                                                 Main.SubtitleLanguages.Select(l => l.Id).ToArray(),
                                                                 Main.SubtitleServices.Select(l => l.Id).ToArray(),
                                                                 out imdb, out others);

                        if (!string.IsNullOrEmpty(s) && imdb.status)
                        {
                            args.Result = new object[] {s, others};
                        }
                        else
                        {
                            args.Result = null;
                        }
                    }
                    finally
                    {
                        _subtitleIsDownloading = false;
                    }
                }
                else
                {
                    args.Result = new object[] { sub.Path, null };
                }
                
            };
            b.RunWorkerCompleted += (sender, args) =>
            {
                if (Source != null)
                {
                    var others = args.Result != null ? ((object[])args.Result)[1] as List<SubtitleMatch> : null;
                    string fName = args.Result != null ? ((object[])args.Result)[0] as string : null;

                    FillSubs(Source);
                    
                    OnlineSubtitleChoices.Clear();
                    if (others != null)
                        foreach (var st in others)
                        {
                            OnlineSubtitleChoices.Add(st);
                        }

                    if (fName != null) DownloadedSubtitle = new SubtitleItem(SubtitleItem.SubtitleType.File, SubtitleItem.SubtitleSubType.Srt, fName, fName);
                    var sub = (DownloadedSubtitle != null && File.Exists(DownloadedSubtitle.Path) ? DownloadedSubtitle : null) ?? FillSubs(Source);
                    ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = sub; }, 200);
                }
                else
                {
                    ServiceLocator.GetService<IMainView>().DelayedInvoke(() => { SelectedSubtitle = null; }, 200);
                }
            };
            b.RunWorkerAsync();            
        }

		private SubtitleItem FillSubs(Uri video)
		{
			List<SubtitleItem> subs = new List<SubtitleItem>();
			SubtitleStreams.Clear();
		    var scMgr = new Subtitles.Subtitles();

			// embedded
			long wouldLikeToLoadEmbedded = -1;
			bool loadedEmbeddedSub = (video.LocalPath.ToLowerInvariant().EndsWith("mkv") ||
									  video.LocalPath.ToLowerInvariant().EndsWith("mp4")) &&
                                     (wouldLikeToLoadEmbedded = scMgr.ListEmbeddedSubtitles(video.LocalPath, out subs)) >= 0;


			foreach (var embeddedSubtitleStream in subs)
			{
			    SubtitleStreams.Add(embeddedSubtitleStream);
			}

			subs.Clear();

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
            
            if (wouldLikeToLoadEmbedded >= 0 && loadedEmbeddedSub)
            {
                return SubtitleStreams.First(e => e.Path == wouldLikeToLoadEmbedded.ToString());
            }

			return null;
		}

		#endregion

	}
}
