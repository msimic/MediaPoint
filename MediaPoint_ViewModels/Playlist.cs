using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using MediaPoint.MVVM;

namespace MediaPoint.VM
{
    public class Playlist : ViewModel
    {
        Player _player;

        public ObservableCollection<Track> Tracks
        {
            get { return GetValue(() => Tracks); }
            set { SetValue(() => Tracks, value); }
        }

        public Track SelectedTrack
        {
            get { return GetValue(() => SelectedTrack); }
            set { SetValue(() => SelectedTrack, value); }
        }

        public Track CurrentTrack
        {
            get { return GetValue(() => CurrentTrack); }
            set { SetValue(() => CurrentTrack, value); }
        }

        public Playlist(Player player)
        {
            _player = player;
            Repeat = RepeatMode.List;
            Shuffle = false;
            Tracks = new ObservableCollection<Track>();
            Tracks.CollectionChanged += Tracks_CollectionChanged;
        }

        void Tracks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            int i = 1;
            foreach (var item in Tracks)
            {
                item.Position = i++;
            }
        }


        public ICommand PlayCommand
        {
            get
            {
                return new Command(o =>
                {
                    if (SelectedTrack != null)
                    {
                        _player.ForceStop();
                        _player.OpenCommand.Execute(SelectedTrack.Uri);
                    }
                }, can =>
                {
                    return SelectedTrack != null;
                });
            }
        }

        public ICommand MoveUpCommand
        {
            get
            {
                return new Command(o =>
                {
                    if (SelectedTrack != null)
                    {
                        var tr = SelectedTrack;
                        int cIndex = Tracks.IndexOf(tr);
                        Tracks.Move(cIndex, --cIndex);
                        SelectedTrack = tr;
                        CommandManager.InvalidateRequerySuggested();
                    }
                }, can =>
                {
                    return Tracks.Any() && SelectedTrack != null && Tracks.First() != SelectedTrack;
                });
            }
        }

        public ICommand MoveDownCommand
        {
            get
            {
                return new Command(o =>
                {
                    if (SelectedTrack != null)
                    {
                        var tr = SelectedTrack;
                        int cIndex = Tracks.IndexOf(tr);
                        Tracks.Move(cIndex, ++cIndex);
                        SelectedTrack = tr;
                        CommandManager.InvalidateRequerySuggested();
                    }
                }, can =>
                {
                    return Tracks.Any() && SelectedTrack != null && Tracks.Last() != SelectedTrack;
                });
            }
        }

        public ICommand RemoveFromPlaylistCommand
        {
            get
            {
                return new Command(o =>
                {
                    var tracks = Tracks.Where(t => t.IsSelected).ToArray();
                    foreach (var item in tracks)
                    {
                        Tracks.Remove(item);
                    }
                    CommandManager.InvalidateRequerySuggested();
                }, can =>
                {
                    return Tracks.Any(t => t.IsSelected);
                });
            }
        }

        public ICommand PreviousCommand
        {
            get
            {
                return new Command(o =>
                {
                    SelectedTrack = TrackForUri(GetPreviousTrack());
                    PlayCommand.Execute(null);
                    CommandManager.InvalidateRequerySuggested();
                }, can =>
                {
                    return GetPreviousTrack() != null;
                });
            }
        }

        public ICommand NextCommand
        {
            get
            {
                return new Command(o =>
                {
                    SelectedTrack = TrackForUri(GetNextTrack());
                    PlayCommand.Execute(null);
                    CommandManager.InvalidateRequerySuggested();
                }, can =>
                {
                    return GetNextTrack() != null;
                });
            }
        }

        public void SetPlaying(Uri uri)
        {
            foreach (var item in Tracks)
            {
                item.IsPlaying = false;
            }

            if (uri != null)
            {
                var track = TrackForUri(uri);
                if (track != null)
                {
                    track.IsPlaying = true;
                    CurrentTrack = track;
                }
            }
        }

        public enum RepeatMode
        {
            Song,
            List,
            None
        }

        public RepeatMode Repeat
        {
            get { return GetValue(() => Repeat); }
            set { SetValue(() => Repeat, value); }
        }

        public bool Shuffle
        {
            get { return GetValue(() => Shuffle); }
            set { SetValue(() => Shuffle, value); }
        }

        public Uri GetPreviousTrack()
        {
            if (!Tracks.Any()) return null;

            if (Repeat == RepeatMode.Song && CurrentTrack != null)
            {
                return CurrentTrack.Uri;
            }

            if (Repeat == RepeatMode.None && CurrentTrack == Tracks.First() && Shuffle == false)
            {
                return null;
            }

            if (Repeat == RepeatMode.List && CurrentTrack == Tracks.First() && Shuffle == false)
            {
                return Tracks.Last().Uri;
            }

            if ((Repeat == RepeatMode.List || Repeat == RepeatMode.None) && Shuffle == true)
            {
                return GetRandomTrack().Uri;
            }

            if (CurrentTrack == null)
            {
                return Tracks.Last().Uri;
            }

            if (CurrentTrack != Tracks.First() && Tracks.IndexOf(CurrentTrack) > -1)
            {
                return Tracks[Tracks.IndexOf(CurrentTrack) - 1].Uri;
            }

            return null;
        }

        public Uri GetNextTrack()
        {
            if (!Tracks.Any()) return null;

            if (Repeat == RepeatMode.Song && CurrentTrack != null)
            {
                return CurrentTrack.Uri;
            }

            if (Repeat == RepeatMode.None && CurrentTrack == Tracks.Last() && Shuffle == false)
            {
                return null;
            }

            if (Repeat == RepeatMode.List && CurrentTrack == Tracks.Last() && Shuffle == false)
            {
                return Tracks.First().Uri;
            }

            if ((Repeat == RepeatMode.List || Repeat == RepeatMode.None) && Shuffle == true)
            {
                return GetRandomTrack().Uri;
            }

            if (CurrentTrack == null)
            {
                return Tracks.First().Uri;
            }

            if (CurrentTrack != Tracks.Last())
            {
                return Tracks[Tracks.IndexOf(CurrentTrack) + 1].Uri;
            }

            return null;
        }

        public Track GetRandomTrack()
        {
            if (!Tracks.Any()) return null;

            Random r = new Random();
            return Tracks.ToArray()[r.Next(Tracks.Count)];
        }

        public Track TrackForUri(Uri uri)
        {
            return Tracks.FirstOrDefault(t => {
                if (uri.IsFile)
                {
                    return t.Uri.AbsolutePath == uri.AbsolutePath;
                }
                else
                {
                    return t.Uri.AbsoluteUri == uri.AbsoluteUri;
                }
            });
        }

        string GetTitleForTrack(Uri uri)
        {
            try
            {
                if (uri.IsFile)
                {
                    return Path.GetFileNameWithoutExtension(uri.LocalPath);
                }
                else
                {
                    return Uri.UnescapeDataString(uri.AbsoluteUri);
                }
            }
            catch
            {
                return Uri.UnescapeDataString(uri.AbsoluteUri);
            }
        }

        public void AddTrackIfNotExisting(Uri uri)
        {
            var track = TrackForUri(uri);
            if (track != null)
            {
                SelectedTrack = track;
                return;
            }

            track = new Track { Title = GetTitleForTrack(uri), Uri = uri };
            Tracks.Add(track);
            SelectedTrack = track;
        }
    }
}
